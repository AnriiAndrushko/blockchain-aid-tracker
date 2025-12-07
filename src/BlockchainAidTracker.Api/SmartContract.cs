using System;
using System.Collections.Generic;

namespace BlockchainAidTracker.Api;

public partial class SmartContract
{
    public string Id { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string Version { get; set; } = null!;

    public string Type { get; set; } = null!;

    public string DeployedAt { get; set; } = null!;

    public int IsEnabled { get; set; }

    public string StateJson { get; set; } = null!;

    public string LastUpdatedAt { get; set; } = null!;
}
