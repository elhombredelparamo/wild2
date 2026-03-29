// -----------------------------------------------------------------------------
// Wild v2.0 - Spawner de Vegetación por Chunk (Optimización Asíncrona)
// -----------------------------------------------------------------------------
using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;
using Wild.Core.Biomes;
using Wild.Core.Quality;
using Wild.Data;
using Wild.Utils;

namespace Wild.Core.Terrain
{
    public class VegetationSpawner
    {
        private readonly int    _seed;
        private readonly BiomaManager _biomaManager;

        private const long SeedMixX     = 73856093L;
        private const long SeedMixZ     = 19349663L;
        private const long SeedMixEntry = 83492791L;

        public VegetationSpawner(int seed, BiomaManager biomaManager)
        {
            _seed         = seed;
            _biomaManager = biomaManager;
        }

        /// <summary>
        /// Genera y renderiza toda la vegetación de un chunk (Árboles y Plantas).
        /// Retorna la lista de instancias para que el TerrainManager las use en colisiones dinámicas.
        /// </summary>
        public async Task<List<VegetationInstance>> SpawnForChunk(ChunkRenderer renderer, ChunkData data, Vector2I coord)
        {
            int vertexCount = ChunkData.Resolution;
            int chunkSize   = ChunkData.Size;

            float densityMultiplier = QualityManager.Instance.Settings.VegetationQuality switch
            {
                QualityLevel.Ultra   => 1.0f,
                QualityLevel.High    => 0.75f,
                QualityLevel.Medium  => 0.5f,
                QualityLevel.Low     => 0.25f,
                QualityLevel.Toaster => 0.1f,
                _ => 0.0f
            };

            if (densityMultiplier <= 0f || !GodotObject.IsInstanceValid(renderer)) return new();

            // PRE-CARGA de modelos en el HILO PRINCIPAL
            var biomaBase = _biomaManager.GetBiomaAt(coord.X * chunkSize, coord.Y * chunkSize);
            if (biomaBase.VegetationEntries != null && biomaBase.VegetationEntries.Length > 0)
            {
                var paths = new List<string>();
                foreach (var e in biomaBase.VegetationEntries) 
                    if (!string.IsNullOrEmpty(e.ModelPath)) paths.Add(e.ModelPath);
                
                VegetationLibrary.Preload(paths.ToArray());
            }
            
            // ── 1. Cálculo de Posiciones (Segundo Plano) ─────────────────────
            var instances = await Task.Run(() =>
            {
                var list = new List<VegetationInstance>();
                var rng = new RandomNumberGenerator();

                int indexCounter = 0;
                for (int z = 0; z < vertexCount; z++)
                for (int x = 0; x < vertexCount; x++)
                {
                    float worldX = (coord.X * chunkSize) + x;
                    float worldZ = (coord.Y * chunkSize) + z;
                    float height = data.Altitudes[(z * vertexCount) + x];
                    
                    BiomaType localBioma = _biomaManager.GetBiomaAt(worldX, worldZ);
                    var entries = localBioma.VegetationEntries;

                    if (entries != null)
                    {
                        for (int i = 0; i < entries.Length; i++)
                        {
                            var entry = entries[i];
                            rng.Seed = (ulong)(_seed + (long)worldX * SeedMixX + (long)worldZ * SeedMixZ + (long)i * SeedMixEntry);
                            
                            if (rng.Randf() < entry.SpawnChance * densityMultiplier)
                            {
                                list.Add(new VegetationInstance {
                                    Index = indexCounter,
                                    ItemId = entry.ItemId,
                                    ModelPath = entry.ModelPath,
                                    Position = new Vector3(worldX, height, worldZ),
                                    RotationY = rng.Randf() * Mathf.Pi * 2.0f,
                                    Scale = rng.RandfRange(entry.MinScale, entry.MaxScale),
                                    HasCollision = entry.HasCollision
                                });
                            }
                            indexCounter++;
                        }
                    }
                }
                return list;
            });

            return instances;
        }
    }
}
