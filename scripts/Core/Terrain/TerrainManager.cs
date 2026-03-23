// -----------------------------------------------------------------------------
// Wild v2.0 - Gestor de Terreno Dinámico
// -----------------------------------------------------------------------------
// Carga y descarga chunks en torno a una posición de referencia (cámara/jugador).
// Emite la señal ChunkInicialListo cuando el primer lote de chunks termina.
// -----------------------------------------------------------------------------
using System;
using System.IO;
using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;
using Wild.Core.Biomes;
using Wild.Data;
using Wild.Core.Deployables;
using Wild.Utils;
using System.Linq;
using Wild.Core.Quality;
using System.Text.Json;

namespace Wild.Core.Terrain
{
    public partial class TerrainManager : Node3D
    {
        public static TerrainManager Instance { get; private set; }

        [Export] public int LoadRadius  = 5;   
        [Export] public int UnloadRadius = 8;   
        [Export] public int ChunkSize   = 10;   

        [Signal] public delegate void ChunkInicialListoEventHandler();

        private readonly Dictionary<Vector2I, ChunkRenderer> _chunks = new();
        private readonly HashSet<Vector2I>  _cargando = new();
        private readonly Dictionary<Vector2I, StaticBody3D> _murosActivos = new();
        private readonly Queue<Vector2I> _queueCarga = new();
        private readonly Queue<Vector2I> _queueDescarga = new();
        
        private readonly LRUCache<Vector2I, (Mesh mesh, Vector3[] faces)> _meshCache = new(200);
        private readonly LRUCache<Vector2I, List<VegetationInstance>> _vegCache = new(200);

        private readonly Dictionary<Vector3, Node3D> _activeColliders = new();
        private readonly Dictionary<Vector2I, HashSet<int>> _removedVegetation = new();
        private const float CollisionRadius = 20.0f;
        private const float CollisionCleanupRadius = 25.0f;


        private Vector3 _lastUpdatePos = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        private const float UpdateThreshold = 2.0f; 
        private bool _procesandoCola = false;
        private bool _procesandoDescarga = false;
        private bool _collisionUpdateInProgress = false;

        private TerrainGenerator _generator;
        private MaterialCache    _materialCache;
        private BiomaManager     _biomaManager;
        private ModelSpawner       _modelSpawner;
        private VegetationSpawner  _vegetationSpawner;
        private System.Threading.CancellationTokenSource _cts = new();

        private int _seed;

        private Vector2I _chunkCentral = new Vector2I(int.MinValue, int.MinValue);
        private bool     _primeraOlaDespachada = false;
        private int      _chunksInicialPendientes = 0;

        public System.Collections.Generic.Dictionary<Vector3, Node3D> GetActiveCollidersForDebug() { return _activeColliders; }

        public override void _Ready()
        {
            Instance = this;
            _seed = 12345;
            var mundoActual = MundoManager.Instance.ObtenerMundoActual();
            if (mundoActual != null) _seed = mundoActual.GetSeedInt();

            var noise = new NoiseGenerator(_seed);
            _biomaManager  = new BiomaManager(_seed);
            _generator     = new TerrainGenerator(noise, _biomaManager);
            _materialCache      = new MaterialCache();
            _modelSpawner       = new ModelSpawner(this);
            _vegetationSpawner  = new VegetationSpawner(_seed, _biomaManager);
            
            SessionData.Instance.OnSettingsApplied += ApplyRenderSettings;
            ApplyRenderSettings(); 

            QualityManager.OnVegetationQualityChanged += ForceFullReload;
            QualityManager.OnTerrainQualityChanged += ForceFullReload;
            QualityManager.OnDeployableQualityChanged += ForceFullReload;
        }

        private void ApplyRenderSettings()
        {
            int distance = SessionData.Instance.RenderDistance;
            LoadRadius = Mathf.Clamp(distance / 10, 1, 10);
            UnloadRadius = LoadRadius + 2;
            
            if (_chunkCentral != new Vector2I(int.MinValue, int.MinValue))
            {
                CargarChunksNuevos();
                DescargarChunksLejanos();
            }
        }

