using System;
using Godot;
using Wild;

namespace Wild.Scripts.Terrain
{
    /// <summary>
    /// Representa un chunk de terreno en el mundo
    /// </summary>
    public partial class Chunk : Node3D
    {
        private ChunkData _data = null!;
        private MeshInstance3D _meshInstance = null!;
        private StaticBody3D _staticBody = null!;
        private CollisionShape3D _collisionShape = null!;
        
        // Constantes
        public const int SIZE = 100;
        private const float SCALE = 1f; // 1 unidad = 1 metro
        
        public ChunkData Data => _data;
        public Vector2I ChunkPosition { get; private set; }
        
        public override void _Ready()
        {
            // El TerrainManager se obtendrá cuando se inicialice el chunk
            // CreateTerrainMesh() se llamará desde Initialize()
        }
        
        /// <summary>
        /// Inicializa el chunk con datos
        /// </summary>
        public void Initialize(ChunkData data)
        {
            _data = data;
            ChunkPosition = new Vector2I(data.ChunkX, data.ChunkZ);
            Name = $"Chunk_{data.ChunkX}_{data.ChunkZ}";
            
            // Primero crear los componentes necesarios
            CreateTerrainMesh();
            
            // Luego actualizar el mesh con los datos
            UpdateTerrainMesh();
        }
        
        /// <summary>
        /// Crea el mesh básico del terreno
        /// </summary>
        private void CreateTerrainMesh()
        {
            // Crear MeshInstance3D para el terreno
            _meshInstance = new MeshInstance3D();
            _meshInstance.Name = "TerrainMesh";
            AddChild(_meshInstance);
            
            // Crear StaticBody3D para colisiones del terreno
            _staticBody = new StaticBody3D();
            _staticBody.Name = "TerrainCollision";
            AddChild(_staticBody);
            
            // Crear CollisionShape3D para el terreno
            _collisionShape = new CollisionShape3D();
            _collisionShape.Name = "Shape";
            _staticBody.AddChild(_collisionShape);
            
            // Crear barreras invisibles alrededor del chunk
            CreateChunkBoundaries();
            
            // Asignar material con textura de hierba
            var material = new StandardMaterial3D();
            material.AlbedoTexture = GD.Load<Texture2D>("res://assets/textures/Grass004.png");
            material.Uv1Scale = new Vector3(10f, 10f, 1f); // Repetir textura
            _meshInstance.MaterialOverride = material;
        }
        
        /// <summary>
        /// Crea barreras invisibles alrededor del chunk para impedir que el jugador salga
        /// </summary>
        private void CreateChunkBoundaries()
        {
            // Crear StaticBody3D para barreras físicas (bloqueo real)
            var boundaryBody = new StaticBody3D();
            boundaryBody.Name = "ChunkBoundaries";
            boundaryBody.CollisionLayer = 2; // Capa 2 para barreras de terreno
            boundaryBody.CollisionMask = 0; // No necesita detectar, solo bloquear
            AddChild(boundaryBody);
            
            // Dimensiones del chunk
            float chunkSize = SIZE * SCALE;
            float wallHeight = 1200f; // Altura para cubrir rango completo (-100 a 1000m)
            float wallThickness = 1f; // Grosor mínimo pero efectivo
            
            // Crear las 4 barreras físicas
            CreateBoundaryWall(boundaryBody, "North", new Vector3(chunkSize/2, wallHeight/2, chunkSize), 
                              new Vector3(chunkSize, wallHeight, wallThickness));
            
            CreateBoundaryWall(boundaryBody, "South", new Vector3(chunkSize/2, wallHeight/2, 0), 
                              new Vector3(chunkSize, wallHeight, wallThickness));
            
            CreateBoundaryWall(boundaryBody, "East", new Vector3(chunkSize, wallHeight/2, chunkSize/2), 
                              new Vector3(wallThickness, wallHeight, chunkSize));
            
            CreateBoundaryWall(boundaryBody, "West", new Vector3(0, wallHeight/2, chunkSize/2), 
                              new Vector3(wallThickness, wallHeight, chunkSize));
        }
        
