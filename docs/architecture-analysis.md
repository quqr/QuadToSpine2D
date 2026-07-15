# QuadToSpine2D 架构分析报告

**分析日期**: 2026-07-15  
**分析方法**: sql-manything + grill-with-docs + improve-codebase-architecture  
**查询次数**: 3轮全面查询，共30+次SQL查询

---

## 项目概述

**领域**: 2D 动画格式转换（Quad → Spine 2D）

**技术栈**: C#、Avalonia UI、SkiaSharp、Newtonsoft.Json

**文件统计**: 119文件（99个CS），无测试文件

**核心流程**:
```
Quad JSON → ProcessQuadJsonFile → ProcessSpine2DJson → Spine JSON
```

---

## 领域词汇表

参见 [CONTEXT.md](../CONTEXT.md) 中的完整定义。

### 核心概念
- **Quad**: Vanillaware 游戏引擎的 2D 动画格式
- **Spine**: Spine 2D 动画编辑器的 JSON 格式
- **转换（Conversion）**: Quad → Spine 的完整流程

### 数据结构
- **Skeleton（骨骼）**: 完整的动画角色定义
- **Animation（动画）**: Timeline 的有序集合
- **Keyframe（关键帧）**: 某一帧的图层快照
- **KeyframeLayer（图层）**: 关键帧内的单个图层
- **Timeline（时间线）**: 动画帧的容器
- **Slot（槽位）**: 图层的容器
- **Attachment（附件）**: 可附加到槽位的对象
- **Hitbox（命中盒）**: 碰撞检测区域
- **Pool（对象池）**: 复用 PoolData 的机制

---

## 第一类问题：严重架构缺陷

### 候选 1: 零测试覆盖 ⚠️ **新增**

**问题**:
- 项目完全没有测试文件
- 99个CS文件，0个测试文件
- 无法验证重构的正确性

**影响**:
- 任何重构都是高风险操作
- 无法保证代码质量
- 难以捕捉回归错误

**建议**:
1. 优先为核心转换逻辑添加单元测试
2. 使用 xUnit + Moq 框架
3. 测试覆盖目标：核心模块 > 70%

---

### 候选 2: ProcessSpine2DJson 拆分

**文件**: `QTSCore/Process/ProcessSpine2DJson.cs`（13,852 bytes, 128 segments）

**问题**:
- God Class，承担 6+ 个职责
- 添加新附件类型需要修改多处 switch 语句
- 维护者曾遗漏需要修改的地方

**设计决策**:
| 决策点 | 选择 |
|--------|------|
| 核心职责 | 协调器 |
| 附件处理 | 策略模式 |
| 接口形状 | `IAttachmentHandler` 有 `OnAdded`/`OnRemoved` |
| 处理器状态 | 无状态，状态在 `ConversionContext` |
| 注册机制 | 手动注册 |

**预期结构**:
```
QTSCore/Process/
├── ProcessSpine2DJson.cs      ← 简化，使用 handlers
└── AttachmentHandlers/
    ├── IAttachmentHandler.cs
    ├── ConversionContext.cs
    ├── KeyframeHandler.cs
    ├── SlotHandler.cs
    └── HitboxHandler.cs
```

---

### 候选 3: QTSCore 层依赖反转

**文件**:
- `QTSCore/Process/ProcessSpine2DJson.cs`
- `QTSCore/Process/ProcessImages.cs`
- `QTSCore/Utility/ProcessUtility.cs`

**问题**:
- 核心层直接依赖 UI 层（`Instances.ConverterSetting`）
- 隐藏依赖，构造函数签名无法表明需要什么
- 无法单独测试核心逻辑

**发现的服务定位器使用**:
| 文件 | Instances 使用次数 |
|------|-------------------|
| Instances.cs | 9（定义） |
| FileManagerViewModel.cs | 3 |
| ProcessQuadData.cs | 2 |
| PlayerViewModel.cs | 1 |
| ProcessImages.cs | 1 |
| ProcessSpine2DJson.cs | 1 |
| ProcessUtility.cs | 1 |

**设计决策**:
| 决策点 | 选择 |
|--------|------|
| 接口范围 | 多个接口，按职责分离 |
| 注入方式 | 构造函数注入为主 |

---

### 候选 4: 服务定位器模式滥用 ⚠️ **新增**

**文件**: `Helper/Instances.cs`