        public void Update(Vector3 refPos)
        {
            if (refPos.DistanceTo(_lastUpdatePos) < UpdateThreshold)
                return;

            _lastUpdatePos = refPos;
            _chunkCentral = WorldToChunk(refPos);

            CargarChunksNuevos();
            DescargarChunksLejanos();
            UpdateDynamicCollisions(refPos);
        }

        /// <summary>
        /// Fuerza una actualización de colisiones ignorando el threshold de posición.
        /// Se llama cuando los chunks acaban de cargar por primera vez.
        /// </summary>
        public void ForceCollisionUpdate(Vector3 refPos)
        {
            _lastUpdatePos = refPos; // Para que la siguiente llamada normal también funcione
            UpdateDynamicCollisions(refPos);
            Logger.LogInfo("TERRAIN: ForceCollisionUpdate ejecutado en spawn.");
        }

        private async void UpdateDynamicCollisions(Vector3 playerPos)
        {
            if (_collisionUpdateInProgress) return;
            _collisionUpdateInProgress = true;

            const float InteractionRadius = 8.0f;

            // CAPTURAR una foto de las coordenadas actuales de forma segura
            var activeCoords = _chunks.Keys.ToArray();

            var vegToActivate = await Task.Run(() => {
                var list = new List<VegetationInstance>();
                foreach (var coord in activeCoords)
                {
                    // Solo chunks muy cercanos
                    if ((coord - _chunkCentral).Length() > 2.5f) continue;

                    if (_vegCache.Contains(coord))
                    {
                        var vegList = _vegCache.Get(coord);
                        foreach (var veg in vegList)
                        {
                            float dist = veg.Position.DistanceTo(playerPos);
                            
                            // Árboles: usar CollisionRadius (20m)
                            bool isCollectible = !string.IsNullOrEmpty(veg.ItemId);
                            if (isCollectible)
                            {
                                if (dist < InteractionRadius)
                                {
                                    list.Add(veg);
                                }
                            }
                            else if (dist < CollisionRadius) // Árboles
                            {
                                list.Add(veg);
                            }
                        }
                    }
                }
                return list;
            });

            foreach (var veg in vegToActivate)
            {
                if (!_activeColliders.ContainsKey(veg.Position))
                {
                    // B) Objetos recolectables (basado en ItemId)
                    if (!string.IsNullOrEmpty(veg.ItemId))
                    {
                        SpawnCollectibleTrigger(veg);
                    }
                    else
                    {
                        // Árboles u otros obstáculos estáticos
                        SpawnTreeCollision(veg);
                    }
                }
            }

            var toRemove = new List<Vector3>();
            foreach (var kvp in _activeColliders)
            {
                float dist = kvp.Key.DistanceTo(playerPos);
                bool isCollectible = kvp.Value is Area3D; // Collectibles are Area3D
                float currentCleanup = isCollectible ? InteractionRadius + 2.0f : CollisionCleanupRadius;

                if (dist > currentCleanup)
                {
                    kvp.Value.QueueFree();
                    toRemove.Add(kvp.Key);
                }
            }
            int removedCount = toRemove.Count;
            foreach (var key in toRemove) _activeColliders.Remove(key);

            if (vegToActivate.Count > 0 || removedCount > 0)
            {
                Logger.LogDebug($"TERRAIN: UpdateDynamicCollisions finalizado. Triggers activos: {_activeColliders.Count}");
            }

            _collisionUpdateInProgress = false;
        }

