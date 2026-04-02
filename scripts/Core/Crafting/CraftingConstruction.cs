using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;
using Wild.Core.Terrain;
using Wild.Data.Inventory;
using Wild.Utils;

namespace Wild.Core.Crafting
{
    // ── DTOs de persistencia ──────────────────────────────────────────────────

    public class CraftingSaveState
    {
        [JsonInclude] public List<CraftingDepositedItem> DepositedItems { get; set; } = new();
        [JsonInclude] public bool IsMaterialPhaseComplete { get; set; } = false;
        [JsonInclude] public Dictionary<string, int> AssemblyProgress { get; set; } = new();
        [JsonInclude] public string RecipeId { get; set; } = "";
    }

    public class CraftingDepositedItem
    {
        [JsonInclude] public string ItemId { get; set; }
        [JsonInclude] public int Quantity { get; set; }
    }

    /// <summary>
    /// Nodo de construcción del sistema de crafteos.
    /// Gestiona las 3 fases: Ghost visible → Depósito de materiales → Ensamblado.
    /// Al completar el 100% llama a TerrainManager.FinalizeCraft para producir un WorldItemDeployable.
    /// Es independiente de ConstructionDeployable (sistema de Deployables).
    /// NO hereda de DeployableBase porque es un nodo temporal (se destruye al finalizar).
    /// Se guarda en el chunk con TypeId "crafting_<recipeId>" para persistir obras a medias.
    /// </summary>
    public partial class CraftingConstruction : Node3D
    {
        // ── Propiedades Públicas ───────────────────────────────────────────────

        public CraftableResource Recipe { get; private set; }
        public InventoryContainer SiteContainer { get; private set; }
        public bool IsMaterialPhaseComplete { get; private set; } = false;
        public Dictionary<string, int> AssemblyProgress { get; private set; } = new();

        /// <summary>Coordenada del chunk donde se ha colocado esta obra.</summary>
        public Vector2I ChunkCoord { get; set; }

        /// <summary>TypeId para persistencia en chunk JSON: "crafting_&lt;recipeId&gt;"</summary>
        public string TypeId { get; set; } = "";

        private Label3D _statusLabel;

        /// <summary>Se emite cuando el jugador interactúa con la obra en fase de materiales.</summary>
        public static event Action<CraftingConstruction> OnCraftingInteracted;

        // ── Inicialización ────────────────────────────────────────────────────

        public void Initialize(CraftableResource recipe, InventoryContainer container)
        {
            Recipe = recipe;
            SiteContainer = container;
            TypeId = "crafting_" + recipe.Id;
            SetupFromRecipe();
        }

        private void SetupFromRecipe()
        {
            if (Recipe == null) return;

            SiteContainer.Validator = ValidateItemTransfer;

            // Limpiar hijos previos (StaticBody + Label)
            foreach (var child in GetChildren())
            {
                if (child is StaticBody3D || child is Label3D)
                {
                    RemoveChild(child);
                    ((Node)child).QueueFree();
                }
            }

            // Colisión en capa 4 (interacciones)
            var staticBody = new StaticBody3D();
            staticBody.CollisionLayer = 8;
            staticBody.CollisionMask = 0;

            Aabb aabb = CalculateAABBRecursive(this);
            var colShape = new CollisionShape3D();
            var box = new BoxShape3D();

            if (aabb.Size.Length() < 0.1f)
            {
                box.Size = new Vector3(0.8f, 0.8f, 0.8f);
                colShape.Position = new Vector3(0, 0.4f, 0);
            }
            else
            {
                box.Size = aabb.Size;
                colShape.Position = aabb.Position + (aabb.Size * 0.5f);
            }

            colShape.Shape = box;
            staticBody.AddChild(colShape);
            AddChild(staticBody);

            // Label de estado
            _statusLabel = new Label3D();
            _statusLabel.Name = "StatusLabel";
            _statusLabel.Billboard = BaseMaterial3D.BillboardModeEnum.Enabled;
            _statusLabel.Position = new Vector3(0, box.Size.Y + 0.5f, 0);
            _statusLabel.FontSize = 48;
            _statusLabel.OutlineSize = 12;
            _statusLabel.Modulate = new Color(0.2f, 1f, 0.5f); // Verde crafteo
            AddChild(_statusLabel);

            UpdateStatusUI();
            Logger.LogInfo($"CRAFTING: Setup para '{Recipe.Name}' completo. Colisión: {box.Size}");
        }

