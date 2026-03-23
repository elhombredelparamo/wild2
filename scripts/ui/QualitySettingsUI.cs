using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Wild.Core.Quality;

namespace Wild.UI
{
    public partial class QualitySettingsUI : Control
    {
        private OptionButton _profileSelector;
        
        // Selectores de calidad individual
        private OptionButton _treeQuality;
        private OptionButton _vegQuality;
        private OptionButton _terrainQuality;
        private OptionButton _playerQuality;
        private OptionButton _buildingQuality;
        private OptionButton _objectQuality;
        private OptionButton _deployQuality;
        private OptionButton _iconQuality;
        private OptionButton _groundTexQuality;
        private OptionButton _charTexQuality;
        private OptionButton _waterTexQuality;
        private OptionButton _skyTexQuality;
        private OptionButton _shadowQuality;
        private OptionButton _particleQuality;
        private OptionButton _postQuality;

        // Gestión de perfiles personalizados
        private LineEdit _customNameEdit;
        private Button _buttonSaveCustom;
        private OptionButton _customLoadSelector;
        private Button _buttonDeleteCustom;

        // Configuración global (Ocultas para futura reubicación)
        private Button _buttonAutoDetect;

        // Botones de acción
        private Button _buttonApply;
        private Button _buttonReset;

        public override void _Ready()
        {
            SetupReferences();
            PopulateSelectors();
            LoadCurrentSettings();
            ConnectSignals();
        }

        private void SetupReferences()
        {
            _profileSelector = GetNodeOrNull<OptionButton>("VBox/ProfileRow/OptionButton");
            
            // Componentes
            _treeQuality = GetNodeOrNull<OptionButton>("VBox/Scroll/Grid/TreeQuality");
            _vegQuality = GetNodeOrNull<OptionButton>("VBox/Scroll/Grid/VegQuality");
            _terrainQuality = GetNodeOrNull<OptionButton>("VBox/Scroll/Grid/TerrainQuality");
            _playerQuality = GetNodeOrNull<OptionButton>("VBox/Scroll/Grid/PlayerQuality");
            _buildingQuality = GetNodeOrNull<OptionButton>("VBox/Scroll/Grid/BuildingQuality");
            _objectQuality = GetNodeOrNull<OptionButton>("VBox/Scroll/Grid/ObjectQuality");
            _deployQuality = GetNodeOrNull<OptionButton>("VBox/Scroll/Grid/DeployQuality");
            _iconQuality = GetNodeOrNull<OptionButton>("VBox/Scroll/Grid/IconQuality");
            _groundTexQuality = GetNodeOrNull<OptionButton>("VBox/Scroll/Grid/GroundTexQuality");
            _charTexQuality = GetNodeOrNull<OptionButton>("VBox/Scroll/Grid/CharTexQuality");
            _waterTexQuality = GetNodeOrNull<OptionButton>("VBox/Scroll/Grid/WaterTexQuality");
            _skyTexQuality = GetNodeOrNull<OptionButton>("VBox/Scroll/Grid/SkyTexQuality");
            _shadowQuality = GetNodeOrNull<OptionButton>("VBox/Scroll/Grid/ShadowQuality");
            _particleQuality = GetNodeOrNull<OptionButton>("VBox/Scroll/Grid/ParticleQuality");
            _postQuality = GetNodeOrNull<OptionButton>("VBox/Scroll/Grid/PostQuality");

            HighlightUnimplementedFeatures();
            
            // Perfiles Custom
            _customNameEdit = GetNodeOrNull<LineEdit>("VBox/Custom/NameEdit");
            _buttonSaveCustom = GetNodeOrNull<Button>("VBox/Custom/SaveBtn");
            _customLoadSelector = GetNodeOrNull<OptionButton>("VBox/Custom/LoadSelector");
            _buttonDeleteCustom = GetNodeOrNull<Button>("VBox/Custom/DeleteBtn");

            _buttonAutoDetect = GetNodeOrNull<Button>("VBox/Global/AutoDetect");
            
            _buttonApply = GetNodeOrNull<Button>("HBox/Apply");
            _buttonReset = GetNodeOrNull<Button>("HBox/Reset");
        }

        private void PopulateSelectors()
        {
            if (_profileSelector != null)
            {
                _profileSelector.Clear();
                foreach (var profile in Enum.GetNames(typeof(QualityProfileType)))
                    _profileSelector.AddItem(profile);
            }

            // Llenar todos los selectores de componentes (Toaster a Ultra + Disabled en algunos)
            var componentSelectors = new List<OptionButton> {
                _treeQuality, _vegQuality, _terrainQuality, _playerQuality, _buildingQuality, _objectQuality, _deployQuality, _iconQuality,
                _groundTexQuality, _charTexQuality, _waterTexQuality, _skyTexQuality, _shadowQuality, _particleQuality, _postQuality
            };

            foreach (var sel in componentSelectors)
            {
                if (sel == null) continue;
                sel.Clear();
                foreach (var level in Enum.GetNames(typeof(QualityLevel)))
                {
                    // No permitir Disabled en árboles, vegetación, terreno, texturas del suelo, deployables ni iconos
                    if (level == "Disabled" && (sel == _treeQuality || sel == _vegQuality || sel == _terrainQuality || sel == _groundTexQuality || sel == _deployQuality || sel == _iconQuality))
                        continue;

                    sel.AddItem(level);
                }
            }

            UpdateCustomProfilesList();
        }

