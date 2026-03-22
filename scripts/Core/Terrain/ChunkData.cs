using System.IO;
using Godot;

namespace Wild.Core.Terrain
{
    /// <summary>
    /// Estructura de datos binaria para persistencia de chunks.
    /// Contiene altitudes y pesos de bioma para un grid de (ChunkSize+1)x(ChunkSize+1).
    /// </summary>
    public partial class ChunkData : RefCounted
    {
        public const int Size = 10;
        public const int Resolution = Size + 1;
        public const int TotalPoints = Resolution * Resolution;

        public float[] Altitudes = new float[TotalPoints];
        public Color[] BlendWeights = new Color[TotalPoints];

        public byte[] ToBinary()
        {
            using (var ms = new MemoryStream())
            {
                using (var writer = new BinaryWriter(ms))
                {
                    // Versión del formato (por si cambia en el futuro)
                    writer.Write((int)1);

                    for (int i = 0; i < TotalPoints; i++)
                    {
                        writer.Write(Altitudes[i]);
                        writer.Write(BlendWeights[i].R);
                        writer.Write(BlendWeights[i].G);
                        writer.Write(BlendWeights[i].B);
                        writer.Write(BlendWeights[i].A);
                    }
                }
                return ms.ToArray();
            }
        }

        public static ChunkData FromBinary(byte[] data)
        {
            var chunk = new ChunkData();
            using (var ms = new MemoryStream(data))
            {
                using (var reader = new BinaryReader(ms))
                {
                    int version = reader.ReadInt32();
                    if (version != 1) return null;

                    for (int i = 0; i < TotalPoints; i++)
                    {
                        chunk.Altitudes[i] = reader.ReadSingle();
                        float r = reader.ReadSingle();
                        float g = reader.ReadSingle();
                        float b = reader.ReadSingle();
                        float a = reader.ReadSingle();
                        chunk.BlendWeights[i] = new Color(r, g, b, a);
                    }
                }
            }
            return chunk;
        }
    }
}
