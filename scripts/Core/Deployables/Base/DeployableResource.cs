using Godot;
using System.Collections.Generic;

namespace Wild.Core.Deployables.Base
{
    [GlobalClass]
    public partial class DeployableResource : Resource
    {
        [Export] public string Id { get; set; } = "";
        [Export] public string Name { get; set; } = "";
        [Export] public string Category { get; set; } = "General";
        [Export] public string Description { get; set; } = "";
        [Export] public string IconPath { get; set; } = "";
        
        [Export] public string TechnicalId { get; set; } = ""; // ID used for TerrainManager (e.g. "cofre1")
        
        // Material required: ItemID -> Amount
        [Export] public Godot.Collections.Dictionary<string, int> Requirements { get; set; } = new();

        // Assembly Phase: ToolID -> Number of interactions
        // Default tool is "hand" (E with empty hands)
        [Export] public Godot.Collections.Dictionary<string, int> AssemblySteps { get; set; } = new();
    }
}