**问题**:
- 全局静态服务定位器，隐藏依赖关系
- 使用源代码生成器自动注册服务
- 9处 ServiceProvider 引用
- 违反依赖注入原则，导致测试困难

**代码示例**:
```csharp
// 问题代码：隐藏依赖
public static ConverterSettingViewModel ConverterSetting
{
    get
    {
        if (ServiceProvider == null)
            throw new InvalidOperationException(...);
        return ServiceProvider.GetRequiredService<ConverterSettingViewModel>();
    }
}
```

**建议**:
1. 将服务定位器替换为构造函数注入
2. 仅在 Program.cs 和 App.axaml.cs 中使用 ServiceProvider
3. 所有 ViewModel 通过构造函数接收依赖

---

## 第二类问题：大型类/文件

### 候选 5: PlayerViewModel 拆分

**文件**: `ViewModels/Pages/PlayerViewModel.cs`（33,362 bytes, 236 segments）

**问题**:
- God Class，超过 800 行代码
- 承担职责过多：
  - 动画播放控制
  - 图像渲染（SKSurface/SKCanvas）
  - 颜色管理
  - UI 状态管理
  - 文件加载

**统计**:
| 指标 | 值 |
|------|-----|
| 文件大小 | 33,362 bytes |
| 代码段数 | 236 |
| using 语句 | 20 |
| try-catch | 6/8 |
| async/await | 7/8 |
| new 关键字 | 39 |
| Logger 调用 | 39 |

**设计建议**:
| 职责 | 提取目标 |
|------|----------|
| 渲染逻辑 | `QuadRenderer` 服务 |
| 动画播放 | `AnimationPlayer` 服务 |
| 颜色管理 | `ColorizeManager` |

---

### 候选 6: FileManagerViewModel 拆分

**文件**: `ViewModels/Pages/FileManagerViewModel.cs`（27,762 bytes, 211 segments）

**问题**:
- God Class，超过 700 行代码
- 混合职责：
  - 文件夹/文件选择
  - 文件加载和解析
  - 缩略图生成
  - 导出功能
  - UI 状态管理

**统计**:
| 指标 | 值 |
|------|-----|
| 文件大小 | 27,762 bytes |
| 代码段数 | 211 |
| using 语句 | 15 |
| try-catch | 13/15 |
| async/await | 13/28 |

---

### 候选 7: 大型数据类

**问题**: 单文件包含过多类定义

| 文件 | 类数 | 公共成员 |
|------|------|----------|
| SpineJsonData.cs | 19 | 75 |
| QuadJsonData.cs | 13 | 82 |
| V55Data.cs | 13 | 75 |
| QuadData.cs | 11 | 51 |

**设计建议**:
将相关类拆分到独立文件：
- `QuadAnimation.cs` — Animation, Timeline 类
- `QuadKeyframe.cs` — Keyframe, KeyframeLayer 类
- `QuadSkeleton.cs` — QuadSkeleton, QuadBone 类
- `QuadAttachment.cs` — Attach, AttachType 枚举

---

## 第三类问题：代码重复

### 候选 8: ByteHelper 重复

**文件**:
- `VanillawareConverter/FTEXConverter/ByteHelper.cs`（6,578 bytes, 15 static）
- `VanillawareConverter/MBSConverter/ByteHelper.cs`（6,467 bytes, 11 static）

**问题**:
- 两个 ByteHelper 类在不同命名空间中
- 功能相似但不完全相同
- 代码重复

**建议**:
1. 合并为一个共享的 `ByteHelper` 类
2. 放在 `VanillawareConverter/Common/` 目录

---

### 候选 9: Matrix4x4 完全重复 ⚠️ **确认**

**文件**:
- `VanillawareConverter/MBSConverter/Converters/Matrix4x4.cs`（14 public）
- `VanillawareConverter/MBSConverter/Math/Matrix4x4.cs`（14 public）

**问题**:
- 两个 Matrix4x4 结构体代码完全相同
- 仅命名空间不同

**建议**:
保留一个，删除另一个，使用别名：
```csharp
using Matrix4x4 = VanillawareConverter.Mbs.Math.Matrix4x4;
```

---

### 候选 10: FTEX 平台解析器重复

