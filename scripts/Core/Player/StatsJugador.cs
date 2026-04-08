using Godot;
using System;
using Wild.Utils;
using Wild.Data;

namespace Wild.Core.Player
{
    /// <summary>
    /// Gestiona las necesidades básicas del jugador (salud, hambre, sed, energía).
    /// </summary>
    public partial class StatsJugador : Node
    {
        private HealthData _saludData = new();
        public HealthData SaludData 
        { 
            get => _saludData; 
            set 
            { 
                _saludData = value; 
                _saludData?.GarantizarIntegridad(); 
            } 
        }

        [Export] public float Salud 
        { 
            get 
            { 
                if (!_isInitialized) return 100f;
                return EsMuerteVital() ? 0f : (SaludData?.GetTotalHealth() ?? 0f); 
            } 
            set { AplicarDañoGeneral(value); } 
        }

        private bool _isInitialized = false;

        [Export] public float Hambre = 100.0f;
        [Export] public float Sed = 100.0f;
        [Export] public float Energia = 100.0f;

        [ExportGroup("Tasas de Desgaste")]
        [Export] public float TasaHambre = 0.5f; // Por minuto
        [Export] public float TasaSed = 0.8f;    // Por minuto
        [Export] public float TasaEnergia = 0.2f; // Por minuto (base)
        [Export] public float TasaSangradoBase = 0.05f; // Por segundo por herida
        [Export] public float TasaInfeccionEnergia = 0.1f; // Pérdida de energía por infección
        [Export] public float TasaRegeneracionBase = 0.05f; // Salud recuperada por segundo (si está bien nutrido)

        public override void _Ready()
        {
            Logger.LogWithContext("PLAYER", "StatsJugador inicializado", "INIT");
        }

        /// <summary>
        /// Activa la lógica de muerte y metabolismo tras cargar los datos.
        /// </summary>
        public void FinalizarCarga()
        {
            _isInitialized = true;
            SaludData?.GarantizarIntegridad();
            Logger.LogInfo("StatsJugador: Carga finalizada. Lógica vital activa.");
        }

        public override void _Process(double delta)
        {
            if (!_isInitialized) return;

            float deltaMinutes = (float)delta / 60.0f;

            // Desgaste natural
            Hambre = Mathf.Max(0, Hambre - TasaHambre * deltaMinutes);
            Sed = Mathf.Max(0, Sed - TasaSed * deltaMinutes);
            
            float energiaConsumo = TasaEnergia;
            
            // ── Lógica de Sangrado e Infección ──
            float dañoSangradoTotal = 0f;
            bool hayInfeccion = false;

            foreach (var part in SaludData.BodyParts.Values)
            {
                if (part.IsBleeding)
                {
                    // Cada parte que sangra aporta a la tasa total
                    dañoSangradoTotal += TasaSangradoBase;
                }
                if (part.IsInfected) hayInfeccion = true;
            }

            // El sangrado afecta a la "Salud General" reduciendo el Torso (órgano vital centro)
            if (dañoSangradoTotal > 0)
            {
                RecibirDaño(dañoSangradoTotal * (float)delta, "Torso", WoundType.Laceracion);
            }

            // La infección drena energía
            if (hayInfeccion)
            {
                energiaConsumo += TasaInfeccionEnergia;
            }

            Energia = Mathf.Max(0, Energia - energiaConsumo * deltaMinutes);
            
            // --- REGENERACIÓN PASIVA ---
            bool estaBienNutrido = Hambre > 80f && Sed > 80f;
            if (estaBienNutrido && dañoSangradoTotal <= 0 && Salud < 100f)
            {
                Curar(TasaRegeneracionBase * (float)delta);
            }

            // Daño por hambre o sed extrema (aplicado al Torso)
            if (Hambre <= 0 || Sed <= 0)
            {
                RecibirDaño(2.0f * (float)delta, "Torso"); // 2 de daño por segundo al torso
            }

            if (Salud <= 0 || EsMuerteVital())
            {
                OnMuerte();
            }
        }

        private bool EsMuerteVital()
        {
            if (SaludData.BodyParts.TryGetValue("Cabeza", out var cabeza) && cabeza.Condition <= 0) return true;
            if (SaludData.BodyParts.TryGetValue("Torso", out var torso) && torso.Condition <= 0) return true;
            return false;
        }

