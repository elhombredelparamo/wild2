# Sistema de Logging - Wild v2.0

## 🎯 Objetivo

Definir el sistema de logging estándar para Wild v2.0, asegurando consistencia, rendimiento y facilidad de debugging en todo el proyecto.

## 📋 Estándar de Logging del Proyecto

### 🚫 Regla Fundamental
**NO usar GD.Print() ni Console.WriteLine() en código de producción.**
**SIEMPRE usar siempre Logger del proyecto para todo logging.**

### 📋 Niveles de Logging

#### **INFO** - Mensajes Informativos
- Estado normal del sistema
- Inicialización completada
- Procesos exitosos
- Cambios de estado importantes

#### **WARNING** - Advertencias y Problemas
- Problemas no críticos que podrían afectar funcionalidad
- Rendimiento degradado
- Configuración por defecto usada

#### **ERROR** - Errores Críticos
- Errores graves que impiden funcionamiento correcto
- Excepciones no manejadas
- Fallas de sistema

#### **DEBUG** - Información Detallada
- Información de depuración temporal
- Variables de estado crítico
- Datos de rendimiento

---

## 🏗️ Arquitectura del Sistema

### 📋 Componentes Principales

#### **Logger** - Clase Central
```csharp
public static class Logger
{
    private static string _logFilePath = "user://logs/wild.log";
    private static LogLevel _minLogLevel = LogLevel.INFO;
    
    public static void Log(string message)
    {
        WriteLog("INFO", message);
    }
    
    public static void LogInfo(string message)
    {
        WriteLog("INFO", message);
    }
    
    public static void LogWarning(string message)
    {
        WriteLog("WARNING", message);
    }
    
    public static void LogError(string message)
    {
        WriteLog("ERROR", message);
    }
    
    private static void WriteLog(string level, string message)
    {
        try
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var logEntry = $"[{timestamp}] [{level}] {message}";
            
            File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);
        }
        catch (Exception ex)
        {
            // Si el logging falla, al menos mostrar en consola
            GD.Print($"[LOGGER ERROR] {ex.Message}");
        }
    }
}
```

#### **LogManager** - Gestión Avanzada
```csharp
public static class LogManager
{
    private static Dictionary<string, string> _contextTags = new();
    private static bool _enableFileLogging = true;
    private static bool _enableConsoleLogging = false; // Solo para desarrollo
    
    public static void SetContext(string contextTag)
    {
        _contextTags["default"] = contextTag;
    }
    
    public static void SetContext(string contextTag, string contextValue)
    {
        _contextTags[contextTag] = contextValue;
    }
    
    public static void EnableFileLogging(bool enable)
    {
        _enableFileLogging = enable;
    }
    
    public static void EnableConsoleLogging(bool enable)
    {
        _enableConsoleLogging = enable;
        if (enable)
        {
            GD.Print("[CONSOLE LOGGING] Console logging habilitado");
        }
    }
    
    public static void LogWithContext(string level, string message, string contextTag = "default")
    {
        var context = _contextTags.GetValueOrDefault(contextTag, "");
        var fullMessage = $"[{contextTag}] {message}";
        WriteLog(level, fullMessage);
    }
}
```

---

## 📋 Formato de Logs

### 📋 Estructura de Entrada de Log
```
[2026-03-10 15:20:30.123] [INFO] [GAMEWORLD] GameWorld: Inicializado nuevo sistema de renderizado...
[2026-03-10 15:20:30.456] [WARNING] [NETWORK] Cliente player_123: Latencia alta detectada (150ms)
[2026-03-10 15:20:30.789] [ERROR] [TERRAIN] Error generando chunk (1,2): Out of bounds
[2026-03-10 15:20:30.999] [DEBUG] [PHYSICS] PlayerController: Salto ejecutado (velocidad: 5.0m/s)
```