        // ── UI de Estado ──────────────────────────────────────────────────────

        private void UpdateStatusUI()
        {
            if (_statusLabel == null || Recipe == null) return;

            if (!IsMaterialPhaseComplete)
            {
                _statusLabel.Text = "[E] Depositar materiales";
                _statusLabel.Visible = true;
                return;
            }

            _statusLabel.Visible = true;

            int totalNeeded = 0, totalDone = 0;
            string currentTool = "???";

            foreach (var step in Recipe.AssemblySteps)
            {
                totalNeeded += step.Value;
                int current = AssemblyProgress.ContainsKey(step.Key) ? AssemblyProgress[step.Key] : 0;
                totalDone  += current;

                if (current < step.Value && currentTool == "???")
                    currentTool = step.Key;
            }

            if (totalNeeded <= 0)
            {
                _statusLabel.Text = "¡Listo para finalizar!";
                return;
            }

            int percent = (int)((float)totalDone / totalNeeded * 100);
            string toolName = currentTool == "hand" ? "Manos" : currentTool;
            _statusLabel.Text = $"{toolName}: {percent}%";
        }

        // ── Transición de Fase ────────────────────────────────────────────────

        public void TransitionToAssembly()
        {
            IsMaterialPhaseComplete = true;
            UpdateStatusUI();
            SaveToChunk();
            Logger.LogInfo($"CRAFTING: '{Recipe.Name}' pasó a fase de ENSAMBLADO.");
        }

        // ── Interacción ───────────────────────────────────────────────────────

        public void Interact()
        {
            if (!IsMaterialPhaseComplete)
            {
                Logger.LogInfo($"CRAFTING: Interacción con '{Recipe?.Name}'. Abriendo CraftBuildUI...");
                OnCraftingInteracted?.Invoke(this);
            }
            else
            {
                ProcessAssemblyInteraction("hand");
            }
        }

        private void ProcessAssemblyInteraction(string toolId)
        {
            if (Recipe == null || !Recipe.AssemblySteps.ContainsKey(toolId)) return;

            int current = AssemblyProgress.ContainsKey(toolId) ? AssemblyProgress[toolId] : 0;
            if (current < Recipe.AssemblySteps[toolId])
            {
                AssemblyProgress[toolId] = current + 1;
                UpdateStatusUI();
                SaveToChunk();

                // Comprobar si está al 100%
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
                    Logger.LogInfo($"CRAFTING: '{Recipe.Name}' COMPLETADO. Finalizando crafteo...");
                    TerrainManager.Instance?.CallDeferred(TerrainManager.MethodName.FinalizeCraft, this);
                }
            }
        }

        private void SaveToChunk()
        {
            TerrainManager.Instance?.SaveCraftingSiteState(ChunkCoord);
        }

        // ── Validación de Materiales ──────────────────────────────────────────

        private int ValidateItemTransfer(InventoryItem item, int quantity)
        {
            if (item == null || Recipe == null) return 0;

            if (!Recipe.Requirements.ContainsKey(item.Id))
            {
                Logger.LogInfo($"CRAFTING: '{item.Name}' no es necesario para '{Recipe.Name}'.");
                return 0;
            }

            int required = Recipe.Requirements[item.Id];
            int current  = SiteContainer.GetTotalQuantity(item.Id);
            int canAdd   = Math.Max(0, required - current);
            int finalAdd = Math.Min(quantity, canAdd);

            if (finalAdd < quantity)
                Logger.LogInfo($"CRAFTING: Limitando '{item.Name}' de {quantity} a {finalAdd} (Requerido: {required}, Actual: {current})");

            return finalAdd;
        }