        public void ProcesarCaida(float distancia)
        {
            float exceso = distancia - 3.5f;
            if (exceso <= 0) return;

            // Daño base: escala agresivamente con la altura
            float dañoTotal = exceso * 15f; 

            Logger.LogInfo($"StatsJugador: Procesando caída de {distancia:F2}m. Daño acumulado: {dañoTotal:F1}");

            // Curva de fatalidad
            if (distancia >= 15.0f)
            {
                RecibirDaño(100f, "Torso", WoundType.HeridaProfunda);
                Logger.LogWarning("StatsJugador: Caída fatal desde más de 15 metros.");
                return;
            }

            // Repartir daño entre las piernas
            float dañoPiernas = dañoTotal * 0.8f;
            float dañoTorso = dañoTotal * 0.2f;

            RecibirDaño(dañoPiernas * 0.5f, "PiernaIzquierda", WoundType.Laceracion);
            RecibirDaño(dañoPiernas * 0.5f, "PiernaDerecha", WoundType.Laceracion);
            RecibirDaño(dañoTorso, "Torso", WoundType.Rasguño);

            // Lógica de fracturas
            Random rand = new Random();
            float probabilidadFractura = Mathf.Clamp((distancia - 6.0f) / 6.0f, 0, 0.95f);

            if (distancia >= 6.0f)
            {
                if (rand.NextDouble() < probabilidadFractura) 
                    RecibirDaño(10f, "PiernaIzquierda", WoundType.Fractura);
                if (rand.NextDouble() < probabilidadFractura) 
                    RecibirDaño(10f, "PiernaDerecha", WoundType.Fractura);
            }
        }

        public void RecibirDaño(float cantidad, string parteCuerpo = "Torso", WoundType tipo = WoundType.Rasguño)
        {
            if (SaludData.BodyParts.ContainsKey(parteCuerpo))
            {
                var part = SaludData.BodyParts[parteCuerpo];
                part.Condition = Mathf.Max(0, part.Condition - cantidad);
                
                // Si el daño es suficiente, añadir una herida
                if (cantidad > 5f)
                {
                    part.Wounds.Add(new Wound { Type = tipo, Severity = cantidad });
                    if (tipo == WoundType.HeridaProfunda || tipo == WoundType.Laceracion) part.IsBleeding = true;
                }
                
                Logger.LogWithContext("HEALTH", $"Daño en {parteCuerpo}: {cantidad}. Nueva condición: {part.Condition}", "STATS");
            }
        }

        private void AplicarDañoGeneral(float nuevaSalud)
        {
            // Simple bridge for existing code that sets Salud float directly
            float diff = Salud - nuevaSalud;
            if (diff > 0) RecibirDaño(diff, "Torso");
        }

        private void OnMuerte()

        {
            Logger.LogWarning("PLAYER: El jugador ha muerto por falta de recursos.");
            // Implementar lógica de respawn o game over después
        }

        public void Alimentar(float cantidad) => Hambre = Mathf.Min(100, Hambre + cantidad);
        public void Hidratar(float cantidad) => Sed = Mathf.Min(100, Sed + cantidad);
        public void Curar(float cantidad) 
        {
            // Obtener todas las partes dañadas
            System.Collections.Generic.List<BodyPartStatus> partesDañadas = new();
            foreach (var part in SaludData.BodyParts.Values)
            {
                if (part.Condition < 100f) partesDañadas.Add(part);
            }

            if (partesDañadas.Count == 0) return;

            // Distribuir curación equitativamente
            float curaPorParte = cantidad / partesDañadas.Count;
            foreach (var part in partesDañadas)
            {
                part.Condition = Mathf.Min(100, part.Condition + curaPorParte);
            }
        }

        public void TratarSangrado()
        {
            foreach (var part in SaludData.BodyParts.Values)
            {
                if (part.IsBleeding)
                {
                    part.IsBleeding = false;
                    Logger.LogInfo($"StatsJugador: Sangrado detenido en {part.PartName}.");
                    return; // Solo tratar una parte por cada uso de venda
                }
            }
        }
    }
}
