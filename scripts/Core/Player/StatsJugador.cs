using Godot;
using System;
using Wild.Utils;

namespace Wild.Core.Player
{
    /// <summary>
    /// Gestiona las necesidades básicas del jugador (salud, hambre, sed, energía).
    /// </summary>
    public partial class StatsJugador : Node
    {
        [Export] public float Salud = 100.0f;
        [Export] public float Hambre = 100.0f;
        [Export] public float Sed = 100.0f;
        [Export] public float Energia = 100.0f;

        [ExportGroup("Tasas de Desgaste")]
        [Export] public float TasaHambre = 0.5f; // Por minuto
        [Export] public float TasaSed = 0.8f;    // Por minuto
        [Export] public float TasaEnergia = 0.2f; // Por minuto (base)

        public override void _Ready()
        {
            Logger.LogWithContext("PLAYER", "StatsJugador inicializado", "INIT");
        }

        public override void _Process(double delta)
        {
            float deltaMinutes = (float)delta / 60.0f;

            // Desgaste natural
            Hambre = Mathf.Max(0, Hambre - TasaHambre * deltaMinutes);
            Sed = Mathf.Max(0, Sed - TasaSed * deltaMinutes);
            Energia = Mathf.Max(0, Energia - TasaEnergia * deltaMinutes);

            // Daño por hambre o sed extrema
            if (Hambre <= 0 || Sed <= 0)
            {
                Salud = Mathf.Max(0, Salud - 2.0f * (float)delta); // 2 de daño por segundo
            }

            if (Salud <= 0)
            {
                OnMuerte();
            }
        }

        private void OnMuerte()
        {
            Logger.LogWarning("PLAYER: El jugador ha muerto por falta de recursos.");
            // Implementar lógica de respawn o game over después
        }

        public void Alimentar(float cantidad) => Hambre = Mathf.Min(100, Hambre + cantidad);
        public void Hidratar(float cantidad) => Sed = Mathf.Min(100, Sed + cantidad);
        public void Curar(float cantidad) => Salud = Mathf.Min(100, Salud + cantidad);
    }
}
