using Godot;
using System.Collections.Generic;

namespace Wild.Core.Crafting
{
    /// <summary>
    /// Recurso que define una receta de crafteo.
    /// Equivalente independiente de DeployableResource para el sistema de Crafteos.
    /// Almacena: materiales necesarios, pasos de ensamblado, modelo 3D del objeto terminado
    /// y el ID del ítem de inventario que se otorgará al recogerlo.
    /// </summary>
    [GlobalClass]
    public partial class CraftableResource : Resource
    {
        /// <summary>ID único de la receta (ej: "hacha_piedra").</summary>
        [Export] public string Id { get; set; } = "";

        /// <summary>Nombre legible para mostrar en el menú.</summary>
        [Export] public string Name { get; set; } = "";

        /// <summary>Categoría para agrupar en el menú (ej: "Herramientas", "Armas").</summary>
        [Export] public string Category { get; set; } = "General";

        /// <summary>Descripción corta del objeto resultante.</summary>
        [Export] public string Description { get; set; } = "";

        /// <summary>Ruta al icono para el menú de crafteo (res://...).</summary>
        [Export] public string IconPath { get; set; } = "";

        /// <summary>
        /// Ruta al modelo 3D (.glb) del objeto terminado que aparecerá en el mundo.
        /// Es el objeto visible en la fase Ghost y en el WorldItemDeployable final.
        /// </summary>
        [Export] public string ModelPath { get; set; } = "";

        /// <summary>
        /// Escala personalizada a aplicar al modelo 3D cuando se instancia. Útil para corregir
        /// modelos que se importaron demasiado grandes desde Blender.
        /// </summary>
        [Export] public Vector3 ModelScale { get; set; } = Vector3.One;

        /// <summary>
        /// Rotación personalizada en grados a aplicar al modelo 3D cuando se instancie.
        /// Ideal para tumbar armas u objetos que vienen "de pie" desde Blender.
        /// </summary>
        [Export] public Vector3 ModelRotation { get; set; } = Vector3.Zero;

        /// <summary>
        /// ID del InventoryItem que se añade al inventario cuando el jugador recoge el objeto.
        /// Debe coincidir con un ítem registrado en InventoryManager.
        /// </summary>
        [Export] public string ResultItemId { get; set; } = "";

        /// <summary>
        /// Materiales necesarios para iniciar el ensamblado.
        /// Clave: ItemId, Valor: cantidad requerida.
        /// </summary>
        [Export] public Godot.Collections.Dictionary<string, int> Requirements { get; set; } = new();

        /// <summary>
        /// Pasos de ensamblado por herramienta.
        /// Clave: ToolId (ej: "hand", "hacha"), Valor: número de interacciones necesarias.
        /// </summary>
        [Export] public Godot.Collections.Dictionary<string, int> AssemblySteps { get; set; } = new();
    }
}
