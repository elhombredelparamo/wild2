using System;
using Godot;
using Wild;

namespace Wild.Scripts.Terrain
{
    /// <summary>
    /// Almacena los datos de un chunk para serialización
    /// </summary>
    public class ChunkData
    {
        public int ChunkX { get; set; }
        public int ChunkZ { get; set; }
        public float[,] HeightMap { get; set; }
        public int Size { get; set; }
        
        public ChunkData(int chunkX, int chunkZ, int size = 100)
        {
            ChunkX = chunkX;
            ChunkZ = chunkZ;
            Size = size;
            HeightMap = new float[size, size];
        }
        
        /// <summary>
        /// Obtiene la altura en una coordenada local del chunk (0-99)
        /// </summary>
        public float GetHeight(int localX, int localZ)
        {
            if (localX < 0 || localX >= Size || localZ < 0 || localZ >= Size)
                return 0f;
                
            return HeightMap[localX, localZ];
        }
        
        /// <summary>
        /// Establece la altura en una coordenada local del chunk
        /// </summary>
        public void SetHeight(int localX, int localZ, float height)
        {
            if (localX >= 0 && localX < Size && localZ >= 0 && localZ < Size)
            {
                HeightMap[localX, localZ] = height;
            }
        }
        
        /// <summary>
        /// Convierte coordenadas locales del chunk a coordenadas mundiales
        /// </summary>
        public Vector2 LocalToWorld(int localX, int localZ)
        {
            return new Vector2(
                ChunkX * Size + localX,
                ChunkZ * Size + localZ
            );
        }
        
        /// <summary>
        /// Convierte coordenadas mundiales a coordenadas locales del chunk
        /// </summary>
        public Vector2I WorldToLocal(float worldX, float worldZ)
        {
            return new Vector2I(
                Mathf.RoundToInt(worldX - ChunkX * Size),
                Mathf.RoundToInt(worldZ - ChunkZ * Size)
            );
        }
    }
}