        private void SpawnCollectibleTrigger(VegetationInstance veg)
        {
            if (_activeColliders.ContainsKey(veg.Position)) return;

            var area = new Area3D();
            area.Position = veg.Position + Vector3.Up * 0.5f; // Elevado un poco para facilitar interacción

            var shape = new CollisionShape3D();
            var sphere = new SphereShape3D { Radius = 1.0f }; // Radio de interacción ajustable
            shape.Shape = sphere;
            area.AddChild(shape);

            // Metadatos para el raycast del jugador
            area.SetMeta("item_id", veg.ItemId);

            // Capas de colisión para que el raycast lo vea (Capa 4: Interacciones)
            area.CollisionLayer = 1 << 3; 
            area.CollisionMask = 0;
            area.Monitoring = false;
            area.Monitorable = true;

            AddChild(area);
            _activeColliders[veg.Position] = area;
            Logger.LogInfo($"TERRAIN: Trigger de '{veg.ItemId}' ACTIVADO en {veg.Position}");
        }

        private void SpawnTreeCollision(VegetationInstance tree)
        {
            var staticBody = new StaticBody3D();
            AddChild(staticBody);
            staticBody.GlobalPosition = tree.Position;
            staticBody.RotateY(tree.RotationY);
            staticBody.Scale = new Vector3(tree.Scale, tree.Scale, tree.Scale);

            var cachedShapes = VegetationLibrary.GetCollisionShapes(tree.ModelPath);
            foreach (var shapeData in cachedShapes)
            {
                var colShape = new CollisionShape3D();
                colShape.Shape = shapeData.shape;
                // APLICAR TRANSFORMACION LOCAL (Igual que en MultiMesh)
                colShape.Transform = shapeData.localTransform;
                staticBody.AddChild(colShape);
            }
            _activeColliders[tree.Position] = staticBody;
        }

        // ── Carga ─────────────────────────────────────────────────────────────

        private void CargarChunksNuevos()
        {
            var primeraOla = new List<Vector2I>();
            for (int x = -LoadRadius; x <= LoadRadius; x++)
            for (int z = -LoadRadius; z <= LoadRadius; z++)
            {
                var coord = new Vector2I(_chunkCentral.X + x, _chunkCentral.Y + z);
                if (!_chunks.ContainsKey(coord) && !_cargando.Contains(coord) && !_queueCarga.Contains(coord))
                {
                    _queueCarga.Enqueue(coord);
                    if (!_primeraOlaDespachada) primeraOla.Add(coord);
                }
            }

            if (!_primeraOlaDespachada && primeraOla.Count > 0)
            {
                _primeraOlaDespachada = true;
                _chunksInicialPendientes = primeraOla.Count;
                _ = ProcesarColaCarga();
            }
            else if (!_procesandoCola && _queueCarga.Count > 0)
            {
                _ = ProcesarColaCarga();
            }
        }

        private async Task ProcesarColaCarga()
        {
            _procesandoCola = true;
            var activeTasks = new List<Task>();
            int maxParallel = 4;

            while (_queueCarga.Count > 0 || activeTasks.Count > 0)
            {
                if (_cts.Token.IsCancellationRequested) break;
                while (_queueCarga.Count > 0 && activeTasks.Count < maxParallel)
                {
                    var coord = _queueCarga.Dequeue();
                    activeTasks.Add(CargarChunkAsync(coord, _cts.Token));
                }
                if (activeTasks.Count > 0)
                {
                    var completed = await Task.WhenAny(activeTasks);
                    activeTasks.Remove(completed);
                }
                await Task.Yield();
            }
            _procesandoCola = false;
        }