**文件**（8 个平台解析器）:
- NdsFtexParser.cs, Ps2FtexParser.cs, Ps3FtexParser.cs
- Ps4FtexParser.cs, PspFtexParser.cs, PsvitaFtexParser.cs
- SwitchFtexParser.cs, WiiFtexParser.cs

**问题**:
- 8 个平台特定的解析器类
- 结构相似，都有 Parse 方法
- 大量重复代码

**建议**:
使用模板方法模式或策略模式

---

### 候选 11: MbsToV55Parser 重复代码

**文件**: `VanillawareConverter/MBSConverter/Parsers/MbsToV55Parser.cs`

**问题**:
- 40 个 `new` 关键字使用
- ParseS0 到 ParseSb 共 12 个方法结构几乎相同
- 每个方法：获取 section header → 循环处理 → 根据平台选择解析方式

**建议**:
使用模板方法模式：
```csharp
public abstract class SectionParser<T> {
    protected abstract T ParseElement(byte[] data, PlatformTag tag, bool bigEndian);
    
    public List<T?> Parse(SectionInfo sect, PlatformData platform) {
        // 共享逻辑
    }
}
```

---

## 第四类问题：资源管理

### 候选 12: 缺少 IDisposable 实现 ⚠️ **新增**

**问题**: 使用 SKBitmap/SKSurface 但未实现 IDisposable

| 文件 | 使用资源 | 实现 IDisposable |
|------|----------|-----------------|
| PlayerViewModel.cs | SKBitmap, SKSurface | ✅ 是 |
| FileCardViewModel.cs | SKBitmap | ✅ 是 |
| FileManagerViewModel.cs | SKBitmap | ✅ 是 |
| ProcessImages.cs | SKBitmap[,] | ❌ 否 |
| FtexReader.cs | 像素数据 | ❌ 可能需要 |

**建议**:
为 `ProcessImages` 添加 IDisposable 实现

---

## 第五类问题：接口不足

### 候选 13: 接口稀少 ⚠️ **新增**

**问题**: 项目仅有 3 个接口文件

| 接口 | 文件 |
|------|------|
| IPool | QTSCore/Interfaces/IPool.cs |
| IProcessQuadData | QTSCore/Interfaces/IProcessQuadData.cs |
| FTEX接口 | VanillawareConverter/FTEXConverter/Interfaces.cs |

**缺失的接口**:
- `IImageSettings` — 图像处理配置
- `IAnimationSettings` — 动画配置
- `ILogger` — 日志接口
- `IAttachmentHandler` — 附件处理器
- `IQuadRenderer` — 渲染器

---

### 候选 14: ProcessQuadData 浅层模块

**文件**: `QTSCore/Process/ProcessQuadData.cs`

**问题**:
- 浅层模块，本质是透传
- 删除测试：删除后只是移动复杂度，没有提供杠杆

**建议**:
删除此类，直接使用 ProcessSpine2DJson

---

## 第六类问题：静态类滥用

### 候选 15: 过多静态类 ⚠️ **确认**

**问题**: 30+ 个静态类

**高静态使用文件**:
| 文件 | static 次数 |
|------|-------------|
| BptcTexture.cs | 25 |
| LoggerHelper.cs | 16 |
| Matrix.cs | 15 |
| ByteHelper.cs (FTEX) | 15 |
| CliRoot.cs | 12 |
| ConvertCommands.cs | 11 |
| ProcessUtility.cs | 11 |

**建议**:
1. 保留纯函数的静态类（如数学函数）
2. 有状态的静态类改为依赖注入
3. 日志类使用接口 `ILogger`

---

## 第七类问题：错误处理

### 候选 16: 异常处理不一致 ⚠️ **新增**

**问题**:
- try-catch 分布不均
- 部分位置缺少完整的异常信息
- 缺少统一的错误处理策略

**高 try-catch 使用**:
| 文件 | try | catch |
|------|-----|-------|
| FileManagerViewModel.cs | 13 | 15 |
| ChessboardControl.cs | 18 | 0 |
| PlayerViewModel.cs | 6 | 8 |

**注意**: ChessboardControl 有 18 个 try 但 0 个 catch，可能存在问题

---

## 第八类问题：命名与术语

### 候选 17: 术语一致性 ⚠️ **新增**

**问题**: 代码术语与 CONTEXT.md 一致，但存在模糊命名