        // ── Persistencia ──────────────────────────────────────────────────────

        public string SaveData()
        {
            if (SiteContainer == null) return "{}";

            var deposited = new List<CraftingDepositedItem>();
            foreach (var slot in SiteContainer.Slots)
            {
                if (!slot.IsEmpty())
                    deposited.Add(new CraftingDepositedItem { ItemId = slot.Item.Id, Quantity = slot.Quantity });
            }

            var state = new CraftingSaveState
            {
                DepositedItems = deposited,
                IsMaterialPhaseComplete = IsMaterialPhaseComplete,
                AssemblyProgress = AssemblyProgress,
                RecipeId = Recipe?.Id ?? ""
            };

            return JsonSerializer.Serialize(state);
        }

        public void LoadData(string data)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(data) || data == "{}") return;

                var state = JsonSerializer.Deserialize<CraftingSaveState>(data);
                if (state == null) return;

                IsMaterialPhaseComplete = state.IsMaterialPhaseComplete;
                AssemblyProgress = state.AssemblyProgress ?? new();

                // Cargar receta desde disco
                var recipeId = !string.IsNullOrEmpty(state.RecipeId)
                    ? state.RecipeId
                    : TypeId.Replace("crafting_", "");

                string recipePath = $"res://assets/data/craftables/{recipeId}.tres";
                CraftableResource recipe = null;
                if (ResourceLoader.Exists(recipePath))
                    recipe = ResourceLoader.Load<CraftableResource>(recipePath);

                if (recipe == null)
                {
                    Logger.LogWarning($"CRAFTING: No se pudo cargar la receta '{recipeId}'.");
                    return;
                }

                // Reconstruir contenedor
                if (SiteContainer == null)
                    SiteContainer = new InventoryContainer("craft_site_" + TypeId, "Obra: " + recipe.Name, 20, 9999f);

                // Restaurar ítems depositados
                if (state.DepositedItems?.Count > 0)
                {
                    SiteContainer.Slots.Clear();
                    foreach (var dep in state.DepositedItems)
                    {
                        var item = InventoryManager.Instance?.GetItemById(dep.ItemId);
                        if (item != null)
                            SiteContainer.Slots.Add(new InventorySlot { Item = item, Quantity = dep.Quantity });
                    }
                    while (SiteContainer.Slots.Count < SiteContainer.MaxSlots)
                        SiteContainer.Slots.Add(new InventorySlot());
                }
                else if (SiteContainer.Slots.Count == 0)
                {
                    for (int i = 0; i < SiteContainer.MaxSlots; i++)
                        SiteContainer.Slots.Add(new InventorySlot());
                }

                Initialize(recipe, SiteContainer);
            }
            catch (Exception e)
            {
                Logger.LogError($"CRAFTING: Error al cargar datos de '{TypeId}': {e.Message}");
            }
        }

        // ── Utilidades ─────────────────────────────────────────────────────────

        private Aabb CalculateAABBRecursive(Node node)
        {
            Aabb total = new Aabb();
            bool first = true;

            void Walk(Node n, Transform3D acc)
            {
                Transform3D cur = acc;
                if (n != node && n is Node3D n3d) cur = acc * n3d.Transform;
                if (n is MeshInstance3D mesh)
                {
                    Aabb w = cur * mesh.GetAabb();
                    if (first) { total = w; first = false; }
                    else         total = total.Merge(w);
                }
                foreach (Node child in n.GetChildren()) Walk(child, cur);
            }

            Walk(node, Transform3D.Identity);
            return first ? new Aabb(new Vector3(-0.2f, 0, -0.2f), new Vector3(0.4f, 0.4f, 0.4f)) : total;
        }
    }
}
