using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;
using Wild.Core.Deployables.Base;
using Wild.Data;
using Wild.Data.Inventory;
using Wild.Utils;
using Wild.Core.Terrain;

namespace Wild.Core.Deployables.Base
{
    public class ConstructionSaveState
    {
        [JsonInclude] public List<DepositedItem> DepositedItems { get; set; } = new();
        [JsonInclude] public bool IsMaterialPhaseComplete { get; set; } = false;
        [JsonInclude] public Dictionary<string, int> AssemblyProgress { get; set; } = new();
    }

    public class DepositedItem
    {
        [JsonInclude] public string ItemId { get; set; }
        [JsonInclude] public int Quantity { get; set; }
    }

    public partial class ConstructionDeployable : DeployableBase
    {
        public DeployableResource Recipe { get; private set; }
        public InventoryContainer SiteContainer { get; private set; }
        
        // Assembly Phase State
        public bool IsMaterialPhaseComplete { get; private set; } = false;
        public Dictionary<string, int> AssemblyProgress { get; private set; } = new();
        
        private Label3D _statusLabel;

        public static event Action<ConstructionDeployable> OnConstructionInteracted;

        public void Initialize(DeployableResource recipe, InventoryContainer container)
        {
            Recipe = recipe;
            SiteContainer = container;
            SetupFromRecipe();
        }

        private void SetupFromRecipe()
        {
            if (Recipe == null) return;
            
            TypeId = "construction_" + Recipe.Id;
            
            // Re-assign validator to ensure it's always active
            SiteContainer.Validator = ValidateItemTransfer;
            
            // Cleanup existing StaticBodies and StatusLabels to avoid duplication on re-init
            foreach (var child in GetChildren())
            {
                if (child is StaticBody3D oldBody)
                {
                    RemoveChild(oldBody);
                    oldBody.QueueFree();
                }
                else if (child is Label3D oldLabel)
                {
                    RemoveChild(oldLabel);
                    oldLabel.QueueFree();
                }
            }
            
            // Add a StaticBody3D and CollisionShape3D exactly like normal deployables
            var staticBody = new StaticBody3D();
            staticBody.CollisionLayer = 8;
            staticBody.CollisionMask = 0;
            
            Aabb aabb = CalculateAABBRecursive(this);
            var colShape = new CollisionShape3D();
            var box = new BoxShape3D();
            
            if (aabb.Size.Length() < 0.1f)
            {
                 box.Size = new Vector3(1.5f, 1.5f, 1.5f);
                 colShape.Position = new Vector3(0, 0.75f, 0);
            }
            else
            {
                 box.Size = aabb.Size;
                 colShape.Position = aabb.Position + (aabb.Size * 0.5f);
            }
            
            colShape.Shape = box;
            staticBody.AddChild(colShape);
            AddChild(staticBody);
            
            // Create Status Label for Assembly
            _statusLabel = new Label3D();
            _statusLabel.Name = "StatusLabel";
            _statusLabel.Billboard = BaseMaterial3D.BillboardModeEnum.Enabled;
            _statusLabel.Position = new Vector3(0, box.Size.Y + 0.5f, 0);
            _statusLabel.FontSize = 48;
            _statusLabel.OutlineSize = 12;
            _statusLabel.Modulate = new Color(1, 0.8f, 0); // Gold
            AddChild(_statusLabel);
            
            UpdateAssemblyUI();
            
            Logger.LogInfo($"CONSTRUCTION: Setup for '{Recipe.Name}' complete. Collision Size: {box.Size}");
        }