        private async Task CargarChunkAsync(Vector2I coord, System.Threading.CancellationToken ct)
        {
            if (ct.IsCancellationRequested) return;
            _cargando.Add(coord);
            CallDeferred(nameof(CrearMuroSeguridad), coord);

            string chunksDir = MundoManager.Instance.ObtenerRutaChunksActual();
            string chunkFile = Path.Combine(chunksDir, $"{coord.X}_{coord.Y}.dat");
            ChunkData data = null;

            if (File.Exists(chunkFile))
            {
                byte[] bin = await Task.Run(() => File.ReadAllBytes(chunkFile), ct);
                data = ChunkData.FromBinary(bin);
            }

            if (data == null)
            {
                await Task.Run(() => {
                    data = _generator.GenerateChunkData(coord);
                    File.WriteAllBytes(chunkFile, data.ToBinary());
                }, ct);
            }

            var renderer = new ChunkRenderer();
            AddChild(renderer);
            renderer.Initialize(coord, _generator, _materialCache);

            Mesh meshData = null;
            Vector3[] faces = null;

            if (_meshCache.Contains(coord))
            {
                var cached = _meshCache.Get(coord);
                meshData = cached.mesh;
                faces = cached.faces;
            }
            else
            {
                await Task.Run(() => {
                    meshData = _generator.CreateHighResMesh(coord, data);
                    faces = meshData.GetFaces();
                }, ct);
                _meshCache.Add(coord, (meshData, faces));
            }

            renderer.CallDeferred(nameof(ChunkRenderer.UpdateMeshWithHeightMap), meshData, data.Altitudes);
            CallDeferred(nameof(PopulateChunk), renderer, data, coord);
            
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            _chunks[coord] = renderer;
            _cargando.Remove(coord);
            CallDeferred(nameof(EliminarMuroSeguridad), coord);

            if (_chunksInicialPendientes > 0)
            {
                _chunksInicialPendientes--;
                if (_chunksInicialPendientes == 0) EmitSignal(SignalName.ChunkInicialListo);
            }
        }

        public void Cleanup()
        {
            Logger.LogInfo("TERRAIN: Iniciando Cleanup. Guardando estado de todos los chunks activos...");
            foreach (var coord in _chunks.Keys.ToArray())
            {
                SaveChunkState(coord);
            }

            _cts.Cancel();
            _cts.Dispose();
            _cts = new System.Threading.CancellationTokenSource();
        }

        public override void _ExitTree()
        {
            SessionData.Instance.OnSettingsApplied -= ApplyRenderSettings;
            QualityManager.OnVegetationQualityChanged -= ForceFullReload;
            QualityManager.OnTerrainQualityChanged -= ForceFullReload;
            QualityManager.OnDeployableQualityChanged -= ForceFullReload;
            Cleanup();
            if (Instance == this) Instance = null;
        }

        private void ForceFullReload()
        {
            _meshCache.Clear();
            _vegCache.Clear();
            Cleanup();
            foreach (var renderer in _chunks.Values) renderer.QueueFree();
            _chunks.Clear();
            _cargando.Clear();
            _queueCarga.Clear();
            _queueDescarga.Clear();
            foreach (var collider in _activeColliders.Values) collider.QueueFree();
            _activeColliders.Clear();
            _removedVegetation.Clear();
            _lastUpdatePos = new Vector3(float.MinValue, 0, float.MinValue);
        }

        private void DescargarChunksLejanos()
        {
            foreach (var coord in _chunks.Keys)
            {
                if (_queueDescarga.Contains(coord)) continue;
                int dx = Mathf.Abs(coord.X - _chunkCentral.X);
                int dz = Mathf.Abs(coord.Y - _chunkCentral.Y);
                if (dx > UnloadRadius || dz > UnloadRadius) _queueDescarga.Enqueue(coord);
            }
            if (!_procesandoDescarga && _queueDescarga.Count > 0) _ = ProcesarColaDescarga();
        }

        private async Task ProcesarColaDescarga()
        {
            _procesandoDescarga = true;
            while (_queueDescarga.Count > 0)
            {
                if (_cts.Token.IsCancellationRequested) break;
                var coord = _queueDescarga.Dequeue();
                if (_chunks.TryGetValue(coord, out var renderer))
                {
                    SaveChunkState(coord);
                    renderer.QueueFree();
                    _chunks.Remove(coord);
                }
                await Task.Yield();
            }
            _procesandoDescarga = false;
        }