        private void UpdateCustomProfilesList()
        {
            if (_customLoadSelector == null) return;
            _customLoadSelector.Clear();
            _customLoadSelector.AddItem("Seleccionar perfil...");
            foreach (var name in QualityManager.Instance.Settings.CustomProfiles.Keys)
                _customLoadSelector.AddItem(name);
        }

        private void LoadCurrentSettings()
        {
            var s = QualityManager.Instance.Settings;
            
            if (_profileSelector != null) _profileSelector.Selected = (int)s.ProfileType;
            
            _treeQuality.Selected = (int)s.TreeQuality;
            _vegQuality.Selected = (int)s.VegetationQuality;
            _terrainQuality.Selected = (int)s.TerrainQuality;
            _playerQuality.Selected = (int)s.PlayerModelQuality;
            _buildingQuality.Selected = (int)s.BuildingModelQuality;
            _objectQuality.Selected = (int)s.ObjectModelQuality;
            _deployQuality.Selected = (int)s.DeployableQuality;
            _iconQuality.Selected = (int)s.IconQuality;
            _groundTexQuality.Selected = (int)s.GroundTextureQuality;
            _charTexQuality.Selected = (int)s.CharacterTextureQuality;
            _waterTexQuality.Selected = (int)s.WaterTextureQuality;
            _skyTexQuality.Selected = (int)s.SkyTextureQuality;
            _shadowQuality.Selected = (int)s.ShadowQuality;
            _particleQuality.Selected = (int)s.ParticleQuality;
            _postQuality.Selected = (int)s.PostProcessingQuality;

            if (_customNameEdit != null) _customNameEdit.Text = s.CustomProfileName;

            UpdateLabels();
            UpdateControlAvailability();
        }

        private void ConnectSignals()
        {
            if (_profileSelector != null) _profileSelector.ItemSelected += OnProfileSelected;
            
            if (_buttonAutoDetect != null)
                _buttonAutoDetect.Pressed += () => { 
                    QualityManager.Instance.AutoDetectAndApply();
                    LoadCurrentSettings();
                };

            if (_buttonApply != null)
                _buttonApply.Pressed += () => {
                    SaveToSettings();
                    QualityManager.Instance.ApplyCurrentSettings();
                };

            if (_buttonSaveCustom != null) _buttonSaveCustom.Pressed += OnSaveCustomPressed;
            if (_customLoadSelector != null) _customLoadSelector.ItemSelected += OnLoadCustomSelected;
            if (_buttonDeleteCustom != null) _buttonDeleteCustom.Pressed += OnDeleteCustomPressed;
        }

        private void OnProfileSelected(long index)
        {
            var profile = (QualityProfileType)index;
            if (profile != QualityProfileType.Custom)
            {
                QualityManager.Instance.Settings.ApplyPresetProfile(profile);
                LoadCurrentSettings();
            }
            UpdateControlAvailability();
        }

        private void SaveToSettings()
        {
            var s = QualityManager.Instance.Settings;
            s.ProfileType = (QualityProfileType)_profileSelector.Selected;
            s.AutoDetect = false; // Si el usuario guarda, asumimos que ya no quiere modo auto estricto
            
            if (s.ProfileType == QualityProfileType.Custom)
            {
                s.TreeQuality = (QualityLevel)_treeQuality.Selected;
                s.VegetationQuality = (QualityLevel)_vegQuality.Selected;
                s.TerrainQuality = (QualityLevel)_terrainQuality.Selected;
                s.PlayerModelQuality = (QualityLevel)_playerQuality.Selected;
                s.BuildingModelQuality = (QualityLevel)_buildingQuality.Selected;
                s.ObjectModelQuality = (QualityLevel)_objectQuality.Selected;
                s.DeployableQuality = (QualityLevel)_deployQuality.Selected;
                s.IconQuality = (QualityLevel)_iconQuality.Selected;
                s.GroundTextureQuality = (QualityLevel)_groundTexQuality.Selected;
                s.CharacterTextureQuality = (QualityLevel)_charTexQuality.Selected;
                s.WaterTextureQuality = (QualityLevel)_waterTexQuality.Selected;
                s.SkyTextureQuality = (QualityLevel)_skyTexQuality.Selected;
                s.ShadowQuality = (QualityLevel)_shadowQuality.Selected;
                s.ParticleQuality = (QualityLevel)_particleQuality.Selected;
                s.PostProcessingQuality = (QualityLevel)_postQuality.Selected;
                s.CustomProfileName = _customNameEdit.Text;
            }
            
            s.Save();
        }

