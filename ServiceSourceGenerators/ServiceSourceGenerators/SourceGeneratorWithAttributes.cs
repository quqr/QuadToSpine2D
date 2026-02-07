using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace ServiceSourceGenerators;

/// <summary>
/// 源代码生成器：自动扫描标记了服务特性的类，并生成 DI 注册代码
/// </summary>
[Generator]
public class SourceGeneratorWithAttributes : IIncrementalGenerator
{
    private const string TargetNamespace = "QTSAvalonia.Helper";
    private const string AttributeName = "SingletonService";
    private const string TransientAttributeName = "TransientService";
    private const string ScopedAttributeName = "ScopedService";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // 高效筛选：仅处理含目标特性名称的类声明（语法层快速过滤）
        var provider = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
                transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx))
            .Where(static m => m is not null)
            .Collect();

        context.RegisterSourceOutput(
            context.CompilationProvider.Combine(provider),
            static (spc, source) => Execute(source.Left, source.Right!, spc));
    }

    /// <summary>
    /// 语法层快速过滤：仅保留含目标特性名称的类声明（避免无效语义分析）
    /// </summary>
    private static bool IsSyntaxTargetForGeneration(SyntaxNode node) =>
        node is ClassDeclarationSyntax classDecl &&
        classDecl.AttributeLists.Any(list =>
            list.Attributes.Any(attr =>
                attr.Name.ToString() is AttributeName or TransientAttributeName or ScopedAttributeName));

    /// <summary>
    /// 语义层精确匹配：提取服务注册所需信息（全限定名 + 生命周期）
    /// </summary>
    private static ServiceRegistrationInfo? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        if (context.Node is not ClassDeclarationSyntax classDecl) return null;
        
        var semanticModel = context.SemanticModel;
        var classSymbol = semanticModel.GetDeclaredSymbol(classDecl);
        if (classSymbol is null) return null;

        // 检查目标命名空间下的特性（精确匹配）
        var attrs = classSymbol.GetAttributes()
            .Where(a => a.AttributeClass?.ContainingNamespace.ToDisplayString() == TargetNamespace)
            .ToList();

        if (!attrs.Any(a => 
            a.AttributeClass?.Name is AttributeName or TransientAttributeName or ScopedAttributeName))
            return null;

        var lifetime = GeneratorServiceLifetime.Singleton;
    
        if (attrs.Any(a => a.AttributeClass?.Name == TransientAttributeName))
            lifetime = GeneratorServiceLifetime.Transient;
        else if (attrs.Any(a => a.AttributeClass?.Name == ScopedAttributeName))
            lifetime = GeneratorServiceLifetime.Scoped;
        else if (attrs.FirstOrDefault(a => a.AttributeClass?.Name == AttributeName) is { } serviceAttr)
        {
            // 对于 [Service] 特性，需要检查其参数
            var arg = serviceAttr.ConstructorArguments.FirstOrDefault();
            if (arg.Value is int intValue)
            {
                lifetime = intValue switch
                {
                    1 => GeneratorServiceLifetime.Transient,
                    2 => GeneratorServiceLifetime.Scoped,
                    _ => GeneratorServiceLifetime.Singleton
                };
            }
            else if (arg.Value is object enumValue && enumValue.GetType().Name == "ServiceLifetime")
            {
                // 处理枚举值
                lifetime = serviceAttr.NamedArguments
                    .Where(na => na.Key == "Lifetime")
                    .Select(na => na.Value.Value)
                    .OfType<object>()
                    .FirstOrDefault() switch
                {
                    object val when val.ToString() == "Transient" => GeneratorServiceLifetime.Transient,
                    object val when val.ToString() == "Scoped" => GeneratorServiceLifetime.Scoped,
                    _ => GeneratorServiceLifetime.Singleton
                };
            }
        }
        // 生成带 global:: 前缀的完全限定名（避免命名冲突）
        var format = new SymbolDisplayFormat(
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);
        
        var fullName = classSymbol.ToDisplayString(format);

        return new ServiceRegistrationInfo(fullName, lifetime);
    }

    /// <summary>
    /// 生成注册代码（无重复语义分析，直接使用预提取信息）
    /// </summary>
    private static void Execute(
        Compilation compilation,
        ImmutableArray<ServiceRegistrationInfo?> registrations,
        SourceProductionContext context)
    {
        var validRegistrations = registrations.Where(r => r != null).Cast<ServiceRegistrationInfo>().ToList();

        if (validRegistrations.Count == 0) return;

        // 生成注册语句（按生命周期分组提升可读性）
        var registrationsByLifetime = validRegistrations
            .GroupBy(r => r.Lifetime)
            .OrderBy(g => g.Key); // Singleton -> Scoped -> Transient

        var registrationLines = new List<string>();
        foreach (var group in registrationsByLifetime)
        {
            var method = group.Key switch
            {
                GeneratorServiceLifetime.Singleton => "AddSingleton",
                GeneratorServiceLifetime.Scoped    => "AddScoped",
                GeneratorServiceLifetime.Transient => "AddTransient",
                _                                  => "AddSingleton"
            };

            registrationLines.AddRange(group.OrderBy(r => r.FullName).Select(reg => $"services.{method}<{reg.FullName}>();"));
        }

        // 生成最终源代码
        var source = $$"""
                       // <auto-generated/>
                       // 此文件由 ServiceSourceGenerators 生成：{{System.DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}} UTC

                       using Microsoft.Extensions.DependencyInjection;

                       namespace QTSAvalonia.Helper
                       {
                           public static partial class Instances
                           {
                               static partial void AddServices(IServiceCollection services)
                               {
                       {{string.Join("\n", registrationLines.Select(line => "            " + line))}}
                               }
                           }
                       }
                       """;

        context.AddSource("Instances.ServiceRegistration.g.cs", SourceText.From(source, Encoding.UTF8));
    }
    
    private enum GeneratorServiceLifetime
    {
        Singleton = 0,
        Scoped = 1,    
        Transient = 2  
    }
    
    private class ServiceRegistrationInfo(string FullName, GeneratorServiceLifetime Lifetime)
    {
        public readonly string FullName = FullName;
        public readonly GeneratorServiceLifetime Lifetime = Lifetime;
    }
}