### 📋 Tags de Contexto
```csharp
// Ejemplos de uso de tags
Logger.LogWithContext("INFO", "Jugador conectado", "NETWORK");
Logger.LogWithContext("WARNING", "Chunk no encontrado", "TERRAIN");
Logger.LogWithContext("DEBUG", "Posición actual: (150.5, 480.2, 75.3)", "PLAYER");
Logger.LogWithContext("ERROR", "No se puede conectar al servidor", "NETWORK");
```

### 📋 Mensajes Estructurados
```csharp
// Logs con datos estructurados
Logger.Log($"[PLAYER] Posición actual: {playerPos}, Velocidad: {playerVelocidad}");

// Logs de rendimiento
Logger.Log($"[PERFORMANCE] FPS: {fps}, Chunks: {chunkCount}, Memory: {memoryUsage}MB");

// Logs de red con contexto
Logger.LogWithContext("NETWORK", $"Cliente {clientId} conectado desde {clientIP}");
Logger.LogWithContext("NETWORK", $"Enviando mensaje {messageType} ({size} bytes)", "CLIENT");
```

---

## 🚀 Niveles de Logging Configurables

### 📋 Configuración por Entorno

#### Desarrollo (Debug)
```csharp
LogManager.SetLogLevel(LogLevel.DEBUG);
LogManager.EnableConsoleLogging(true);
LogManager.EnableFileLogging(true);
Logger.Log("Logger: Modo DEBUG activado");
```

#### Producción (Release)
```csharp
LogManager.SetLogLevel(LogLevel.INFO);
LogManager.EnableConsoleLogging(false);
LogManager.EnableFileLogging(true);
Logger.Log("Logger: Modo RELEASE activado");
```

#### Pruebas (Testing)
```csharp
LogManager.SetLogLevel(LogLevel.DEBUG);
LogManager.EnableConsoleLogging(true);
LogManager.EnableFileLogging(true);
Logger.Log("Logger: Modo TESTING activado");
```

---

## 🔧 Métodos Especializados

### 📋 Logging de Componentes

#### Logging de Singleton
```csharp
public class BiomaManager : Node
{
    public override void _Ready()
    {
        Logger.LogWithContext("BIOMA_MANAGER", "BiomaManager inicializado", "INIT");
        InitializeBiomaSystem();
        Logger.LogWithContext("BIOMA_MANAGER", $"Seed configurado: {Seed}", "INIT");
    }
    
    public BiomaType GetBiomaAtPosition(Vector3 position)
    {
        var bioma = CalculateBioma(position);
        Logger.LogWithContext("BIOMA_MANAGER", $"Bioma en ({position.X:F1}, {position.Z:F1}): {bioma}", "TERRAIN");
        return bioma;
    }
}
```

#### Logging de Rendimiento
```csharp
public class ChunkRenderer : Node
{
    private async Task RenderChunkAsync(Vector2I chunkPos)
    {
        Logger.LogWithContext("CHUNK_RENDERER", $"Generando chunk {chunkPos}", "RENDER");
        
        try
        {
            var chunkData = await GenerateChunkData(chunkPos);
            var mesh = CreateMesh(chunkData);
            
            Logger.LogWithContext("CHUNK_RENDERER", $"Chunk {chunkPos} renderizado exitosamente", "RENDER");
            return mesh;
        }
        catch (Exception ex)
        {
            Logger.LogError($"CHUNK_RENDERER: Error renderizando chunk {chunkPos}: {ex.Message}");
            return null;
        }
    }
}
```

#### Logging de Red
```csharp
public class GameClient : Node
{
    public async Task<bool> ConnectToServer(string address, int port)
    {
        Logger.LogWithContext("NETWORK_CLIENT", $"Conectando a {address}:{port}", "NETWORK");
        
        try
        {
            _serverPeer = await NetworkManager.ConnectToServer(address, port);
            Logger.LogWithContext("NETWORK_CLIENT", "Conexión establecida", "NETWORK");
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError($"NETWORK_CLIENT: Error conectando: {ex.Message}");
            return false;
        }
    }
    
    public void SendPlayerMove(Vector3 position, Vector3 velocity)
    {
        var message = new PlayerMoveMessage
        {
            Type = "PlayerMove",
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Position = position,
            Velocity = velocity
        };
        
        Logger.LogWithContext("NETWORK_CLIENT", $"Enviando PlayerMove ({position:F1}, {velocity:F1})", "PLAYER");
        _serverPeer.SendJson(JsonSerializer.Serialize(message));
    }
}
```

