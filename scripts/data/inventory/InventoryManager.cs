using System.Collections.Generic;
using Godot;

namespace Wild.Data.Inventory
{
    public partial class InventoryManager : Node
    {
        public static InventoryManager Instance { get; private set; }

        public List<InventoryContainer> Containers { get; private set; } = new List<InventoryContainer>();
        public InventoryContainer HandLeft { get; private set; }
        public InventoryContainer HandRight { get; private set; }

        public override void _Ready()
        {
            Instance = this;
            InitializeHands();
            GD.Print("[SISTEMA][InventoryManager] Inicializado.");
        }

        private void InitializeHands()
        {
            // Las manos solo tienen 1 slot y soportan max 10kg
            HandLeft = new InventoryContainer("hand_left", "Mano Izquierda", 1, 10.0f);
            HandLeft.IconPath = "res://assets/textures/items/manoizq.png";
            
            HandRight = new InventoryContainer("hand_right", "Mano Derecha", 1, 10.0f);
            HandRight.IconPath = "res://assets/textures/items/manoder.png";
            
            Containers.Add(HandLeft);
            Containers.Add(HandRight);
            
            GD.Print("[SISTEMA][InventoryManager] Contenedores de mano listos (1 slot, 10kg cada uno).");
        }

        public void AddContainer(InventoryContainer container)
        {
            if (!Containers.Contains(container))
            {
                Containers.Add(container);
                GD.Print($"[SISTEMA][InventoryManager] Nuevo contenedor registrado: {container.Name}");
            }
        }

        public List<InventoryContainer> GetActiveContainers()
        {
            return Containers;
        }
    }
}
