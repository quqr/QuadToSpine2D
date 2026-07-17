

// 强制启用全并行测试执行
[assembly:
    CollectionBehavior(CollectionBehavior.CollectionPerAssembly, DisableTestParallelization = false,
        MaxParallelThreads = -1)]