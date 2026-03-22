# 🧠 Wild v2.0: Memoria Optimizada (Antigravity)

## 📌 Identidad & Vibe
- **Meta**: Versión "limpia" de Wild, libre de sobre-ingeniería.
- **Prioridad**: 60 FPS estables, carga asíncrona, terreno procedural infinito.
- **Metodología**: Feature-Driven Development (FDD). Ciclos cortos (1-2 días).
- **Stack**: Godot 4.6.1 + C# (.NET 8.0).

## ⚠️ Reglas Inviolables
1. **Ejecución**: SOLO el usuario puede iniciar el juego. Prohibido ejecutar testing/comandos Godot sin permiso explícito.
2. **Logging**: Prohibido `GD.Print()`. Usar `Logger` Singleton (`LogInfo`, `LogWarning`, `LogError`). Salida: Consola + `wild.log`.
3. **Memorias**: Solo crear/modificar memorias bajo orden expresa del usuario.
4. **Godot CLI**: Sin acceso al editor visual. Uso de comandos:
   - Reimportar: `& "C:\Godot\Godot_v4.6.1-stable_win64_console.exe" --headless --editor --quit-after 60`
   - Import/Reimport: `--headless --import <file>` / `--reimport <dir>`
5. **Estructura**: `programa/` (Código Godot/C#), `codigo/` (Pseudocódigo/Referencia), `contexto/` (Specs), `fdd/` (Gestión).
6. **Pseudocódigo**: La carpeta `codigo/` es la VERDAD lógica. `programa/` es la implementación técnica adaptada.

## 🏗️ Arquitectura Core
- **Escala**: 1 unidad = 1 metro. Sistema global único (prohibido mezclar local/global).
- **Unificación**: No hay diferencia entre local y multijugador. El cliente siempre se conecta a un servidor (local o remoto).
- **Persistencia Continua**: Guardado instantáneo asíncrono. Sin botones de "Guardar".
- **Sin Ciclos**: Arquitectura unidireccional (High-level -> Low-level). Comunicación inversa solo vía eventos/callbacks.
- **SessionData**: Única fuente de verdad para variables globales (seed, nivel_calidad, posicion_jugador, etc.).

## 🌍 Terreno & Física
- **Chunks**: 10x10 metros. 100 puntos por chunk.
- **Altitudes**: Rango [-100, 1000]m. Perlin Noise múltiple.
- **Física**: CharacterBody3D, Gravedad 9.8m/s², Masa 70kg, Salto 5m/s (1.25m altura).
- **Colisión**: StaticBody3D + ConcavePolygonShape3D por chunk. Solo colisiones cercanas activas.

## 📡 Red & Datos
- **Red**: TCP/IP (IPv4), Puerto 7777, binario estructurado, LZ4 opcional.
- **Sesión**: Handshake -> Auth -> Sync -> Play. Heartbeat cada 30s, Timeout 60s.
- **Persistencia de Archivos**:
  - `worlds/[nombre]/seed.txt`: Seed 32-bit.
  - `worlds/[nombre]/metadata.json`: Configuración y stats.
  - `worlds/[nombre]/chunks/[x]_[z].dat`: Datos binarios del terreno.
  - `worlds/[nombre]/personajes/[id]/`: JSONs de inventario, equipamiento y datos.

## ⚡ Optimización & Gráficos
- **LOD (0-4)**: 0 (Cercano 0-50m, 4K), 1 (50-100m, 2K), 2 (100-200m, 1K), 3 (200-500m, 512px), 4 (500m+, Toaster).
- **Calidad Dinámica**: Toaster, Low, Medium, High, Ultra. Ajusta texturas (256px -> 4096px) y render distance.
- **Memoria**: Object Pooling (Partículas/Chunks), Cache LRU, Streaming de texturas/audio.

## 🏹 Gameplay
- **Biomas**: Océano (<-20m), Costa (-20 a 5m), Pradera (5-50m), Bosque (50-200m), Montaña (>200m).
- **Habilidades**: Nivel 1-100. Categorías: Supervivencia, Exploración, Crafting.
- **Personajes**: Vida, hambre, sed. Guardado automático de stats cada 30s o cambios críticos.
