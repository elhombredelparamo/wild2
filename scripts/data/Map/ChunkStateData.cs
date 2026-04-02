using System.Collections.Generic;
using Godot;

namespace Wild.Data
{
    /// <summary>
    /// Estructura para persistir el estado de un chunk, como vegetación eliminada, ítems sueltos, etc.
    /// Se guarda como JSON por chunk: chunk_{X}_{Y}.json
    /// </summary>
    public class ChunkStateData
    {
        public List<int> RemovedVegetationIndices { get; set; } = new List<int>();
        public List<int> RemovedGeologyIndices { get; set; } = new List<int>();
        public List<DeployableData> AddedDeployables { get; set; } = new List<DeployableData>();
        
        // Aquí se pueden añadir propiedades futuras (ej. cofres, drops, etc.)
    }
}
