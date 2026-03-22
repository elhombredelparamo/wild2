using Godot;
using Wild.Core.Biomes;
using Wild.Core.Quality;

namespace Wild.Core.Terrain
{
    public class TerrainGenerator
    {
        private NoiseGenerator _noiseGenerator;
        private BiomaManager  _biomaManager;

        public TerrainGenerator(NoiseGenerator noiseGenerator, BiomaManager biomaManager)
        {
            _noiseGenerator = noiseGenerator;
            _biomaManager   = biomaManager;
        }

        public ChunkData GenerateChunkData(Vector2I chunkPos)
        {
            var data = new ChunkData();
            int chunkSize = ChunkData.Size;
            int vertexCount = ChunkData.Resolution;

            for (int z = 0; z < vertexCount; z++)
            {
                for (int x = 0; x < vertexCount; x++)
                {
                    float worldX = (chunkPos.X * chunkSize) + x;
                    float worldZ = (chunkPos.Y * chunkSize) + z;

                    // Calculamos la altura base global (continua entre chunks)
                    float height = _noiseGenerator.GetHeight(worldX, worldZ);
                    
                    // Obtenemos el bioma específico de este VÉRTICE para el blending
                    BiomaType localBioma = _biomaManager.GetBiomaAt(worldX, worldZ);
                    
                    // Añadimos una pequeña variación extra según el bioma
                    float noiseVal = _noiseGenerator.FractalNoise2D(worldX, worldZ, 4, 0.5f);
                    height += (noiseVal * localBioma.HeightVariation * 0.1f);

                    // Configuramos los pesos de mezcla en el color del vértice (Splat Map)
                    Color blendWeights = Color.FromHtml("#00000000"); // Base Oceano (0,0,0,0)
                    if (localBioma.Name == "Costa")      blendWeights = new Color(1, 0, 0, 0);
                    else if (localBioma.Name == "Pradera") blendWeights = new Color(0, 1, 0, 0);
                    else if (localBioma.Name == "Bosque")  blendWeights = new Color(0, 0, 1, 0);
                    else if (localBioma.Name == "Montana") blendWeights = new Color(0, 0, 0, 1);

                    int index = (z * vertexCount) + x;
                    data.Altitudes[index] = height;
                    data.BlendWeights[index] = blendWeights;
                }
            }

            return data;
        }

        public ArrayMesh CreateHighResMesh(Vector2I chunkPos, ChunkData data)
        {
            var st = new SurfaceTool();
            st.Begin(Mesh.PrimitiveType.Triangles);

            int baseSize = ChunkData.Size; // 10

            // Determinar resolución de salida según calidad
            QualityLevel quality = Quality.QualityManager.Instance?.Settings.TerrainQuality ?? QualityLevel.Medium;
            int segments = quality switch
            {
                QualityLevel.Toaster => 10,
                QualityLevel.Low => 15,
                QualityLevel.Medium => 20,
                QualityLevel.High => 30,
                QualityLevel.Ultra => 40,
                _ => 20
            };

            int vCount = segments + 1;
            float step = (float)baseSize / segments;

            for (int z = 0; z < vCount; z++)
            {
                for (int x = 0; x < vCount; x++)
                {
                    float localX = x * step;
                    float localZ = z * step;
                    float worldX = (chunkPos.X * baseSize) + localX;
                    float worldZ = (chunkPos.Y * baseSize) + localZ;

                    // Muestrear altura exacta del ruido (Teselación real)
                    float height = GetNoiseHeight(worldX, worldZ);

                    // Interpolar pesos de bioma del ChunkData (Bilineal)
                    Color weights = GetInterpolatedWeights(data, localX / baseSize, localZ / baseSize);

                    st.SetUV(new Vector2(localX / baseSize, localZ / baseSize));
                    st.SetColor(weights);
                    st.AddVertex(new Vector3(localX, height, localZ));
                }
            }

            for (int z = 0; z < segments; z++)
            {
                for (int x = 0; x < segments; x++)
                {
                    int topLeft = (z * vCount) + x;
                    int topRight = topLeft + 1;
                    int bottomLeft = ((z + 1) * vCount) + x;
                    int bottomRight = bottomLeft + 1;

                    st.AddIndex(topLeft);
                    st.AddIndex(topRight);
                    st.AddIndex(bottomLeft);

                    st.AddIndex(topRight);
                    st.AddIndex(bottomRight);
                    st.AddIndex(bottomLeft);
                }
            }

            st.GenerateNormals();
            return st.Commit();
        }

        private Color GetInterpolatedWeights(ChunkData data, float u, float v)
        {
            int res = ChunkData.Resolution;
            float x = u * (res - 1);
            float z = v * (res - 1);
            int x0 = Mathf.Clamp((int)x, 0, res - 2);
            int x1 = x0 + 1;
            int z0 = Mathf.Clamp((int)z, 0, res - 2);
            int z1 = z0 + 1;

            float tx = x - x0;
            float tz = z - z0;

            Color c00 = data.BlendWeights[z0 * res + x0];
            Color c10 = data.BlendWeights[z0 * res + x1];
            Color c01 = data.BlendWeights[z1 * res + x0];
            Color c11 = data.BlendWeights[z1 * res + x1];

            Color top = c00.Lerp(c10, tx);
            Color bottom = c01.Lerp(c11, tx);
            return top.Lerp(bottom, tz);
        }

        public float GetNoiseHeight(float worldX, float worldZ)
        {
            float height = _noiseGenerator.GetHeight(worldX, worldZ);
            BiomaType localBioma = _biomaManager.GetBiomaAt(worldX, worldZ);
            float noiseVal = _noiseGenerator.FractalNoise2D(worldX, worldZ, 4, 0.5f);
            height += (noiseVal * localBioma.HeightVariation * 0.1f);
            return height;
        }

        public ArrayMesh GenerateChunkMesh(Vector2I chunkPos)
        {
            var data = GenerateChunkData(chunkPos);
            return CreateHighResMesh(chunkPos, data);
        }
    }
}
