using Godot;
using System;
using Wild.Core.Player;
using Wild.Data;
using Wild.Utils;

namespace Wild.UI
{
    public partial class HealthPanel : Control
    {
        private Control _silhouetteContainer;
        private ItemList _woundsList;
        private Label _statusLabel;
        
        public bool IsOpen => Visible;

        public override void _Ready()
        {
            _silhouetteContainer = GetNodeOrNull<Control>("Panel/Silhouette");
            _woundsList = GetNodeOrNull<ItemList>("Panel/WoundsList");
            _statusLabel = GetNodeOrNull<Label>("Panel/StatusLabel");
            
            Hide();
        }

        public void Open()
        {
            Show();
            UpdateUI();
            Logger.LogInfo("HealthPanel: Abierto.");
        }

        public void Close()
        {
            Hide();
            Logger.LogInfo("HealthPanel: Cerrado.");
        }

        public void UpdateUI()
        {
            // En una versión final, aquí actualizaríamos los colores de la silueta
            // basándonos en SaludData.
            var jugador = PlayerManager.Instance?.JugadorActual;
            if (jugador == null || jugador.Stats == null) return;

            var stats = jugador.Stats;
            if (_statusLabel != null)
            {
                _statusLabel.Text = $"Salud General: {stats.Salud:F0}% | Hambre: {stats.Hambre:F0}% | Sed: {stats.Sed:F0}%";
            }

            if (_woundsList != null)
            {
                _woundsList.Clear();
                foreach (var part in stats.SaludData.BodyParts)
                {
                    if (part.Value.Condition < 100)
                    {
                        _woundsList.AddItem($"[{part.Key}] Condición: {part.Value.Condition:F0}%");
                    }
                    foreach (var wound in part.Value.Wounds)
                    {
                        _woundsList.AddItem($"  - {wound.Type} (Gravedad: {wound.Severity:F1})", null, false);
                    }
                    if (part.Value.IsBleeding) _woundsList.AddItem($"  ⚠️ SANGRANDO", null, false);
                    if (part.Value.IsFractured) _woundsList.AddItem($"  🦴 FRACTURA", null, false);
                }
            }
        }
    }
}
