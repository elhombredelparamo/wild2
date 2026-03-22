// -----------------------------------------------------------------------------
// Wild v2.0 - Gestor de Terreno Dinámico
// -----------------------------------------------------------------------------
// Carga y descarga chunks en torno a una posición de referencia (cámara/jugador).
// Emite la señal ChunkInicialListo cuando el primer lote de chunks termina.
// -----------------------------------------------------------------------------
using System.IO;
using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;
using Wild.Core.Biomes;
using Wild.Data;
using Wild.Utils;
using System.Linq;
using Wild.Core.Quality;

namespace Wild.Core.Terrain
{
    public partial class TerrainManager : Node3D
    {
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

        private readonly Dictionary<Vector3, StaticBody3D> _activeColliders = new();
        private const float CollisionRadius = 20.0f;
        private const float CollisionCleanupRadius = 25.0f;


        private Vector3 _lastUpdatePos = new Vector3(float.MinValue, 0, float.MinValue);
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

        public override void _Ready()
        {
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

        private async void UpdateDynamicCollisions(Vector3 playerPos)
        {
            if (_collisionUpdateInProgress) return;
            _collisionUpdateInProgress = true;

            // CAPTURAR una foto de las coordenadas actuales de forma segura
            var activeCoords = _chunks.Keys.ToArray();

            var treesToActivate = await Task.Run(() => {
                var list = new List<VegetationInstance>();
                foreach (var coord in activeCoords)
                {
                    if (((Vector2)(coord - _chunkCentral)).Length() > 2.2f) continue;

                    if (_vegCache.Contains(coord))
                    {
                        var treeList = _vegCache.Get(coord);
                        foreach (var tree in treeList)
                        {
                            if (tree.Position.DistanceTo(playerPos) < CollisionRadius)
                                list.Add(tree);
                        }
                    }
                }
                return list;
            });

            foreach (var tree in treesToActivate)
            {
                if (!_activeColliders.ContainsKey(tree.Position))
                    SpawnTreeCollision(tree);
            }

            var toRemove = new List<Vector3>();
            foreach (var kvp in _activeColliders)
            {
                if (kvp.Key.DistanceTo(playerPos) > CollisionCleanupRadius)
                {
                    kvp.Value.QueueFree();
                    toRemove.Add(kvp.Key);
                }
            }
            foreach (var key in toRemove) _activeColliders.Remove(key);

            _collisionUpdateInProgress = false;
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
            _cts.Cancel();
            _cts.Dispose();
            _cts = new System.Threading.CancellationTokenSource();
        }

        public override void _ExitTree()
        {
            SessionData.Instance.OnSettingsApplied -= ApplyRenderSettings;
            QualityManager.OnVegetationQualityChanged -= ForceFullReload;
            QualityManager.OnTerrainQualityChanged -= ForceFullReload;
            Cleanup();
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
            
            List<VegetationInstance> objectsToSpawn;
            if (_vegCache.Contains(coord))
            {
                objectsToSpawn = _vegCache.Get(coord);
                // Si está en cache, aún necesitamos renderizarlo visualmente
                await RenderVegetationFromList(renderer, objectsToSpawn, coord);
            }
            else
            {
                objectsToSpawn = await _vegetationSpawner.SpawnForChunk(renderer, data, coord);
                _vegCache.Add(coord, objectsToSpawn);
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
    }
}