        private void UpdateAssemblyUI()
        {
            if (_statusLabel == null || Recipe == null) return;
            
            if (!IsMaterialPhaseComplete)
            {
                _statusLabel.Visible = false;
                return;
            }
            
            _statusLabel.Visible = true;
            
            // Calculate total and current progress
            int totalNeeded = 0;
            int totalDone = 0;
            string currentToolNeeded = "???";

            foreach (var step in Recipe.AssemblySteps)
            {
                totalNeeded += step.Value;
                int currentHandled = AssemblyProgress.ContainsKey(step.Key) ? AssemblyProgress[step.Key] : 0;
                totalDone += currentHandled;
                
                if (currentHandled < step.Value && currentToolNeeded == "???")
                {
                    currentToolNeeded = step.Key;
                }
            }

            if (totalNeeded <= 0)
            {
                _statusLabel.Text = "¡Listo para finalizar!";
                return;
            }

            int percent = (int)((float)totalDone / totalNeeded * 100);
            string toolName = currentToolNeeded == "hand" ? "Manos" : currentToolNeeded;
            _statusLabel.Text = $"{toolName}: {percent}%";
        }

        public void TransitionToAssembly()
        {
            IsMaterialPhaseComplete = true;
            UpdateAssemblyUI();
            Logger.LogInfo($"CONSTRUCTION: '{Recipe.Name}' ha pasado a fase de ENSAMBLADO.");
        }

        public override void Interact()
        {
            if (!IsMaterialPhaseComplete)
            {
                Logger.LogInfo($"CONSTRUCTION: Interacción con '{Recipe.Name}' en construcción. Abriendo BuildUI...");
                OnConstructionInteracted?.Invoke(this);
            }
            else
            {
                // Assembly click
                ProcessAssemblyInteraction("hand"); // No tools yet, so default to hand
            }
        }

        private void ProcessAssemblyInteraction(string toolId)
        {
            if (!Recipe.AssemblySteps.ContainsKey(toolId)) return;
            
            int current = AssemblyProgress.ContainsKey(toolId) ? AssemblyProgress[toolId] : 0;
            if (current < Recipe.AssemblySteps[toolId])
            {
                AssemblyProgress[toolId] = current + 1;
                UpdateAssemblyUI();
                
                // Persist progress to disk
                TerrainManager.Instance?.SaveChunkState(ChunkCoord);
                
                // Check if 100% complete
                bool complete = true;
                foreach (var step in Recipe.AssemblySteps)
                {
                    if (!AssemblyProgress.ContainsKey(step.Key) || AssemblyProgress[step.Key] < step.Value)
                    {
                        complete = false;
                        break;
                    }
                }
                
                if (complete)
                {
                    Logger.LogInfo($"CONSTRUCTION: '{Recipe.Name}' COMPLETADO. Finalizando...");
                    // Using CallDeferred to avoid physics issues during replacement
                    TerrainManager.Instance?.CallDeferred(TerrainManager.MethodName.FinalizeConstruction, this);
                }
            }
        }