---

## 📊 Manejo de Errores

### 🚨 Logging de Excepciones
```csharp
public async Task<ChunkData> LoadChunkAsync(Vector2I chunkPos)
{
    try
    {
        var chunkData = await LoadChunkFromFile(chunkPos);
        Logger.LogWithContext("TERRAIN", $"Chunk {chunkPos} cargado desde disco", "TERRAIN");
        return chunkData;
    }
    catch (FileNotFoundException ex)
    {
        Logger.LogError($"TERRAIN: Chunk {chunkPos} no encontrado en disco: {ex.Message}");
        return null;
    }
    catch (Exception ex)
    {
        Logger.LogError($"TERRAIN: Error cargando chunk {chunkPos}: {ex.Message}");
        return null;
    }
}
```

### 🔄 Logging con Contexto de Error
```csharp
public void ProcessPlayerInput(PlayerInput input)
{
    try
    {
        // Validar input
        if (!ValidateInput(input))
        {
            Logger.LogWarning($"PLAYER: Input inválido: {input}");
            return;
        }
        
        // Aplicar input
        ApplyInput(input);
        Logger.LogWithContext("PLAYER", $"Input procesado: {input}", "PLAYER");
    }
    catch (Exception ex)
    {
        Logger.LogError($"PLAYER: Error procesando input: {ex.Message}");
    }
}

private bool ValidateInput(PlayerInput input)
{
    // Validar que el input esté en rangos razonables
    return input.Movement.Length <= 1.0f &&
           input.Movement.Length >= 0.1f;
}
```

---

## 📈 Logs Específicos por Sistema

### 🎮 Sistema de Terreno
```
[TERRAIN] TerrainManager: Chunk (0, 0) generado con 121 biomas usando 1 generadores
[TERRAIN] TerrainManager: Aplicado generador PraderaGenerator para bioma Pradera (10201 posiciones)
[TERRAIN] TerrainManager: Chunk (1, 0) generado con 98 biomas usando 2 generadores
[INFO] TerrainManager: Terrain inicializado para mundo: Mundo_20260310_165313
[TERRAIN] TerrainManager: 0 chunks liberados
[TERRAIN] TerrainManager: Cache de datos de chunks limpiada
[TERRAIN] TerrainManager: ✅ Limpieza de chunks completada
```

### 🎮 Sistema de Renderizado
```
[RENDER] ChunkRenderer: Chunk (-1, -1) renderizado con biomas
[RENDER] ChunkRenderer: Chunk (-1, 0) renderizado con biomas
[RENDER] GameWorld: ✅ Chunk (-1, -1) renderizado con nuevo sistema
[RENDER] GameWorld: ✅ Chunk (-1, 0) renderizado con nuevo sistema
[RENDER] GameWorld: Actualizando renderizado en chunk (0, 0)
[RENDER] GameWorld: Renderizados 2 chunks este frame
[RENDER] GameWorld: Límite de chunks alcanzado (2), retrasando resto
```

### 🌐 Sistema de Red
```
[NETWORK] GameServer: Servidor iniciado en puerto 7777
[NETWORK] GameServer: Configurado para máximo 32 jugadores
[NETWORK] GameServer: Chunk (0, 0) generado para cliente player_123
[NETWORK] GameServer: Cliente player_123 desconectado
[NETWORK] NetworkManager: Cliente conectado al servidor
[NETWORK] GameClient: Conexión establecida a localhost:7777
[NETWORK] GameClient: Enviando PlayerMove (150.5, 480.2, 75.3)
[NETWORK] GameClient: Recibido ChunkData para chunk (0, 0)
[NETWORK] GameClient: Recibido BiomaUpdate para chunk (0, 0)
[NETWORK] GameClient: Latencia promedio: 45ms
```

