namespace IdleWizard.BuildTool.Cli.Commands;

public static class ImprintHeritageSpendModel
{
    public const string TotalMemoriesPath = "$.Memories.TotalMemories";
    public const string OwnedMemoryUpgradeLevelsPath = "$.Memories.Upgrades";
    public const string RealmUpgradesDataFileName = "RealmUpgrades.bytes";
    public const string ImprintGroupName = "Imprint";

    public static ImprintHeritagePools CreatePools(decimal totalMemories)
    {
        return new ImprintHeritagePools(
            TotalMemories: totalMemories,
            ImprintPool: totalMemories,
            HeritagePool: totalMemories
        );
    }

    public static decimal GetSpendForLevel(decimal cost, decimal costDelta, int level)
    {
        if (level <= 0)
        {
            return 0m;
        }

        var n = (decimal)level;
        return (n * ((2m * cost) + ((n - 1m) * costDelta))) / 2m;
    }

    public static decimal GetNextCost(decimal cost, decimal costDelta, int currentLevel)
    {
        if (currentLevel < 0)
        {
            currentLevel = 0;
        }

        return cost + ((decimal)currentLevel * costDelta);
    }

    public static ImprintHeritageSpendSummary BuildSummary(
        decimal totalMemories,
        IReadOnlyList<ImprintHeritageUpgradeSpend> imprints,
        IReadOnlyList<ImprintHeritageUpgradeSpend> heritage)
    {
        var imprintSpent = imprints
            .Where(x => x.IsOwned)
            .Sum(x => x.Spend);

        var heritageSpent = heritage
            .Where(x => x.IsOwned)
            .Sum(x => x.Spend);

        return new ImprintHeritageSpendSummary(
            TotalMemories: totalMemories,
            ImprintPool: totalMemories,
            HeritagePool: totalMemories,
            ImprintTotalRecords: imprints.Count,
            ImprintOwnedRecords: imprints.Count(x => x.IsOwned),
            ImprintSpent: imprintSpent,
            ImprintRemaining: totalMemories - imprintSpent,
            HeritageTotalRecords: heritage.Count,
            HeritageOwnedRecords: heritage.Count(x => x.IsOwned),
            HeritageSpent: heritageSpent,
            HeritageRemaining: totalMemories - heritageSpent
        );
    }
}

public sealed record ImprintHeritagePools(
    decimal TotalMemories,
    decimal ImprintPool,
    decimal HeritagePool
);

public sealed record ImprintHeritageUpgradeSpend(
    int Id,
    string Name,
    string Group,
    string Param,
    int Level,
    string MaxLevel,
    decimal Cost,
    decimal CostDelta,
    decimal Spend,
    decimal NextCost,
    bool IsOwned
);

public sealed record ImprintHeritageSpendSummary(
    decimal TotalMemories,
    decimal ImprintPool,
    decimal HeritagePool,
    int ImprintTotalRecords,
    int ImprintOwnedRecords,
    decimal ImprintSpent,
    decimal ImprintRemaining,
    int HeritageTotalRecords,
    int HeritageOwnedRecords,
    decimal HeritageSpent,
    decimal HeritageRemaining
);
