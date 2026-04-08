using System;
using System.Collections.Generic;
using Godot;

namespace Wild.Data
{
    /// <summary>
    /// Modelo de datos central para el sistema de salud anatómico.
    /// </summary>
    public class HealthData
    {
        public Dictionary<string, BodyPartStatus> BodyParts { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        public HealthData()
        {
            InitializeBodyParts();
        }

        private void InitializeBodyParts()
        {
            string[] parts = { "Cabeza", "Torso", "BrazoIzquierdo", "BrazoDerecho", "ManoIzquierda", "ManoDerecha", "PiernaIzquierda", "PiernaDerecha", "PieIzquierdo", "PieDerecho" };
            foreach (var part in parts)
            {
                if (!BodyParts.ContainsKey(part))
                    BodyParts[part] = new BodyPartStatus { PartName = part };
            }
        }

        /// <summary>
        /// Asegura que el diccionario contiene todas las partes necesarias tras una deserialización.
        /// </summary>
        public void GarantizarIntegridad()
        {
            if (BodyParts == null) BodyParts = new(StringComparer.OrdinalIgnoreCase);
            InitializeBodyParts();
        }

        public float GetTotalHealth()
        {
            if (BodyParts == null || BodyParts.Count == 0) return 100f; // Valor de seguridad

            float headWeight = 0.30f;
            float torsoWeight = 0.40f;
            float otherWeight = 0.30f; // Distribuido entre los 8 restantes (3.75% cada uno)

            float total = 0;
            int limbsCount = 0;
            float limbsTotal = 0;

            // Verificar si tenemos las partes vitales para el cálculo.
            // Si faltan (posible error de deserialización), evitamos devolver 0 para no matar al jugador.
            if (!BodyParts.ContainsKey("Cabeza") || !BodyParts.ContainsKey("Torso"))
            {
                return 100f; 
            }

            foreach (var kvp in BodyParts)
            {
                if (kvp.Key.Equals("Cabeza", StringComparison.OrdinalIgnoreCase)) total += kvp.Value.Condition * headWeight;
                else if (kvp.Key.Equals("Torso", StringComparison.OrdinalIgnoreCase)) total += kvp.Value.Condition * torsoWeight;
                else
                {
                    limbsTotal += kvp.Value.Condition;
                    limbsCount++;
                }
            }

            if (limbsCount > 0)
            {
                total += (limbsTotal / limbsCount) * otherWeight;
            }

            return total;
        }
    }

    public class BodyPartStatus
    {
        public string PartName { get; set; } = "";
        public float Condition { get; set; } = 100f; // 0-100
        public List<Wound> Wounds { get; set; } = new();
        public bool IsFractured { get; set; } = false;
        public bool IsBleeding { get; set; } = false;
        public bool IsInfected { get; set; } = false;
    }

    public class Wound
    {
        public WoundType Type { get; set; }
        public float Severity { get; set; } // 0-100
        public bool IsClean { get; set; } = false;
        public bool IsSutured { get; set; } = false;
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    public enum WoundType
    {
        Rasguño,
        Laceracion,
        HeridaProfunda,
        Fractura,
        Infeccion
    }
}
