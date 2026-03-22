using Godot;
using System.Collections.Generic;
using Wild.Core.Biomes;
using Wild.Core.Quality;

namespace Wild.Core.Terrain
{
    public class MaterialCache
    {
    private ShaderMaterial _terrainMaterial;

    public MaterialCache()
    {
        InitializeTerrainMaterial();
    }

    public void Initialize()
    {
        // No hace falta nada aquí ahora
    }

    private void InitializeTerrainMaterial()
    {
        var shader = GD.Load<Shader>("res://shaders/terrain.gdshader");
        _terrainMaterial = new ShaderMaterial();
        _terrainMaterial.Shader = shader;

        // Determinar el sufijo de calidad para las texturas del suelo
        string suffix = GetQualitySuffix(QualityManager.Instance.Settings.GroundTextureQuality);

        // Cargamos todas las texturas de forma dinámica
        _terrainMaterial.SetShaderParameter("tex_oceano", LoadQualityTexture("res://assets/textures/biomas/oceano/1/", suffix));
        _terrainMaterial.SetShaderParameter("tex_costa", LoadQualityTexture("res://assets/textures/biomas/costa/1/", suffix));
        _terrainMaterial.SetShaderParameter("tex_pradera", LoadQualityTexture("res://assets/textures/biomas/pradera/1/", suffix));
        _terrainMaterial.SetShaderParameter("tex_bosque", LoadQualityTexture("res://assets/textures/biomas/bosque/1/", suffix));
        _terrainMaterial.SetShaderParameter("tex_montana", LoadQualityTexture("res://assets/textures/biomas/montana/1/", suffix));
    }

    private string GetQualitySuffix(QualityLevel level)
    {
        return level switch
        {
            QualityLevel.Ultra => "ultra",
            QualityLevel.High => "high",
            QualityLevel.Medium => "medium",
            QualityLevel.Low => "low",
            QualityLevel.Toaster => "toaster",
            _ => "ultra"
        };
    }

    private Texture2D LoadQualityTexture(string basePath, string suffix)
    {
        string path = $"{basePath}{suffix}.png";
        if (FileAccess.FileExists(path))
        {
            return GD.Load<Texture2D>(path);
        }
        
        // Fallback a ultra si no existe el nivel específico
        return GD.Load<Texture2D>($"{basePath}ultra.png");
    }

    public Material GetTerrainMaterial()
    {
        return _terrainMaterial;
    }

    public Material GetMaterial(BiomaType biomeType)
    {
        return _terrainMaterial;
    }
}
}
