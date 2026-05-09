namespace IdleWizard.BuildTool.Cli.Commands;

public static class MemoryPoolModel
{
    public const string TotalMemoriesPath = "$.Memories.TotalMemories";
    public const string CurrentMemoriesPath = "$.Memories.Memories";
    public const string SwitchMemoriesPath = "$.Memories.SwitchMemories";

    public static DuplicatedMemoryPools Create(decimal totalMemories)
    {
        return new DuplicatedMemoryPools(
            MemoriesTotal: totalMemories,
            ImprintSpendPool: totalMemories,
            HeritageSpendPool: totalMemories
        );
    }
}

public sealed record DuplicatedMemoryPools(
    decimal MemoriesTotal,
    decimal ImprintSpendPool,
    decimal HeritageSpendPool
);