        /// <summary>
        /// Pueblo un chunk con vegetación usando el VegetationSpawner.
        /// </summary>
        private async void PopulateChunk(ChunkRenderer renderer, ChunkData data, Vector2I coord)
        {
            if (!GodotObject.IsInstanceValid(renderer)) return;
            
            // Intentar cargar JSON del chunk si no se ha cargado antes
            string chunkFileName = $"chunk_{coord.X}_{coord.Y}.json";
            string objectsDir = MundoManager.Instance.ObtenerRutaObjetosActual();
            if (!string.IsNullOrEmpty(objectsDir))
            {
                string filePath = Path.Combine(objectsDir, chunkFileName);
                if (File.Exists(filePath))
                {
                    try
                    {
                        var json = File.ReadAllText(filePath);
                        var chunkState = JsonSerializer.Deserialize<ChunkStateData>(json);
                        
                        if (chunkState != null)
                        {
                            // 1. Cargar Vegetación Eliminada
                            if (chunkState.RemovedVegetationIndices != null)
                            {
                                if (!_removedVegetation.ContainsKey(coord)) 
                                    _removedVegetation[coord] = new HashSet<int>();

                                foreach (var index in chunkState.RemovedVegetationIndices)
                                {
                                    _removedVegetation[coord].Add(index);
                                }
                            }

                            // 2. Cargar Deployables (Cofres, etc.)
                            if (chunkState.AddedDeployables != null && chunkState.AddedDeployables.Count > 0)
                            {
                                Logger.LogInfo($"TERRAIN: Cargando {chunkState.AddedDeployables.Count} deployables para chunk {coord}");
                                foreach (var dData in chunkState.AddedDeployables)
                                {
                                    CreateDeployableNode(renderer, dData, coord);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"TERRAIN: Error leyendo {chunkFileName}: {ex.Message}");
                    }
                }
            }

            List<VegetationInstance> objectsToSpawn;
            if (_vegCache.Contains(coord))
            {
                objectsToSpawn = _vegCache.Get(coord);
                await RenderVegetationFromList(renderer, objectsToSpawn, coord);
            }
            else
            {
                var rawInstances = await _vegetationSpawner.SpawnForChunk(renderer, data, coord);
                
                // Filtrar usando los índices removidos para este chunk
                if (_removedVegetation.TryGetValue(coord, out var removedIndices))
                {
                    objectsToSpawn = rawInstances.FindAll(inst => !removedIndices.Contains(inst.Index));
                }
                else
                {
                    objectsToSpawn = rawInstances;
                }

                _vegCache.Add(coord, objectsToSpawn);
                await RenderVegetationFromList(renderer, objectsToSpawn, coord);
            }
        }

        /// <summary>
        /// Renderiza una lista de vegetación (usualmente desde cache) usando MultiMesh.
        /// </summary>
        private async Task RenderVegetationFromList(ChunkRenderer renderer, List<VegetationInstance> instances, Vector2I coord)
        {
            if (instances.Count == 0 || !GodotObject.IsInstanceValid(renderer)) return;

            await Task.Run(() => {
                var grouped = new Dictionary<string, List<VegetationInstance>>();
                foreach (var inst in instances)
                {
                    if (!grouped.ContainsKey(inst.ModelPath)) grouped[inst.ModelPath] = new();
                    grouped[inst.ModelPath].Add(inst);
                }

                foreach (var entry in grouped)
                {
                    var visualData = VegetationLibrary.GetVisualMeshes(entry.Key);
                    if (visualData == null) continue;

                    foreach (var meshMat in visualData)
                    {
                        var transforms = new Transform3D[entry.Value.Count];
                        for (int i = 0; i < entry.Value.Count; i++)
                        {
                            var inst = entry.Value[i];
                            // CORRECCIÓN COORDINADAS: mundo -> local del chunk
                            float localX = inst.Position.X - (coord.X * ChunkSize);
                            float localZ = inst.Position.Z - (coord.Y * ChunkSize);
                            
                            var localTransform = new Transform3D(
                                Basis.Identity.Rotated(Vector3.Up, inst.RotationY).Scaled(Vector3.One * inst.Scale),
                                new Vector3(localX, inst.Position.Y, localZ)
                            );
                            transforms[i] = localTransform * meshMat.localTransform;
                        }
                        
                        var m = meshMat.mesh;
                        var mat = meshMat.material;
                        Callable.From(() => {
                            if (!GodotObject.IsInstanceValid(renderer)) return;
                            var mmInst = new MultiMeshInstance3D();
                            var mm = new MultiMesh { 
                                TransformFormat = MultiMesh.TransformFormatEnum.Transform3D, 
                                Mesh = m, 
                                InstanceCount = transforms.Length 
                            };
                            for (int i = 0; i < transforms.Length; i++) mm.SetInstanceTransform(i, transforms[i]);
                            mmInst.Multimesh = mm;
                            if (mat != null) mmInst.MaterialOverride = mat;
                            renderer.AddChild(mmInst);
                        }).CallDeferred();
                    }
                }
            });
        }

        private Vector2I WorldToChunk(Vector3 worldPos) => new Vector2I(Mathf.FloorToInt(worldPos.X / ChunkSize), Mathf.FloorToInt(worldPos.Z / ChunkSize));

        private void CrearMuroSeguridad(Vector2I coord)
        {
            if (_murosActivos.ContainsKey(coord)) return;
            var muro = new StaticBody3D { GlobalPosition = new Vector3(coord.X * ChunkSize + ChunkSize / 2f, 450, coord.Y * ChunkSize + ChunkSize / 2f) };
            var shape = new CollisionShape3D { Shape = new BoxShape3D { Size = new Vector3(ChunkSize, 1100, ChunkSize) } };
            muro.AddChild(shape);
            AddChild(muro);
            _murosActivos[coord] = muro;
        }

        private void EliminarMuroSeguridad(Vector2I coord)
        {
            if (_murosActivos.TryGetValue(coord, out var muro)) { muro.QueueFree(); _murosActivos.Remove(coord); }
        }

        public float GetHeightAt(float x, float z) => _generator?.GetNoiseHeight(x, z) ?? 0f;

        public void RemoveVegetationAt(Vector3 globalPos, string keyword)
        {
            var chunkCoord = WorldToChunk(globalPos);

            if (_vegCache.Contains(chunkCoord))
            {
                var list = _vegCache.Get(chunkCoord);
                
                // Buscar la instancia más cercana que contenga la palabra clave
                int bestIndex = -1;
                float bestDist = float.MaxValue;

                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].ModelPath.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    {
                        float d = list[i].Position.DistanceTo(globalPos);
                        if (d < bestDist && d < 2.0f) // Tolerancia razonable
                        {
                            bestDist = d;
                            bestIndex = i;
                        }
                    }
                }

                if (bestIndex != -1)
                {
                    var veg = list[bestIndex];
                    list.RemoveAt(bestIndex);
                    
                    // Añadir al set del chunk y guardar persistencia
                    if (!_removedVegetation.ContainsKey(chunkCoord))
                        _removedVegetation[chunkCoord] = new HashSet<int>();
                    
                    _removedVegetation[chunkCoord].Add(veg.Index);
                    SaveChunkState(chunkCoord);

                    // Limpiar colisiones activas (Búsqueda por proximidad para evitar fallos de precisión float)
                    Vector3 bestColliderKey = Vector3.Zero;
                    float bestColDist = float.MaxValue;
                    foreach (var key in _activeColliders.Keys)
                    {
                        float d = key.DistanceTo(veg.Position);
                        if (d < bestColDist)
                        {
                            bestColDist = d;
                            bestColliderKey = key;
                        }
                    }

                    if (bestColDist < 0.5f)
                    {
                        var collider = _activeColliders[bestColliderKey];
                        if (GodotObject.IsInstanceValid(collider)) collider.QueueFree();
                        _activeColliders.Remove(bestColliderKey);
                        Logger.LogInfo($"TERRAIN: Collider de '{keyword}' detectado y eliminado en {bestColliderKey} (distancia: {bestColDist})");
                    }
                    else
                    {
                        Logger.LogWarning($"TERRAIN: No se encontró collider activo cerca de {veg.Position} (mejor distancia: {bestColDist}) para remover.");
                    }

                    // Reconstruir visuales del chunk
                    if (_chunks.TryGetValue(chunkCoord, out var renderer))
                    {
                        foreach (Node child in renderer.GetChildren())
                        {
                            if (child is MultiMeshInstance3D) child.QueueFree();
                        }
                        
                        _ = RenderVegetationFromList(renderer, list, chunkCoord);
                        Logger.LogInfo($"TERRAIN: Vegetación ({keyword}) removida exitosamente en {veg.Position}");
                    }
                }
                else
                {
                    Logger.LogWarning($"TERRAIN: No se encontró vegetación cercana a {globalPos} para remover.");
                }
            }
        }