### 🎮 Sistema de Física
```
[PHYSICS] PlayerController: Física inicializada
[PHYSICS] PlayerController: Cápsula de colisión configurada (0.5f, 2.0f)
[PHYSICS] PlayerController: Gravedad configurada (9.8 m/s²)
[PHYSICS] PlayerController: Fricción configurada (0.5f)
[PHYSICS] PlayerController: Salto ejecutado (velocidad: 5.0m/s)
[PHYSICS] PlayerController: Colisión detectada con TerrainChunk_0_0
[PHYSICS] PlayerController: Sobre superficie detectada
```

---

## 🔧 Configuración y Opciones

### ⚙️ Configuración del Logger

#### En Settings.json
```json
{
    "logging": {
        "level": "INFO",
        "enableFileLogging": true,
        "enableConsoleLogging": false,
        "logFilePath": "user://logs/wild.log",
        "maxFileSize": "10MB",
        "backupCount": 5
    }
}
```

#### Carga de Configuración
```csharp
public class ConfigLoader
{
    public static void LoadLoggingConfig()
    {
        try
        {
            var config = LoadConfig<LoggingConfig>("config/logging.json");
            
            LogManager.SetLogLevel(ParseLogLevel(config.Level));
            LogManager.EnableFileLogging(config.EnableFileLogging);
            LogManager.EnableConsoleLogging(config.EnableConsoleLogging);
            
            Logger.Log($"Logger: Configuración cargada: {config.Level}");
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error cargando configuración de logging: {ex.Message}");
            // Usar configuración por defecto
            LogManager.SetLogLevel(LogLevel.INFO);
            LogManager.EnableFileLogging(true);
            LogManager.EnableConsoleLogging(false);
        }
    }
}
```

---

## 📝 Mejores Prácticas

### ✅ Convenciones de Estilo

#### Nombres de Métodos
```csharp
// ✅ Bueno
public void UpdatePlayerPosition(Vector3 position)
{
    Logger.LogWithContext("PLAYER", $"Posición actual: {position}", "PLAYER");
}

// ❌ Evitar
public void UpdatePlayerPos(Vector3 pos) // Nombre poco descriptivo
{
    Logger.Log($"Posición: {pos}");
}
```

#### Mensajes Descriptivos
```csharp
// ✅ Bueno
Logger.Log($"Chunk {chunkPos} generado con {biomaCount} biomas");

// ❌ Evitar
Logger.Log("Chunk generado");
Logger.Log("Chunk generado con 121 biomas");
```

#### Contexto Consistente
```csharp
// ✅ Bueno
Logger.LogWithContext("NETWORK", $"Cliente {clientId} conectado", "NETWORK");

// ❌ Evitar
Logger.Log($"Cliente {clientId} conectado");
Logger.Log($"Conectado cliente {clientId}");
```

### ⚡️ Optimización de Rendimiento

#### Evitar Logging en Bucles Críticos
```csharp
// ❌ Evitar en bucles de renderizado
for (int i = 0; i < 1000; i++)
{
    Logger.Log($"Procesando chunk {i}"); // Esto genera demasiado spam
}

// ✅ Alternativa: Logging con cooldown
private int _logCounter = 0;
if (++_logCounter % 60 == 0)
{
    Logger.Log($"Procesados {_logCounter} chunks este frame");
}
```

#### Logging Asíncrono
```csharp
// ✅ Para operaciones largas
await Task.Run(() => GenerateLargeWorld());

// ❌ Para operaciones rápidas
var result = FastCalculation();
Logger.Log($"Resultado: {result}");
```

---

## 🎯 Estrategia de Debugging

### 🔍 Logs de Estado del Sistema
```csharp
void LogSystemState()
{
    Logger.Log("=== Estado del Sistema ===");
    Logger.Log($"Chunks cargados: {_loadedChunks.Count}");
    Logger.Log($"Clientes conectados: {_clients.Count}");
    Logger.Log($"Memoria usada: {GetMemoryUsage()}MB");
    Logger.Log($"FPS actual: {Engine.GetFramesPerSecond()}");
    Logger.Log("========================");
}
```