        /// <summary>
        /// Crea una barrera invisible con colisión física
        /// </summary>
        private void CreateBoundaryWall(StaticBody3D parent, string direction, Vector3 position, Vector3 size)
        {
            var collisionShape = new CollisionShape3D();
            var boxShape = new BoxShape3D();
            boxShape.Size = size;
            collisionShape.Shape = boxShape;
            collisionShape.Position = position;
            collisionShape.Name = $"Boundary_{direction}";
            parent.AddChild(collisionShape);
        }
        
        /// <summary>
        /// Elimina una barrera específica (usado cuando se carga un chunk adyacente)
        /// </summary>
        public void RemoveBoundary(string direction)
        {
            var boundaryBody = GetNode<StaticBody3D>("ChunkBoundaries");
            if (boundaryBody != null)
            {
                var boundary = boundaryBody.GetNode<CollisionShape3D>($"Boundary_{direction}");
                if (boundary != null)
                {
                    boundary.QueueFree();
                }
            }
        }
        
        /// <summary>
        /// Elimina todas las barreras (usado cuando se descarga el chunk)
        /// </summary>
        public void RemoveAllBoundaries()
        {
            var boundaryBody = GetNode<StaticBody3D>("ChunkBoundaries");
            if (boundaryBody != null)
            {
                boundaryBody.QueueFree();
            }
        }
        
        /// <summary>
        /// Actualiza el mesh del terreno con los datos de altura
        /// </summary>
        private void UpdateTerrainMesh()
        {
            if (_data == null) return;
            
            // Crear ArrayMesh para el terreno
            var arrayMesh = new ArrayMesh();
            var surfaceTool = new SurfaceTool();
            
            surfaceTool.Begin(Mesh.PrimitiveType.Triangles);
            surfaceTool.SetMaterial(_meshInstance.MaterialOverride);
            
            // Generar vértices y triángulos
            GenerateTerrainVertices(surfaceTool);
            
            surfaceTool.Index();
            surfaceTool.Commit(arrayMesh);
            
            // Asignar mesh
            _meshInstance.Mesh = arrayMesh;
            
            // Crear colisiones
            CreateCollisionShape(arrayMesh);
        }
        
        /// <summary>
        /// Genera los vértices del terreno
        /// </summary>
        private void GenerateTerrainVertices(SurfaceTool surfaceTool)
        {
            int size = _data.Size;
            
            for (int x = 0; x < size - 1; x++)
            {
                for (int z = 0; z < size - 1; z++)
                {
                    // Obtener alturas de los 4 vértices del quad
                    float h00 = _data.GetHeight(x, z);
                    float h10 = _data.GetHeight(x + 1, z);
                    float h01 = _data.GetHeight(x, z + 1);
                    float h11 = _data.GetHeight(x + 1, z + 1);
                    
                    // Posiciones de los vértices
                    Vector3 v00 = new Vector3(x * SCALE, h00, z * SCALE);
                    Vector3 v10 = new Vector3((x + 1) * SCALE, h10, z * SCALE);
                    Vector3 v01 = new Vector3(x * SCALE, h01, (z + 1) * SCALE);
                    Vector3 v11 = new Vector3((x + 1) * SCALE, h11, (z + 1) * SCALE);
                    
                    // Primer triángulo
                    surfaceTool.AddVertex(v00);
                    surfaceTool.AddVertex(v10);
                    surfaceTool.AddVertex(v01);
                    
                    // Segundo triángulo
                    surfaceTool.AddVertex(v10);
                    surfaceTool.AddVertex(v11);
                    surfaceTool.AddVertex(v01);
                }
            }
        }
        
        /// <summary>
        /// Crea la forma de colisión del terreno
        /// </summary>
        private void CreateCollisionShape(ArrayMesh mesh)
        {
            // Crear colisión convexa para el terreno
            var collisionShape = mesh.CreateTrimeshShape();
            _collisionShape.Shape = collisionShape;
        }
        
        /// <summary>
        /// Libera recursos del chunk
        /// </summary>
        public void Dispose()
        {
            QueueFree();
        }
    }
}
