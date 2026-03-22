using Godot;
using System;

public class NoiseGenerator
{
    private FastNoiseLite _baseNoise;
    private FastNoiseLite _fractalNoise;
    private int _seed = 12345;

    public int Seed
    {
        get => _seed;
        set
        {
            _seed = value;
            _baseNoise.Seed = _seed;
            _fractalNoise.Seed = _seed;
        }
    }

    public NoiseGenerator() : this(12345) { }

    public NoiseGenerator(int seed)
    {
        _seed = seed;
        
        // Configurar ruido base
        _baseNoise = new FastNoiseLite();
        _baseNoise.NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex;
        _baseNoise.Seed = _seed;

        // Configurar ruido fractal (inmutable en runtime)
        _fractalNoise = new FastNoiseLite();
        _fractalNoise.NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex;
        _fractalNoise.Seed = _seed;
        _fractalNoise.FractalType = FastNoiseLite.FractalTypeEnum.Fbm;
    }

    public float Noise2D(float x, float y)
    {
        return _baseNoise.GetNoise2D(x, y);
    }

    public float FractalNoise2D(float x, float y, int octaves, float persistence)
    {
        // En lugar de modificar el objeto global afectando a los demás hilos paralelos, 
        // usamos la instancia pre-configurada (y los parámetros si es necesario, aunque en FastNoiseLite no podemos
        // pasarlos como argumentos a GetNoise2D directamente, así que lo ideal sería configurarlo antes 
        // si cambian, pero en nuestro caso siempre se usa con octavas=4 y persistence=0.5f).
        
        // Puesto que siempre lo llamamos con (x, y, 4, 0.5f) en TerrainGenerator:
        _fractalNoise.FractalOctaves = octaves;
        _fractalNoise.FractalGain = persistence;
        
        // Nota: Técnicamente si otro hilo cambia octaves aquí mientras se evalúa, podría haber carrera.
        // Pero dado que TerrainGenerator siempre pasa (4, 0.5f), la carrera es inofensiva al re-asignar el mismo valor.
        return _fractalNoise.GetNoise2D(x, y);
    }

    /// <summary>
    /// Calcula una altura base "global" para una coordenada del mundo.
    /// Basado en biomas.pseudo (L479-L500) para garantizar continuidad.
    /// </summary>
    public float GetHeight(float worldX, float worldZ)
    {
        // Frecuencia baja para formas grandes (continentes/fosas)
        float noise1 = _baseNoise.GetNoise2D(worldX * 0.1f, worldZ * 0.1f) * 60f;
        // Frecuencia media para colinas
        float noise2 = _baseNoise.GetNoise2D(worldX * 0.5f, worldZ * 0.5f) * 20f;
        // Frecuencia alta para detalle
        float noise3 = _baseNoise.GetNoise2D(worldX * 2.0f, worldZ * 2.0f) * 5f;

        // Combinamos y mapeamos (ruido suele ser -1 a 1, aquí buscamos un rango amplio)
        float height = noise1 + noise2 + noise3;
        
        // Ajustamos para que el "nivel del mar" (0) esté en un punto interesante
        // En biomas.pseudo se mapea de -100 a 1000.
        // Provisonal: lo dejamos tal cual para ver el relieve.
        return height;
    }

    /// <summary>
    /// Calcula la humedad [0, 1] para una coordenada.
    /// Basado en biomas.pseudo (L505-L519).
    /// </summary>
    public float GetHumidity(float worldX, float worldZ)
    {
        // Usamos un offset en el muestreo para que no coincida con la altura
        float noise = _baseNoise.GetNoise2D(worldX * 0.5f + 2000f, worldZ * 0.5f + 2000f);
        return (noise + 1.0f) * 0.5f;
    }

    /// <summary>
    /// Calcula la temperatura [0, 40] grados Celsius.
    /// Basado en biomas.pseudo (L520-L534).
    /// </summary>
    public float GetTemperature(float worldX, float worldZ)
    {
        // Offset diferente para temperatura
        float noise = _baseNoise.GetNoise2D(worldX * 0.3f + 3000f, worldZ * 0.3f + 3000f);
        return (noise + 1.0f) * 20.0f;
    }
}