        private void OnSaveCustomPressed()
        {
            string name = _customNameEdit?.Text.Trim();
            if (string.IsNullOrEmpty(name)) return;

            var profile = new QualityProfile {
                Name = name,
                TreeQuality = (QualityLevel)_treeQuality.Selected,
                VegetationQuality = (QualityLevel)_vegQuality.Selected,
                TerrainQuality = (QualityLevel)_terrainQuality.Selected,
                PlayerModelQuality = (QualityLevel)_playerQuality.Selected,
                BuildingModelQuality = (QualityLevel)_buildingQuality.Selected,
                ObjectModelQuality = (QualityLevel)_objectQuality.Selected,
                DeployableQuality = (QualityLevel)_deployQuality.Selected,
                IconQuality = (QualityLevel)_iconQuality.Selected,
                GroundTextureQuality = (QualityLevel)_groundTexQuality.Selected,
                CharacterTextureQuality = (QualityLevel)_charTexQuality.Selected,
                WaterTextureQuality = (QualityLevel)_waterTexQuality.Selected,
                SkyTextureQuality = (QualityLevel)_skyTexQuality.Selected,
                ShadowQuality = (QualityLevel)_shadowQuality.Selected,
                ParticleQuality = (QualityLevel)_particleQuality.Selected,
                PostProcessingQuality = (QualityLevel)_postQuality.Selected,
            };

            QualityManager.Instance.Settings.CustomProfiles[name] = profile;
            QualityManager.Instance.Settings.Save();
            UpdateCustomProfilesList();
        }

        private void OnLoadCustomSelected(long index)
        {
            if (index <= 0) return;
            string name = _customLoadSelector.GetItemText((int)index);
            if (QualityManager.Instance.Settings.CustomProfiles.TryGetValue(name, out var p))
            {
                var s = QualityManager.Instance.Settings;
                s.ProfileType = QualityProfileType.Custom;
                s.CustomProfileName = name;
                s.TreeQuality = p.TreeQuality;
                s.VegetationQuality = p.VegetationQuality;
                s.TerrainQuality = p.TerrainQuality;
                s.PlayerModelQuality = p.PlayerModelQuality;
                s.BuildingModelQuality = p.BuildingModelQuality;
                s.ObjectModelQuality = p.ObjectModelQuality;
                s.DeployableQuality = p.DeployableQuality;
                s.IconQuality = p.IconQuality;
                s.GroundTextureQuality = p.GroundTextureQuality;
                s.CharacterTextureQuality = p.CharacterTextureQuality;
                s.WaterTextureQuality = p.WaterTextureQuality;
                s.SkyTextureQuality = p.SkyTextureQuality;
                s.ShadowQuality = p.ShadowQuality;
                s.ParticleQuality = p.ParticleQuality;
                s.PostProcessingQuality = p.PostProcessingQuality;
                
                LoadCurrentSettings();
            }
        }

        private void OnDeleteCustomPressed()
        {
            int idx = _customLoadSelector.Selected;
            if (idx <= 0) return;
            string name = _customLoadSelector.GetItemText(idx);
            QualityManager.Instance.Settings.CustomProfiles.Remove(name);
            QualityManager.Instance.Settings.Save();
            UpdateCustomProfilesList();
        }

        private void UpdateLabels()
        {
        }

        private void UpdateControlAvailability()
        {
            bool isCustom = _profileSelector?.Selected == (int)QualityProfileType.Custom;

            if (_profileSelector != null) _profileSelector.Disabled = false;

            var componentSelectors = new List<OptionButton> {
                _treeQuality, _vegQuality, _terrainQuality, _playerQuality, _buildingQuality, _objectQuality, _deployQuality, _iconQuality,
                _groundTexQuality, _charTexQuality, _waterTexQuality, _skyTexQuality, _shadowQuality, _particleQuality, _postQuality
            };

            foreach (var sel in componentSelectors)
                if (sel != null) sel.Disabled = !isCustom;

            if (_customNameEdit != null) _customNameEdit.Editable = isCustom;
            if (_buttonSaveCustom != null) _buttonSaveCustom.Disabled = !isCustom;
        }

        private void HighlightUnimplementedFeatures()
        {
            var red = new Color(1, 0.3f, 0.3f); // Rojo suave para legibilidad

            // Lista de etiquetas de características pendientes
            string[] pendingLabels = {
                "VBox/Scroll/Grid/LabelPlayer",
                "VBox/Scroll/Grid/LabelBuild",
                "VBox/Scroll/Grid/LabelObj",
                "VBox/Scroll/Grid/LabelCharTex",
                "VBox/Scroll/Grid/LabelWaterTex",
                "VBox/Scroll/Grid/LabelPart"
            };

            foreach (var path in pendingLabels)
            {
                var label = GetNodeOrNull<Label>(path);
                if (label != null)
                {
                    label.SelfModulate = red;
                    label.TooltipText = "Esta característica aún no está implementada.";
                }
            }
        }
    }
}