        public void AddDeployable(string typeId, Vector3 globalPos, Vector3 rotation)
        {
            var coord = WorldToChunk(globalPos);
            if (_chunks.TryGetValue(coord, out var renderer))
            {
                Vector3 localPos = globalPos - new Vector3(coord.X * ChunkSize, 0, coord.Y * ChunkSize);
                var dData = new DeployableData {
                    TypeId = typeId,
                    Position = new SerializableVector3(localPos),
                    Rotation = new SerializableVector3(rotation)
                };
                CreateDeployableNode(renderer, dData, coord);
                SaveChunkState(coord);
                Logger.LogInfo($"TERRAIN: Deployable '{typeId}' spawneado manualmente en {globalPos} (Local: {localPos})");
            }
            else
            {
                Logger.LogWarning($"TERRAIN: No se puede spawnear {typeId} en {coord} porque el chunk no está cargado (Cerca del jugador).");
            }
        }

        public void SaveChunkState(Vector2I coord)
        {
            try
            {
                bool hasRemovedVeg = _removedVegetation.TryGetValue(coord, out var removedInChunk);
                
                var chunkState = new ChunkStateData();
                if (hasRemovedVeg) chunkState.RemovedVegetationIndices = removedInChunk.ToList();

                // Recopilar deployables activos en el chunk
                if (_chunks.TryGetValue(coord, out var renderer))
                {
                    foreach (Node child in renderer.GetChildren())
                    {
                        if (child is DeployableBase deployable)
                        {
                            chunkState.AddedDeployables.Add(new DeployableData
                            {
                                TypeId = deployable.TypeId,
                                Position = new SerializableVector3(deployable.Position), // Usar posicion LOCAL al chunk
                                Rotation = new SerializableVector3(deployable.Rotation),
                                CustomData = deployable.SaveData()
                            });
                        }
                    }
                }

                // Si no hay nada que guardar, NO borramos el archivo si existía (podría haber otros datos)
                // Pero en nuestro sistema, este JSON es el ÚNICO lugar para veg eliminada y deployables.
                if (chunkState.RemovedVegetationIndices.Count == 0 && chunkState.AddedDeployables.Count == 0)
                {
                    // Si el archivo existía, quizás deberíamos borrarlo para no cargar basura después?
                    // Por ahora simplemente no hacemos nada para evitar IO innecesario.
                    return;
                }

                var json = JsonSerializer.Serialize(chunkState, new JsonSerializerOptions { WriteIndented = true });
                string fileName = $"chunk_{coord.X}_{coord.Y}.json";
                
                Logger.LogInfo($"TERRAIN: Guardando estado del chunk {coord} (Veg: {chunkState.RemovedVegetationIndices.Count}, Dep: {chunkState.AddedDeployables.Count})");
                WorldObjectRegistrar.RegistrarObjeto(fileName, json);
            }
            catch (Exception ex)
            {
                Logger.LogError($"TERRAIN: Error al guardar estado del chunk {coord} -> {ex.Message}");
            }
        }
        private void CreateDeployableNode(ChunkRenderer renderer, DeployableData dData, Vector2I coord)
        {
            try
            {
                DeployableBase node = null;
                string modelPath = "";

                if (dData.TypeId == "cofre1")
                {
                    node = new CofreDeployable();
                    var quality = QualityManager.Instance.Settings.DeployableQuality.ToString().ToLower();
                    modelPath = $"res://assets/models/deploy/cofre/1/{quality}/cofreCesta1.glb";
                }

                if (node == null) return;

                node.TypeId = dData.TypeId;
                node.ChunkCoord = coord;
                node.Position = dData.Position.ToVector3();
                node.Rotation = dData.Rotation.ToVector3();
                
                if (!string.IsNullOrEmpty(modelPath) && ResourceLoader.Exists(modelPath))
                {
                    var scene = ResourceLoader.Load<PackedScene>(modelPath);
                    var mesh = scene.Instantiate();
                    node.AddChild(mesh);

                    Logger.LogDebug($"TERRAIN: Estructura del modelo '{dData.TypeId}':");
                    LogNodeTree(mesh, "  ");

                    // Configurar Capa de Colisión para interacción (Capa 4) y física (Capa 1)
                    int bodiesFound = SetCollisionLayerRecursive(mesh, (1 << 0) | (1 << 3));

                    if (bodiesFound == 0)
                    {
                        Aabb aabb = CalculateAABBRecursive(mesh);
                        Logger.LogWarning($"TERRAIN: El modelo '{dData.TypeId}' no tiene colisionadores internos. Creando colisionador de caja basado en AABB ({aabb.Size}).");
                        
                        var staticBody = new StaticBody3D();
                        staticBody.CollisionLayer = (1 << 0) | (1 << 3);
                        
                        var colShape = new CollisionShape3D();
                        var box = new BoxShape3D { Size = aabb.Size };
                        colShape.Shape = box;
                        colShape.Position = aabb.Position + (aabb.Size * 0.5f); // Centro del AABB
                        
                        staticBody.AddChild(colShape);
                        node.AddChild(staticBody);
                    }
                }

                // Cargar datos específicos
                node.LoadData(dData.CustomData);

                // Añadir al chunk
                renderer.AddChild(node);
                
                // Asegurar que tenga colisión para interactuar
                // NOTA: Si el .glb no trae colisión, habría que añadirla aquí.
                // Usualmente los .glb de importación traen StaticBody si se configuró en Godot.
            }
            catch (Exception ex)
            {
                Logger.LogError($"TERRAIN: Error instanciando deployable {dData.TypeId} en {coord}: {ex.Message}");
            }
        }