        public override void LoadData(string data)
        {
            try
            {
                var saveState = !string.IsNullOrWhiteSpace(data) && data != "{}" 
                    ? JsonSerializer.Deserialize<ConstructionSaveState>(data) 
                    : new ConstructionSaveState();

                if (saveState == null) saveState = new ConstructionSaveState();

                IsMaterialPhaseComplete = saveState.IsMaterialPhaseComplete;
                AssemblyProgress = saveState.AssemblyProgress ?? new();
                
                // Load the recipe resource directly from disk using the stored TypeId
                var recipeId = TypeId.Replace("construction_", "");
                string recipePath = $"res://assets/data/deployables/{recipeId}.tres";
                
                DeployableResource recipe = null;
                if (ResourceLoader.Exists(recipePath))
                {
                    recipe = ResourceLoader.Load<DeployableResource>(recipePath);
                }

                if (recipe == null)
                {
                    Logger.LogWarning($"CONSTRUCTION: No se pudo cargar la receta '{recipeId}' para {TypeId}.");
                    return;
                }

                // Ensure SiteContainer exists
                if (SiteContainer == null)
                {
                    SiteContainer = new InventoryContainer("site_" + TypeId, "Obra: " + recipe.Name, 20, 9999f);
                    SiteContainer.IconPath = recipe.IconPath;
                }
                
                // Restore deposited items
                if (saveState.DepositedItems != null && saveState.DepositedItems.Count > 0)
                {
                    SiteContainer.Slots.Clear();
                    foreach (var deposited in saveState.DepositedItems)
                    {
                        var item = InventoryManager.Instance?.GetItemById(deposited.ItemId);
                        if (item != null)
                        {
                            var slot = new InventorySlot { Item = item, Quantity = deposited.Quantity };
                            SiteContainer.Slots.Add(slot);
                        }
                    }
                    
                    // Fill remaining slots
                    while (SiteContainer.Slots.Count < SiteContainer.MaxSlots)
                    {
                        SiteContainer.Slots.Add(new InventorySlot());
                    }
                }
                else
                {
                    // If no data, ensure we have empty slots
                    if (SiteContainer.Slots.Count == 0)
                    {
                        for (int i = 0; i < SiteContainer.MaxSlots; i++)
                            SiteContainer.Slots.Add(new InventorySlot());
                    }
                }
                
                // FINAL SETUP
                Initialize(recipe, SiteContainer);
            }
            catch (Exception e)
            {
                Logger.LogError($"CONSTRUCTION: Error loading construction data for {TypeId}: {e.Message}");
            }
        }

        public override string SaveData()
        {
            if (SiteContainer == null) return "{}";
            
            var deposited = new List<DepositedItem>();
            foreach (var slot in SiteContainer.Slots)
            {
                if (!slot.IsEmpty())
                {
                    deposited.Add(new DepositedItem { ItemId = slot.Item.Id, Quantity = slot.Quantity });
                }
            }
            
            var state = new ConstructionSaveState 
            { 
                DepositedItems = deposited,
                IsMaterialPhaseComplete = IsMaterialPhaseComplete,
                AssemblyProgress = AssemblyProgress
            };
            
            return JsonSerializer.Serialize(state);
        }



        private Aabb CalculateAABBRecursive(Node node)
        {
            Aabb totalAABB = new Aabb();
            bool first = true;

            void Walk(Node n, Transform3D accumulatedTransform)
            {
                Transform3D currentTransform = accumulatedTransform;
                if (n != node && n is Node3D n3d)
                {
                    currentTransform = accumulatedTransform * n3d.Transform;
                }

                if (n is MeshInstance3D meshInstance)
                {
                    Aabb localAABB = meshInstance.GetAabb();
                    Aabb rootSpaceAABB = currentTransform * localAABB;

                    if (first) { totalAABB = rootSpaceAABB; first = false; }
                    else { totalAABB = totalAABB.Merge(rootSpaceAABB); }
                }

                foreach (Node child in n.GetChildren()) Walk(child, currentTransform);
            }

            Walk(node, Transform3D.Identity);
            
            if (first) return new Aabb(new Vector3(-0.4f, 0, -0.4f), new Vector3(0.8f, 0.8f, 0.8f));
            return totalAABB;
        }
        private int ValidateItemTransfer(InventoryItem item, int quantity)
        {
            if (item == null || Recipe == null) return 0;
            
            // Check if item is required
            if (!Recipe.Requirements.ContainsKey(item.Id))
            {
                Logger.LogInfo($"CONSTRUCTION: Item '{item.Name}' no es necesario para esta obra.");
                return 0;
            }
            
            int required = Recipe.Requirements[item.Id];
            int current = SiteContainer.GetTotalQuantity(item.Id);
            
            int canAdd = Math.Max(0, required - current);
            int finalToAdd = Math.Min(quantity, canAdd);
            
            if (finalToAdd < quantity)
            {
                Logger.LogInfo($"CONSTRUCTION: Limitando '{item.Name}' de {quantity} a {finalToAdd} (Requerido: {required}, Actual: {current})");
            }
            
            return finalToAdd;
        }
    }
}
