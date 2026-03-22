using Godot;
using System.Linq;
using System;

namespace Wild.Core.Quality
{
    public class HardwareCapabilities
    {
        public string GPUName { get; set; }
        public string CPUName { get; set; }
        public int SystemMemoryMB { get; set; }
        public int GPUMemoryMB { get; set; }
        public int Cores { get; set; }
        public bool IsIntegratedGPU { get; set; }

        public static HardwareCapabilities Detect()
        {
            var caps = new HardwareCapabilities
            {
                GPUName = RenderingServer.GetVideoAdapterName(),
                CPUName = OS.GetProcessorName(),
                SystemMemoryMB = (int)(OS.GetStaticMemoryUsage() / 1024 / 1024), // Aproximación
                GPUMemoryMB = GetGPUMemory(),
                Cores = OS.GetProcessorCount()
            };

            caps.IsIntegratedGPU = CheckIsIntegrated(caps.GPUName);

            return caps;
        }

        private static int GetGPUMemory()
        {
            try
            {
                // En Godot 4, podemos intentar obtener memoria del RenderingDevice si está disponible
                var rd = RenderingServer.GetRenderingDevice();
                if (rd != null)
                {
                    // No hay un método directo "GetTotalMemory" en RenderingDevice que sea portable e inmediato siempre, 
                    // pero podemos usar lo que tengamos.
                    return 0; // Placeholder para detección real si fuera crítica
                }
            }
            catch {}
            return 0;
        }

        private static bool CheckIsIntegrated(string name)
        {
            string n = name.ToLower();
            return n.Contains("intel") || n.Contains("uhd") || n.Contains("iris") || n.Contains("vega") || (n.Contains("radeon") && !n.Contains("rx"));
        }

        public QualityLevel GetRecommendedQuality()
        {
            // Lógica simplificada basada en la documentación
            long ram = SystemMemoryMB;
            
            if (ram < 2048) return QualityLevel.Toaster;
            if (ram < 4096) return QualityLevel.Low;
            if (ram < 8192) return QualityLevel.Medium;
            if (ram < 16384) return QualityLevel.High;
            return QualityLevel.Ultra;
        }
    }
}