### 🔍 Logs de Flujo Completo
```csharp
Logger.Log("=== Iniciando Juego ===");
Logger.Log("1. Inicializando sistemas básicos...");
Logger.Log("2. Conectando al servidor...");
Logger.Log("3. Generando terreno inicial...");
Logger.Log("4. Creando jugador...");
Logger.Log("5. Iniciando gameplay...");
Logger.Log("=== Juego Iniciado ===");
```

### 🔍 Logs de Transición de Estado
```csharp
Logger.Log("=== Cambiando a Menú ===");
Logger.Log("Guardando estado del jugador...");
Logger.Log("Cerrando conexiones de red...");
Logger.Log("=== Volviendo al Menú ===");
```

---

## 🎯 Manejo de Errores

### 🚨 Jerarquía de Errores

#### 1. Error Crítico - Detener ejecución
```csharp
try
{
    await CriticalOperation();
}
catch (Exception ex)
{
    Logger.LogError($"ERROR CRÍTICO: {ex.Message}");
    Logger.LogError($"Stack Trace: {ex.StackTrace}");
    // Guardar estado de emergencia
    EmergencySave();
    throw;
}
```

#### 2. Error No Crítico - Recuperar
```csharp
try
{
    await RiskyOperation();
}
catch (Exception ex)
{
    Logger.LogWarning($"ERROR: {ex.Message}");
    return GetDefaultValue();
}
```

#### 3. Error Esperado - Ignorar
```csharp
try
    {
        await OptionalOperation();
    }
    catch (TimeoutException)
    {
        Logger.LogWarning("Timeout en operación opcional");
        // Continuar con valor por defecto
    }
}
```

---

## 📊 Logs de Rendimiento

### 📈 Métricas de Rendimiento
```csharp
class PerformanceMonitor
{
    private float _fps;
    private int _chunkCount;
    private float _memoryUsage;
    
    public void UpdateMetrics()
    {
        _fps = Engine.GetFramesPerSecond();
        _chunkCount = GetLoadedChunkCount();
        _memoryUsage = GetMemoryUsage();
        
        Logger.Log($"[PERFORMANCE] FPS: {_fps:F1}, Chunks: {_chunkCount}, Memoria: {_memoryUsage:F1}MB");
        
        // Alertas de rendimiento
        if (_fps < 30)
            Logger.LogWarning("⚠️ FPS bajo detectado");
        if (_memoryUsage > 500)
            Logger.LogWarning("⚠️ Uso alto de memoria");
    }
}
```

### 📈 Logs de Red
```csharp
class NetworkMetrics
{
    public void UpdateNetworkMetrics()
    {
        var metrics = CalculateNetworkMetrics();
        
        Logger.Log($"[NETWORK] {metrics.PlayersOnline} jugadores online");
        Logger.Log($"[NETWORK] {metrics.ChunksPerSecond} chunks/s");
        Logger.Log($"[NETWORK] {metrics.BytesPerSecond} bytes/s");
        Logger.Log($"[NETWORK] {metrics.AverageLatency:F2}ms avg latency");
    }
}
```

---

## 🎯 Conclusión

Este sistema de logging proporciona:

**✅ Consistencia Total:**
- Único estándar para todo el proyecto
- Formato estructurado y legible
- Tags de contexto para fácil filtrado

**🚀 Rendimiento Óptimizado:**
- Logging asíncrono para no bloquear
- Compresión opcional para archivos grandes
- Niveles configurables por entorno

**🔧 Depuración Sistemática:**
- Logs de estado para debugging
- Logs de errores para troubleshooting
- Logs de rendimiento para optimización

**🎯 Mantenibilidad:**
- Centralizado y fácil de modificar
- Configurable sin cambiar código
- Compatible con herramientas de análisis

Este sistema asegura que Wild v2.0 tenga un sistema de logging profesional que facilita debugging, optimización y mantenimiento a lo largo del desarrollo.
