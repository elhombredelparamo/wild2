using Godot;
using System.Threading.Tasks;
using System.Collections.Generic;
using Wild.Utils;

namespace Wild.Core.Terrain
{
    public partial class ChunkRenderer : MeshInstance3D
    {
        private Vector2I _chunkPos;
        private TerrainGenerator _generator;
        private MaterialCache _materialCache;

        public void Initialize(Vector2I chunkPos, TerrainGenerator generator, MaterialCache materialCache)
        {
            _chunkPos = chunkPos;
            _generator = generator;
            _materialCache = materialCache;
            
            Position = new Vector3(_chunkPos.X * 10, 0, _chunkPos.Y * 10);
            MaterialOverride = _materialCache.GetTerrainMaterial();
        }


        public void UpdateMeshWithHeightMap(Mesh mesh, float[] altitudes)
        {
            Mesh = mesh;
            
            // Limpiar colisiones previas
            foreach (var child in GetChildren())
            {
                if (child is StaticBody3D) child.QueueFree();
            }
            
            if (altitudes == null || altitudes.Length == 0) return;

            // Crear los nodos de física optimizados (HeightMapShape3D es MUCHO más estable)
            var staticBody = new StaticBody3D();
            var collisionShape = new CollisionShape3D();
            var shape = new HeightMapShape3D();
            
            shape.MapWidth = ChunkData.Resolution;
            shape.MapDepth = ChunkData.Resolution;
            shape.MapData = altitudes;
            
            collisionShape.Shape = shape;
            
            // HeightMapShape3D en Godot 4 está centrado en el nodo.
            // Como nuestro chunk va de 0 a 10, desplazamos la colisión al centro (5, 0, 5)
            float offset = (ChunkData.Resolution - 1) * 0.5f;
            collisionShape.Position = new Vector3(offset, 0, offset);
            
            staticBody.AddChild(collisionShape);
            AddChild(staticBody);
        }

    }
}