        private int SetCollisionLayerRecursive(Node node, uint layer)
        {
            int found = 0;
            if (node is CollisionObject3D body)
            {
                Logger.LogInfo($"TERRAIN: Configurando colisión en '{node.Name}' (Tipo: {node.GetType().Name}) -> Layer {layer}");
                body.CollisionLayer = layer;
                found++;
            }

            foreach (Node child in node.GetChildren())
            {
                found += SetCollisionLayerRecursive(child, layer);
            }
            return found;
        }

        private void LogNodeTree(Node node, string indent)
        {
            Logger.LogDebug($"{indent}- {node.Name} ({node.GetType().Name})");
            foreach (Node child in node.GetChildren())
            {
                LogNodeTree(child, indent + "  ");
            }
        }

        private Aabb CalculateAABBRecursive(Node node)
        {
            Aabb totalAABB = new Aabb();
            bool first = true;

            void Walk(Node n, Transform3D accumulatedTransform)
            {
                Transform3D currentTransform = accumulatedTransform;
                if (n is Node3D n3d)
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
            
            // Si no se encontró nada, devolver un cubo mínimo
            if (first) return new Aabb(new Vector3(-0.5f, 0, -0.5f), new Vector3(1, 1, 1));
            
            return totalAABB;
        }
    }
}