**CONTEXT.md 术语覆盖**:
- ✅ Quad, Spine, Conversion
- ✅ Skeleton, Animation, Keyframe, KeyframeLayer
- ✅ Timeline, Slot, Attachment, Hitbox, Pool

**模糊命名**:
- `Instances` — 建议改为 `ServiceLocator`
- `ConverterSetting` — 建议改为 `ConversionSettings`
- `ProcessUtility` — 建议拆分为 `MathHelper`, `AnimationCombiner`

---

## 统计摘要

| 类别 | 数量 |
|------|------|
| **零测试覆盖** | 0 个测试文件 |
| God Class（>500行） | 6 |
| 代码重复（类/方法） | 5 处 |
| 依赖反转问题 | 4 处 |
| 过多静态类 | 30+ |
| 过大文件（>10KB） | 15 |
| 缺少 IDisposable | 3+ |
| 接口数量 | 3 |

---

## 实现顺序建议

### 第一阶段：基础建设（必须先做）

1. **添加测试框架** — 最高优先级
   - 创建测试项目
   - 为核心模块添加单元测试
   - 测试覆盖目标 > 70%

2. **候选 9: Matrix4x4 合并** — 低风险，立即收益
3. **候选 8: ByteHelper 合并** — 低风险，立即收益
4. **候选 4: 服务定位器重构** — 定义配置接口

### 第二阶段：核心重构（中风险，高收益）

5. **候选 3: 依赖反转** — 定义配置接口
6. **候选 14: ProcessUtility 拆分** — 依赖候选 4
7. **候选 11: MbsToV55Parser 模板化** — 消除重复逻辑
8. **候选 7: 大型数据类拆分** — 文件拆分

### 第三阶段：高层重构（高风险，高收益）

9. **候选 2: ProcessSpine2DJson 拆分** — 使用策略模式
10. **候选 14: 删除 ProcessQuadData** — 独立，可并行
11. **候选 5: PlayerViewModel 拆分** — 大型 God Class
12. **候选 6: FileManagerViewModel 拆分** — 大型 God Class

### 第四阶段：可选重构（低优先级）

13. **候选 10: FTEX 解析器重构** — 策略模式
14. **候选 12: 添加 IDisposable** — 资源管理
15. **候选 15: 静态类重构** — 依赖注入改造
16. **候选 13: 接口补充** — 增加抽象层

---

## 架构原则

### 接口深度
- **Deep**: 高杠杆，小接口大实现
- **Shallow**: 接口复杂度接近实现复杂度

### 接缝（Seam）
- 一个适配器 = 假设的接缝
- 两个适配器 = 真正的接缝

### 删除测试
- 删除模块后，如果复杂度消失 → 透传
- 删除模块后，如果复杂度重新出现在 N 个调用者 → 有价值

---

## 验证清单

在实施任何重构前，确认以下条件：

- [ ] 已为核心模块添加测试
- [ ] 测试覆盖率达到目标（>70%）
- [ ] 重构后有测试验证
- [ ] 无回归问题
- [ ] 符合 CONTEXT.md 术语定义

---

## 附录：SQL 查询记录

### 查询 1: 文件大小分布
```sql
SELECT f.path, length(f.content) AS size FROM files f 
WHERE f.ext = '.cs' ORDER BY size DESC LIMIT 20;
```

### 查询 2: 静态使用分布
```sql
SELECT f.path, (length(f.content) - length(replace(f.content, 'static', ''))) / 6 
FROM files f WHERE f.ext = '.cs' ORDER BY static_count DESC;
```

### 查询 3: 服务定位器使用
```sql
SELECT f.path, (length(f.content) - length(replace(f.content, 'Instances.', ''))) / 10 
FROM files f WHERE f.content LIKE '%Instances.%';
```

### 查询 4: 类定义统计
```sql
SELECT f.path, (length(f.content) - length(replace(f.content, 'class ', ''))) / 6 
FROM files f WHERE f.ext = '.cs' ORDER BY class_count DESC;
```

### 查询 5: 测试文件检查
```sql
SELECT f.path FROM files f 
WHERE f.ext = '.cs' AND (f.path LIKE '%Test%' OR f.path LIKE '%test%');
-- 结果：0 行
```

---

## 术语表

参见 [CONTEXT.md](../CONTEXT.md) 中的领域定义。

---

*本报告基于 sql-manything、grill-with-docs、improve-codebase-architecture 三种技能综合分析生成。*