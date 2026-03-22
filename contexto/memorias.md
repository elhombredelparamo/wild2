# Wild: Memorias Permanentes

Este archivo contiene el contexto vital del proyecto, decisiones arquitectónicas y "vibras" que deben persistir entre sesiones de trabajo.

## 📝 Estado Inicial (2026-03-15)
- **Hito**: Migración de `wild-new` a `wild-anti` completada.
- **Plataforma**: Godot 4.6.1 + C# (.NET 8.0).
- **Vibe**: Desarrollo estructurado basado en FDD (Feature-Driven Development). Enfoque en simplicidad y rendimiento 3D (chunks 10x10).
- **IA**: Transición exitosa de Windsurf/Cascade a Antigravity.

## 🏗️ Decisiones Arquitectónicas
1. **Coordenadas**: Sistema global único. Prohibido mezclar local/global para evitar los errores de la v1.
2. **Terreno**: Chunks pequeños de 10x10 para permitir un culling más agresivo y mejor rendimiento en hardware tipo "Toaster".
3. **Persistencia**: Basada en servidor-cliente con archivos `.dat`.

## 🧠 Conocimiento del Proyecto
- El proyecto busca ser una versión "limpia" de un desarrollo anterior que sufrió de sobre-ingeniería.
- Prioridad absoluta: 60 FPS estables y carga asíncrona de terreno.

---

## 📋 Memorias del Sistema (Grupo 1)

### 🌍 SISTEMA DE COORDENADAS - ESCALA EN METROS
**Fecha de Establecimiento**: 2026-03-11

**Escala y Unidades**:
- **1 unidad = 1 metro** en todas las dimensiones
- **Coordenadas**: Vector3(x, y, z) donde cada valor está en metros
- **Altura**: Posición Y en metros sobre el nivel del mar
- **Distancia**: Calculada en metros reales

**CoordenadaGlobal**:
```
CoordenadaGlobal:
├── chunk_x: int      # Coordenada del chunk (10x10 metros)
├── chunk_z: int      # Coordenada del chunk (10x10 metros)
├── local_x: int      # Posición dentro del chunk (0-9 metros)
└── local_z: int      # Posición dentro del chunk (0-9 metros)
```

**Dimensiones del Mundo**:
- Nivel del mar: 0 metros
- Montañas bajas: 50-100 metros
- Montañas medias: 100-200 metros
- Montañas altas: 200-500 metros
- Montañas extremas: 500+ metros

---

### 🎮 CONTROLADOR DE JUGADOR CON FÍSICA
**Fecha de Establecimiento**: 2026-03-11

**Componentes Principales**:
- **Tipo**: CharacterBody3D de Godot
- **Física**: Basada en fuerzas y gravedad real
- **Movimiento**: ApplyCentralForce() en lugar de posición directa
- **Colisiones**: Automáticas mediante motor de física
- **Terreno**: Siempre sobre superficie sin ajustes manuales

**Constantes Físicas**:
- **Gravedad**: 9.8 m/s² (estándar terrestre)
- **Fuerza de salto**: 5 m/s (salto de ~1.25 metros)
- **Fricción**: 0.5 (coeficiente de fricción moderado)
- **Masa**: 70 kg (masa promedio de personaje)
- **Velocidad máxima**: 8 m/s (correr)
- **Velocidad caminar**: 5 m/s (caminar)

**Ventajas del Sistema Basado en Fuerzas**:
- **Realismo Físico**: Inercia, aceleración, fricción natural
- **Estabilidad**: Sin teletransportes, clips, flotación
- **Interacciones Naturales**: Colisiones, rebotes, deslizamientos

---

### 🚫 REGLA DE EJECUCIÓN DEL JUEGO - SOLO USUARIO
**Fecha de Establecimiento**: 2026-03-12

**Regla Principal**: Solo el usuario puede iniciar la ejecución del juego Wild v2.0.

**Aplicación**:
- **Prohibido ejecutar**: El asistente no puede ejecutar el juego por iniciativa propia
- **Prohibido testing**: No se pueden lanzar pruebas del juego sin autorización explícita
- **Prohibido comandos**: No se pueden ejecutar comandos de Godot sin permiso del usuario
- **Solo informativo**: El asistente solo puede proporcionar información sobre cómo ejecutar

**Excepciones**:
- **Solo con autorización explícita**: El usuario debe dar permiso claro para ejecutar
- **Comandos informativos**: Se pueden mostrar comandos pero no ejecutarlos
- **Configuración**: Se puede configurar pero no ejecutar

---

### 🔄 COMANDO DE REIMPORTACIÓN DE ASSETS - GODOT 4.6.1
**Fecha de Establecimiento**: 2026-03-12

**Comando Correcto para Reimportación**:
```bash
& "C:\Godot\Godot_v4.6.1-stable_win64_console.exe" --headless --editor --quit-after 60
```

**Explicación de Parámetros**:
- **--headless**: Ejecutar Godot sin interfaz gráfica
- **--editor**: Inicia Godot en modo editor (necesario para importaciones)
- **--quit-after 60**: Espera 60 segundos para completar importación

**Por qué este comando funciona**:
- **Tiempo adecuado**: 60 segundos permiten importación completa
- **Sin interfaz**: Funciona en modo headless
- **Modo editor**: Procesa correctamente todos los assets

---

### 🌐 PROTOCOLO DE RED
**Fecha de Establecimiento**: 2026-03-11

**Especificación del Protocolo**:
- **Transporte**: TCP/IP sobre IPv4
- **Puerto**: 7777 (configurable)
- **Formato**: Mensajes binarios estructurados
- **Compresión**: LZ4 para payloads grandes

**Estructura del Mensaje**:
```
Cabecera (16 bytes):
├── TipoMensaje: 1 byte
├── IDCliente: 4 bytes
├── Timestamp: 8 bytes
├── TamañoPayload: 2 bytes
└── Checksum: 1 byte

Payload:
├── Datos específicos del tipo
└── Compresión opcional

Cola (4 bytes):
└── CRC32 del mensaje completo
```

**Tipos de Mensajes**:
- **Cliente → Servidor**: TRANSACCION_JUGADOR, TRANSACCION_CHUNK, SOLICITUD_ESTADO_INICIAL
- **Servidor → Cliente**: ESTADO_JUGADOR, ESTADO_CHUNK, CONEXION_ACEPTADA

**Manejo de Conexiones**:
- **Handshake**: MensajeConexion → CONEXION_ACEPTADA/RECHAZADA
- **Heartbeat**: Cada 30 segundos (PING/PONG)
- **Timeout**: 60 segundos sin respuesta

---

## 📋 Memorias del Sistema (Grupo 2)

### 📦 CARGADOR DINÁMICO DE RECURSOS
**Fecha de Establecimiento**: 2026-03-11

**Componentes Principales**:
- **Carga adaptativa**: Modelos y texturas por calidad
- **Rutas específicas**: Sufijos por nivel de calidad
- **Cache inteligente**: Separado por nivel de calidad
- **Fallback automático**: Recursos de menor calidad si no existen

**Convención de Nomenclatura**:
```
Modelos 3D:
├── modelo_ultra.glb      # Ultra Quality (máximo detalle)
├── modelo_high.glb        # High Quality (alta calidad)
├── modelo_medium.glb      # Medium Quality (calidad media)
├── modelo_low.glb         # Low Quality (baja calidad)
└── modelo_toaster.glb    # Toaster Quality (mínimo detalle)

Texturas:
├── textura_ultra.png      # 4K (4096x4096)
├── textura_high.png        # 2K (2048x2048)
├── textura_medium.png      # 1K (1024x1024)
├── textura_low.png         # 512x512
└── textura_toaster.png    # 256x256
```

**Ventajas del Sistema**:
- **Adaptabilidad**: Se adapta a cualquier configuración
- **Rendimiento**: Cache inteligente y carga asíncrona
- **Mantenimiento**: Convención clara y debugging fácil

---

### 📝 MEMORIA PERSISTENTE - SessionData Variables Globales
**Fecha de Establecimiento**: 2026-03-11

**Objetivo**: Establecer SessionData como el único repositorio de variables globales en Wild v2.0.

**Variables Globales Centralizadas**:
- **Configuración del Mundo**: seed_mundo, nombre_mundo, ruta_mundo
- **Configuración de Red**: es_servidor, ip_servidor, puerto_servidor, id_cliente
- **Estado del Jugador**: id_personaje, posicion_jugador, rotacion_jugador, estado_jugador
- **Configuración de Calidad**: nivel_calidad, render_distance, target_fps, vsync_enabled
- **Métricas en Tiempo Real**: fps_actual, chunks_cargados, chunks_visibles, memoria_usada
- **Configuración de Audio**: volumen_master, volumen_musica, volumen_efectos, audio_habilitado
- **Estado del Juego**: juego_pausado, tiempo_dia, clima_actual, modo_debug

**Patrón de Acceso**:
```pseudo
# Acceso universal desde cualquier sistema
SessionData.Instance.seed_mundo
SessionData.Instance.nivel_calidad
SessionData.Instance.posicion_jugador
```

**Principios Clave**:
- **Única fuente de verdad**: Todas las variables globales aquí
- **Acceso controlado**: Getters/setters con validación
- **Sin duplicación**: Ningún otro sistema almacena variables globales
- **Persistencia automática**: Guardado/carga de estado

---

### 🚫 MEMORIA PERSISTENTE - Evitar Referencias Circulares
**Fecha de Establecimiento**: 2026-03-11

**Objetivo**: Establecer como principio fundamental evitar referencias circulares a toda costa en Wild v2.0.

**Problema de Referencias Circulares**:
- **Deadlocks**: Sistemas esperándose mutuamente
- **Memory leaks**: Objetos que nunca se liberan
- **Bugs impredecibles**: Comportamiento errático
- **Testing imposible**: Dependencias cíclicas
- **Mantenimiento pesadilla**: Cambios en cadena

**Principios Fundamentales**:
- **Arquitectura Sin Ciclos**: Flujo unidireccional
- **Dependencias Siempre Hacia Abajo**: Sistemas de alto nivel → sistemas de bajo nivel
- **Eventos para Comunicación Inversa**: Sistema de eventos para comunicación ascendente
- **Singletons Solo para Acceso Global**: Sin referencias directas entre singletons

**Arquitectura Sin Ciclos**:
```
GameManager → SistemaTerreno → SessionData
GameManager → SistemaBiomas → SessionData
GameManager → SistemaRed → SessionData
GameManager → SistemaUI → SessionData
```

**Reglas Estrictas**:
- **Nunca**: SistemaA → SistemaB → SistemaA
- **Siempre**: SistemaA → SistemaB (unidireccional)
- **Comunicación inversa**: Eventos y callbacks
- **Validación**: Herramientas de análisis de dependencias

---

### 🌊 ALTURAS DEL TERRENO: -100 A 1000 METROS
**Fecha de Establecimiento**: 2026-03-11

**Rango de Alturas**:
```
-100m  a  -50m: Agua profunda (océano)
-50m   a  -20m: Agua somera (mar)
-20m   a   0m: Aguas poco profundas (costas)
0m     a   5m: Playas y costas
5m     a  50m: Llanuras y colinas bajas
50m    a 200m: Colinas y terrenos elevados
200m   a 500m: Montañas bajas
500m   a 800m: Montañas medias
800m   a 1000m: Montañas altas y extremas
```

**Sistema de Clasificación por Altura**:
- **Zonas Acuáticas**: Océano profundo (-100 a -50m), mar abierto (-50 a -20m), costas (-20 a 0m)
- **Zonas Costeras**: Playas y costas (0 a 5m), llanuras bajas (5 a 50m), colinas bajas (50 a 200m)
- **Zonas Montañosas**: Colinas elevadas (200 a 500m), montañas bajas (500 a 800m), montañas medias/altas (800 a 1000m)

**Generación Procedural**:
- **Combinación de múltiples capas de ruido Perlin**
- **Amplitudes**: 50m, 25m, 10m con diferentes frecuencias
- **Rango final**: -100 a 1000 metros
- **Continuidad**: Transiciones suaves entre alturas

**Ventajas del Rango Extendido**:
- **Diversidad de Paisajes**: Océanos profundos y montañas imponentes
- **Jugabilidad Mejorada**: Desafío vertical, exploración rica, construcción flexible
- **Realismo**: Escala vertical realista y coherente

---

### 🏔️ SISTEMA DE TERRENO
**Fecha de Establecimiento**: 2026-03-11

**Componentes Principales**:
- **Generación de Chunks**: Terreno en bloques de 10x10
- **Altitudes Procedurales**: Algoritmo de ruido Perlin
- **Lazy Loading**: Carga bajo demanda
- **Cache Inteligente**: Memoria eficiente

**Estructura de Datos**:
```
Chunk:
├── coordenada: CoordenadaGlobal
├── altitudes: float[10, 10]
├── tipo_terreno: int[10, 10]
└── vegetacion: List<ElementoVegetacion>
```

**Algoritmos**:
- **Perlin Noise**: Generación de alturas naturales
- **Interpolación**: Transiciones suaves entre chunks
- **LOD**: Level of Detail por distancia
- **Optimización**: Culling y renderizado eficiente

**Características**:
- **Infinito**: Mundo sin límites
- **Consistente**: Mismo resultado para misma semilla
- **Escalable**: Rendimiento constante
- **Persistente**: Chunks guardados en archivos .dat

**Integración**:
- **Coordenadas Globales**: Posicionamiento unificado
- **Biomas**: Clasificación y aplicación
- **Jugador**: Interacción y modificación
- **Persistencia**: Guardado automático

---

## 📋 Memorias del Sistema (Grupo 3)

### 🌍 SISTEMA DE COORDENADAS GLOBALES
**Fecha de Establecimiento**: 2026-03-11

**Descripción General**: Sistema de coordenadas únicas y consistentes para todo el mundo de Wild v2.0.

**Sistema de Coordenadas**:
- **CoordenadaGlobal**: Identificador único para cada posición del mundo
- **CoordenadaChunk**: Referencia a chunks de 10x10
- **Coordenadas Locales**: 0-9 dentro de cada chunk
- **Sistema Unificado**: Misma referencia para todos los sistemas

**Estructura de Coordenadas**:
```
CoordenadaGlobal(chunk_x, chunk_z, local_x, local_z)
├── chunk_x, chunk_z: Coordenada del chunk
├── local_x, local_z: Posición dentro del chunk (0-9)
└── Ejemplo: (5, 3, 7, 4) = Chunk (5,3), posición local (7,4)
```

**Conversión entre Sistemas**:
- **Vector3 → CoordenadaGlobal**: `Vector3AGlobal(posicion)`
- **CoordenadaGlobal → Vector3**: `GlobalAVector3(coord)`
- **Chunk → CoordenadaGlobal**: `ChunkAGlobal(chunk_x, chunk_z, local_x, local_z)`

**Beneficios**:
- **Consistencia** - Única fuente de verdad para posiciones
- **Escalabilidad** - Sistema infinito sin límites
- **Eficiencia** - Cálculos rápidos con coordenadas enteras
- **Modularidad** - Independiente de otros sistemas
- **Precisión** - Posicionamiento exacto sin ambigüedad

---

### 👥 GESTIÓN DE SESIONES
**Fecha de Establecimiento**: 2026-03-11

**Descripción General**: Sistema de gestión de sesiones de jugadores para Wild v2.0.

**Ciclo de Vida de Sesión**:
```
Conexión → Autenticación → Juego → Desconexión
├── Handshake inicial
├── Validación de personaje
├── Sincronización de estado
├── Juego activo
└── Limpieza de recursos
```

**Estados de Sesión**:
- **CONECTANDO**: Estableciendo conexión
- **AUTENTICANDO**: Validando personaje
- **CARGANDO**: Sincronizando estado inicial
- **JUGANDO**: Sesión activa
- **DESCONECTANDO**: Cerrando sesión
- **ERROR**: Error en sesión

**Gestión de Conexiones**:
```
Servidor:
├── Máximo jugadores: 32
├── Timeout conexión: 30s
├── Heartbeat: 30s
├── Rate limiting: 100 msg/s
└── Baneo: 24h por abuso
```

**Datos de Sesión**:
```
Sesion:
├── ID: int
├── IDCliente: int
├── IDPersonaje: string
├── IP: string
├── Estado: EstadoSesion
├── TimestampConexión: DateTime
├── TimestampUltimaActividad: DateTime
├── Jugador: JugadorLocal
└── BufferMensajes: Queue<MensajeRed>
```

**Manejo de Desconexiones**:
- **Graceful**: Desconexión voluntaria
- **Timeout**: Por inactividad
- **Error**: Por problemas de red
- **Kick**: Por comportamiento inapropiado
- **Baneo**: Por abuso del sistema

---

### 🔄 CAMBIO DE ARQUITECTURA: PERSISTENCIA CONTINUA
**Fecha del Cambio**: 2026-03-11

**Motivación del Cambio**: El usuario solicitó modificar el sistema de guardado/carga manual por un sistema de **persistencia continua** donde todo cambio se guarda instantáneamente sin necesidad de intervención del usuario.

**Cambios Realizados**:

**1. Sistema de Persistencia Continua**:
- **Archivo eliminado**: `codigo/data/save.pseudo`
- **Archivo creado**: `codigo/data/persistence.pseudo`
- **Características principales**:
  - Todo cambio se guarda instantáneamente mediante transacciones
  - Hilo dedicado de persistencia corriendo en background
  - Sistema de colas de transacciones para procesamiento eficiente
  - No requiere intervención del usuario para guardar

**2. Sistema de Conexión a Mundos**:
- **Archivo eliminado**: `codigo/data/loading.pseudo`
- **Archivo creado**: `codigo/data/world-connection.pseudo`
- **Características principales**:
  - "Cargar partida" reemplazado por "Conectar a Mundo"
  - Verificación de integridad de mundos
  - Sincronización automática al conectar
  - Lista de mundos disponibles con estado

**3. Interfaz de Usuario Actualizada**:
- **Archivo modificado**: `codigo/core/ui.pseudo`
- **Cambios principales**:
  - Menú principal: "Crear Mundo" y "Conectar a Mundo" en lugar de "Jugar"
  - Menú de pausa: "Desconectar del Mundo" en lugar de opciones de guardado
  - Eliminados todos los botones de guardado manual
  - Nuevos menús para creación y conexión de mundos

**Flujo de Usuario Actualizado**:

**Antes (Sistema Manual)**:
1. Menú Principal → "Jugar" / "Cargar Partida"
2. Durante juego → Opción de "Guardar"
3. Menú Pausa → "Guardar" / "Cargar"
4. Salir → "¿Guardar antes de salir?"

**Después (Persistencia Continua)**:
1. Menú Principal → "Crear Mundo" / "Conectar a Mundo"
2. Durante juego → Todo se guarda automáticamente
3. Menú Pausa → "Desconectar del Mundo"
4. Salir → Mundo ya está guardado

**Ventajas del Nuevo Sistema**:
- **Nunca se pierde progreso**: Todo se guarda instantáneamente
- **Experiencia fluida**: Sin interrupciones para guardar
- **Menor complejidad**: No necesita pensar en guardar
- **Recuperación instantánea**: Al conectar, mundo está como se dejó

---

### 🏗️ ARQUITECTURA SERVIDOR-CLIENTE
**Fecha de Establecimiento**: 2026-03-11

**Descripción General**: Separación clara de responsabilidades entre cliente y servidor para Wild v2.0.

**Responsabilidades del Servidor**:
- **Gestión centralizada** de toda la persistencia
- **Validación y autorización** de todas las acciones
- **Sincronización** de estado entre todos los clientes
- **Gestión de mundos** y sus datos asociados
- **Control de concurrencia** y consistencia de datos
- **Procesamiento de transacciones** de persistencia

**Responsabilidades del Cliente**:
- **Renderizado y presentación** de la interfaz
- **Captura de input** del usuario
- **Envío de transacciones** al servidor
- **Gestión local de personajes** (creación, selección, personalización)
- **Cache local** de datos recibidos del servidor
- **Predicción y extrapolación** para mejor rendimiento

**Comunicación**:
- **Protocolo personalizado** sobre TCP/IP
- **Mensajes estructurados** con tipos específicos
- **Validación de versión** entre cliente y servidor
- **Manejo de timeouts** y reconexión automática
- **Compresión opcional** para optimizar ancho de banda

**Flujo de Datos**:
- **Cliente → Servidor**: Transacciones de cambios
- **Servidor → Cliente**: Estado sincronizado
- **Servidor → Disco**: Persistencia en archivos .dat
- **Cliente → Local**: Cache de personajes

**Beneficios**:
- **Estado único** - Sin inconsistencias entre clientes
- **Seguridad** - Validación centralizada
- **Escalabilidad** - Servidor puede manejar N clientes
- **Rendimiento** - Cliente optimizado para renderizado

---

### 🌐 SISTEMA DE RED
**Fecha de Establecimiento**: 2026-03-11

**Descripción General**: Protocolo de comunicación cliente-servidor para Wild v2.0.

**Arquitectura de Red**:
- **TCP/IP**: Conexión confiable
- **Cliente-Servidor**: Modelo centralizado
- **Asíncrono**: No bloqueante
- **Compresión**: Optimización de ancho de banda

**Protocolo de Mensajes**:
```
MensajeRed:
├── tipo: TipoMensaje
├── id_cliente: int
├── timestamp: float
└── datos: específicos del tipo
```

**Tipos de Mensajes**:
- **Transacciones**: Cambios de estado
- **Sincronización**: Estado actual
- **Control**: Conexión y desconexión
- **Datos**: Información del juego

**Flujo de Comunicación**:
```
Cliente → Servidor:
├── Transacciones de cambios
├── Solicitudes de estado
└── Eventos de interacción

Servidor → Cliente:
├── Estado sincronizado
├── Actualizaciones de otros jugadores
└── Confirmaciones y errores
```

**Características**:
- **Confiable**: TCP garantiza entrega
- **Eficiente**: Compresión y batching
- **Seguro**: Validación de datos
- **Escalable**: Soporta múltiples clientes

**Manejo de Errores**:
- **Timeouts**: Desconexión automática
- **Reconexión**: Recuperación automática
- **Validación**: Verificación de integridad
- **Logging**: Registro de eventos

---

## 📋 Memorias del Sistema (Grupo 4)

### ⚡ SISTEMA DE CARGA ASÍNCRONA
**Fecha de Establecimiento**: 2026-03-11

**Descripción General**: Sistema de carga de recursos en segundo plano sin bloquear el juego.

**Concepto**: Carga de assets en hilo separado mientras el juego continúa funcionando.

**Tipos de Carga Asíncrona**:
- **Chunks**: Terreno procedural
- **Texturas**: Imágenes y materiales
- **Modelos**: Objetos 3D
- **Audio**: Música y efectos
- **UI**: Interfaces y menús

**Arquitectura**:
```
Carga Asíncrona:
├── Hilo Principal: Renderizado y input
├── Hilo Carga: Assets en background
├── Cola de Tareas: Solicitudes pendientes
└── Cache: Recursos ya cargados
```

**Flujo de Carga**:
1. **Solicitud**: Sistema solicita recurso
2. **Verificación**: Revisa si está en cache
3. **Carga**: Si no está, carga en background
4. **Notificación**: Informa cuando está listo
5. **Aplicación**: Sistema usa el recurso

**Estrategias**:
- **Lazy Loading**: Cargar bajo demanda
- **Preloading**: Cargar anticipadamente
- **Streaming**: Carga continua durante juego
- **Priorización**: Recursos importantes primero

**Optimizaciones**:
- **Compresión**: Reducir tamaño de archivos
- **Streaming**: Carga progresiva
- **Batching**: Agrupar cargas similares
- **Cache**: Reutilizar recursos

**Características**:
- **No Bloqueante**: Juego continúa funcionando
- **Transparente**: Usuario no nota las cargas
- **Eficiente**: Uso óptimo de recursos
- **Escalable**: Maneja grandes cantidades

---

### 🧠 OPTIMIZACIÓN DE MEMORIA
**Fecha de Establecimiento**: 2026-03-11

**Descripción General**: Gestión eficiente de memoria para Wild v2.0.

**Estrategias de Optimización**:
- **Object Pooling**: Reutilizar objetos
- **Cache Inteligente**: Almacenar recursos usados
- **Garbage Collection**: Liberación manual
- **Compresión**: Reducir uso de memoria

**Object Pooling**:
```
Pool de Objetos:
├── Chunks: Reutilizar chunks no visibles
├── Partículas: Efectos visuales
├── Audio: Sonidos y música
└── UI: Elementos de interfaz
```

**Cache Sistema**:
- **LRU Cache**: Least Recently Used
- **Tamaño Límite**: Memoria máxima configurable
- **Priorización**: Recursos importantes primero
- **Limpieza**: Liberar memoria automáticamente

**Gestión de Texturas**:
- **Streaming**: Carga progresiva
- **Compresión**: Formatos optimizados
- **Mipmaps**: Niveles de detalle
- **Unload**: Liberar no usadas

**Gestión de Audio**:
- **Streaming**: Música continua
- **Pooling**: Efectos de sonido
- **Compresión**: Formatos optimizados
- **Prioridades**: Sonidos importantes primero

**Monitoreo**:
- **Uso de RAM**: Memoria total usada
- **Fragmentación**: Espacio perdido
- **GC Pressure**: Frecuencia de recolección
- **Peak Memory**: Máximo uso alcanzado

**Características**:
- **Eficiente**: Uso óptimo de memoria
- **Adaptativo**: Se ajusta al hardware
- **Transparente**: Sin impacto en rendimiento
- **Configurable**: Ajustes manuales posibles

---

### 🎯 SISTEMA DE LOD - LEVEL OF DETAIL
**Fecha de Establecimiento**: 2026-03-11

**Descripción General**: Sistema de nivel de detalle para optimización de renderizado en Wild v2.0.

**Concepto**: LOD (Level of Detail) ajusta la calidad de renderizado según la distancia.

**Niveles de Detalle**:
```
LOD 0: Cercano (0-50m)
├── Máxima calidad
├── Texturas 4K
├── Modelos completos
└── 60+ FPS

LOD 1: Medio (50-100m)
├── Alta calidad
├── Texturas 2K
├── Modelos simplificados
└── 60 FPS

LOD 2: Lejano (100-200m)
├── Media calidad
├── Texturas 1K
├── Modelos básicos
└── 45 FPS

LOD 3: Muy lejano (200m+)
├── Baja calidad
├── Texturas 512px
├── Modelos muy simplificados
└── 30 FPS
```

**Algoritmos**:
- **Distancia Cálculo**: Vector3.Distance()
- **Transición Suave**: Interpolación entre niveles
- **Culling**: No renderizar objetos lejanos
- **Batching**: Agrupar objetos similares

**Optimizaciones**:
- **Frustum Culling**: Solo visible en cámara
- **Occlusion Culling**: Oculto por otros objetos
- **Instancing**: Múltiples copias del mismo objeto
- **Cache**: Reutilizar modelos y texturas

**Aplicaciones**:
- **Terreno**: Chunks con diferentes resolución
- **Vegetación**: Modelos simplificados por distancia
- **Jugadores**: Menos detalles en personajes lejanos
- **Items**: Simplificación de objetos 3D

**Características**:
- **Adaptativo**: Se ajusta al rendimiento
- **Transparente**: Cambios sin interrupciones
- **Eficiente**: Ahorra recursos significativamente
- **Configurable**: Ajuste manual posible

---

### ⚔️ SISTEMA DE HABILIDADES
**Fecha de Establecimiento**: 2026-03-11

**Descripción General**: Sistema de progresión de habilidades para personajes en Wild v2.0.

**Categorías de Habilidades**:
- **Supervivencia**: Caza, recolección, construcción
- **Exploración**: Navegación, detección, velocidad
- **Crafting**: Creación, mejoras, eficiencia

**Sistema de Progresión**:
```
Habilidad:
├── Nombre: string
├── Nivel: int (1-100)
├── Experiencia: long
├── Bonificaciones: Dictionary<string, float>
└── Desbloqueos: List<string>
```

**Mecánicas**:
- **Ganancia XP**: Uso de la habilidad
- **Bonificaciones**: Mejoras por nivel
- **Desbloqueos**: Nuevas capacidades
- **Synergies**: Combinaciones entre habilidades

**Bonificaciones por Nivel**:
- **Nivel 1-10**: +5% por nivel
- **Nivel 11-25**: +3% por nivel
- **Nivel 26-50**: +2% por nivel
- **Nivel 51-100**: +1% por nivel

**Ejemplos de Habilidades**:
```
Minería:
├── Nivel: 8
├── Velocidad: +40%
├── Raridad: +15%
└── Durabilidad: +25%
```

**Características**:
- **Progresivo**: Mejora continua
- **Especializable**: Diferentes caminos
- **Recompensante**: Sentido de logro
- **Balanceado**: Sin habilidades dominantes

**Integración**:
- **Jugador**: Sistema de progreso
- **UI**: Visualización de habilidades
- **Persistencia**: Guardado automático
- **Combate**: Aplicación de bonificaciones

---

### 📋 METODOLOGÍA FDD - FEATURE-DRIVEN DEVELOPMENT
**Fecha de Establecimiento**: 2026-03-11

**Descripción General**: Sistema de desarrollo por features para Wild v2.0.

**Flujo FDD**:
1. **Definición** - Especificación detallada de la feature
2. **Desarrollo** - Implementación incremental
3. **Pruebas** - Verificación y corrección
4. **Completado** - Revisión y documentación

**Proceso por Feature**:
```
1. Crear feature.md con especificación completa
2. Inicializar desarrollo en fdd/active/
3. Trabajar en la feature en ciclos cortos
4. Probar continuamente
5. Completar y mover a fdd/completed/
```

**Características**:
- **Ciclos cortos**: 1-2 días por feature
- **Entregable**: Especificaciones claras y medibles
- **Incremental**: Progreso visible y medible
- **Documentado**: Cada feature tiene su propia documentación

**Beneficios**:
- **Control** - Proceso predecible y controlado
- **Visibilidad** - Progreso claro y medible
- **Calidad** - Revisión continua del código
- **Entendimiento** - Entregable para nuevas features

**Integración**:
- **Jugador**: Sistema de progreso
- **UI**: Visualización de habilidades
- **Persistencia**: Guardado automático
- **Combate**: Aplicación de bonificaciones

---

## 📋 Memorias del Sistema (Grupo 5)

### 🚫 REGLA DE LOGGING - SISTEMA PROPIO
**Fecha de Establecimiento**: 2026-03-11

**Regla Principal**: No se usará la consola de Godot para logging. Se usará el sistema Logger propio del juego.

**Sistema de Logging Requerido**:
- **Archivo**: `codigo/utils/logger.pseudo`
- **Clase**: `Logger` (Singleton)
- **Niveles**: DEBUG, INFO, WARNING, ERROR, FATAL
- **Salidas**: Consola + archivo `wild.log`
- **Formato**: `[timestamp] [nivel] mensaje`

**Aplicación Obligatoria**:
- Todo el código debe usar `Log()`, `LogInfo()`, `LogError()`, etc.
- Nunca usar `GD.Print()` o consola de Godot
- Logging estructurado por sistemas: `[SISTEMA] mensaje`
- Archivo de log único para todo el juego

**Funciones Globales Disponibles**:
```pseudo
Log(mensaje, nivel)           # Genérico
LogInfo(mensaje)              # Informativo
LogWarning(mensaje)           # Advertencia
LogError(mensaje)             # Error
LogDebug(mensaje)             # Depuración
LogFatal(mensaje)             # Error fatal
```

**Logging por Sistema**:
- `LogSistema("nombre", mensaje)` - Sistema específico
- `LogErrorSistema("nombre", mensaje)` - Errores por sistema
- `LogRendimiento(metrica, valor)` - Métricas
- `LogRed(mensaje)` - Sistema de red
- `LogTerreno(mensaje)` - Sistema de terreno
- `LogJugador(id, mensaje)` - Jugadores
- `LogBioma(bioma, mensaje)` - Biomas
- `LogCalidad(calidad, mensaje)` - Calidad
- `LogUI(mensaje)` - Interfaz
- `LogRender(mensaje)` - Renderizado

**Configuración**:
- **Archivo log**: `wild.log`
- **Nivel mínimo**: Configurable (default INFO)
- **Consola activa**: Configurable
- **Archivo activo**: Configurable
- **Buffer**: 1000 mensajes máximo

**Implementación**:
- Singleton para acceso global
- Hilo separado para escritura de archivos
- Buffer circular para memoria eficiente
- Formato estandarizado para todos los mensajes

**Motivo**:
- Control total sobre formato y destino
- Integración con sistema de persistencia
- Mejor rendimiento que consola Godot
- Logs estructurados para análisis
- Compatible con arquitectura servidor-cliente

---

### 🖥️ REGLA DE GESTIÓN DE GODOT - SIN ACCESO AL EDITOR
**Fecha de Establecimiento**: 2026-03-12

**Regla Principal**: Godot está ubicado en "C:\Godot" y no tenemos acceso al editor de Godot. Las importaciones y operaciones del editor deben realizarse mediante comandos al programa de Godot.

**Ubicación de Godot**:
- **Ruta**: `C:\Godot\godot.exe`
- **Versión**: Godot 4.6.1
- **Acceso**: Solo línea de comandos

**Operaciones Requeridas**:

**🔄 Importaciones de Recursos**:
- **Comando**: `C:\Godot\godot.exe --headless --import <archivo>`
- **Uso**: Importar nuevos assets y recursos
- **Ejemplo**: `C:\Godot\godot.exe --headless --import res://assets/ui/new_image.png`

**🔄 Reimportaciones**:
- **Comando**: `C:\Godot\godot.exe --headless --reimport <ruta>`
- **Uso**: Actualizar recursos existentes
- **Ejemplo**: `C:\Godot\godot.exe --headless --reimport res://assets/ui/`

**🔄 Compilación**:
- **Comando**: `C:\Godot\godot.exe --headless --export <preset> <archivo_salida>`
- **Uso**: Compilar proyecto para distribución
- **Ejemplo**: `C:\Godot\godot.exe --headless --export Windows Desktop wild.exe`

**🔄 Validación de Proyecto**:
- **Comando**: `C:\Godot\godot.exe --headless --check <proyecto>`
- **Uso**: Verificar errores y advertencias
- **Ejemplo**: `C:\Godot\godot.exe --headless --check res://project.godot`

**Flujo de Trabajo**:

**📋 1. Desarrollo de Código**:
- **Editar archivos**: Directamente en el IDE
- **Guardar cambios**: Normal como archivos .cs y .tscn
- **Validar sintaxis**: Con herramientas del IDE

**🔄 2. Importación de Recursos**:
- **Añadir archivos**: Copiar a carpetas correspondientes
- **Importar**: Usar comando Godot para procesar
- **Verificar**: Comprobar que se importaron correctamente

**🔄 3. Testing**:
- **Ejecutar**: `C:\Godot\godot.exe --headless res://scenes/ui/main_menu.tscn`
- **Pruebas unitarias**: `C:\Godot\godot.exe --headless --script tests/test_ui.cs`
- **Validación**: `C:\Godot\godot.exe --headless --check`

**Comandos Comunes**:

**Importación de Assets**:
```bash
# Importar imagen
C:\Godot\godot.exe --headless --import res://assets/ui/fondo.png

# Importar todos los assets de una carpeta
C:\Godot\godot.exe --headless --import res://assets/ui/
```

**Testing de Escenas**:
```bash
# Probar menú principal
C:\Godot\godot.exe --headless res://scenes/ui/main_menu.tscn

# Probar sistema completo
C:\Godot\godot.exe --headless res://programa/Main.cs
```

**Limitaciones**:

**❌ Operaciones No Disponibles**:
- **Editor visual**: No disponible
- **Editor de escenas**: No disponible
- **Inspector de propiedades**: No disponible
- **Preview en tiempo real**: No disponible

**✅ Operaciones Disponibles**:
- **Edición de código**: Mediante IDE externo
- **Edición de escenas**: Mediante edición de .tscn
- **Importación**: Mediante línea de comandos
- **Testing**: Mediante línea de comandos
- **Compilación**: Mediante línea de comandos

---

### 📚 REGLA DE REFERENCIA DE CÓDIGO - PSEUDOCÓDIGO
**Fecha de Establecimiento**: 2026-03-12

**Regla Principal**: El pseudocódigo de la carpeta "codigo" y sus subcarpetas son la referencia para el código de programación que se va a programar, aunque luego haya que hacer ajustes específicos de implementación tecnológica.

**Flujo de Trabajo Establecido**:

**📋 1. Referencia Principal**:
- **Carpeta**: `codigo/` y subcarpetas
- **Contenido**: Pseudocódigo técnico y especificaciones
- **Propósito**: Guía de referencia para implementación

**🔧 2. Implementación Tecnológica**:
- **Carpeta**: `programa/` 
- **Contenido**: Código C# funcional para Godot
- **Propósito**: Adaptación técnica del pseudocódigo

**🔄 3. Proceso de Desarrollo**:
1. **Consultar pseudocódigo** en `codigo/` para entender lógica
2. **Adaptar a tecnología** C# + Godot en `programa/`
3. **Ajustar implementación** según requerimientos técnicos
4. **Mantener coherencia** con diseño original

**Relación entre Carpetas**:

**`codigo/` - Diseño y Lógica**:
- **Especificaciones técnicas** detalladas
- **Algoritmos y estructuras** de datos
- **Flujos de trabajo** y procesos
- **Patrones de diseño** y arquitectura
- **Documentación** de sistemas

**`programa/` - Implementación Funcional**:
- **Código C#** compilable y funcional
- **Adaptación Godot** específica
- **Optimizaciones** de rendimiento
- **Integración** con motor Godot
- **Testing** y depuración

**Ejemplos de Adaptación**:

**Pseudocódigo (`codigo/`)**:
```pseudo
class SistemaTerreno:
    function GenerarChunk(coord: CoordenadaGlobal): Chunk
        datos = generador.GenerarChunk(coord)
        return new Chunk(coord, datos)
```

**Implementación C# (`programa/`)**:
```csharp
namespace Wild.Terrain
{
    public partial class TerrainSystem : Node
    {
        public Chunk GenerateChunk(GlobalCoordinate coord)
        {
            var data = _generator.GenerateChunk(coord);
            return new Chunk(coord, data);
        }
    }
}
```

**Ajustes de Implementación Permitidos**:

**✅ Adaptaciones Técnicas**:
- **Sintaxis C#:** Cambiar de pseudocódigo a C#
- **Godot API:** Usar clases y métodos de Godot
- **Tipado fuerte:** Definir tipos explícitos
- **Namespaces:** Organizar código en namespaces
- **Eventos:** Usar sistema de eventos de C#

**✅ Optimizaciones**:
- **Rendimiento:** Mejoras específicas de Godot
- **Memoria:** Gestión específica de C#
- **Threading:** Implementación asíncrona
- **Cache:** Estrategias de memoria

**❌ Cambios Prohibidos**:
- **Lógica principal:** No cambiar algoritmos fundamentales
- **Arquitectura:** Mantener estructura de diseño
- **Flujos:** Preservar procesos definidos
- **Patrones:** No romper patrones establecidos

**Validación de Coherencia**:

**📋 Checklist de Implementación**:
- [ ] **Lógica preservada:** Algoritmo idéntico al pseudocódigo
- [ ] **Estructura mantenida:** Clases y métodos correspondientes
- [ ] **Flujo intacto:** Secuencia de pasos igual
- [ ] **Datos consistentes:** Estructuras de datos equivalentes
- [ ] **Comportamiento igual:** Resultados esperados idénticos

**Beneficios del Sistema**:

**✅ Claridad**:
- **Diseño separado** de implementación
- **Referencia única** para lógica
- **Documentación viva** en pseudocódigo

**✅ Flexibilidad**:
- **Adaptación tecnológica** sin perder diseño
- **Optimizaciones** específicas posibles
- **Evolución** independiente de cada capa

**✅ Mantenimiento**:
- **Cambios en lógica** solo en pseudocódigo
- **Cambios técnicos** solo en implementación
- **Versionado** diferenciado

---

### 📁 REGLA DE ESTRUCTURA DE CÓDIGO
**Fecha de Establecimiento**: 2026-03-12

**Regla Principal**: Todo el código de programación va dentro de la carpeta "programa"

**Estructura Obligatoria**:
- **Carpeta principal**: `programa/`
- **Proyecto Godot**: `programa/project.godot`
- **Código C#**: `programa/*.cs`
- **Recursos**: `programa/assets/`
- **Escenas**: `programa/scenes/`

**Separación de Responsabilidades**:
- **`programa/`:** Código ejecutable y recursos del juego
- **`codigo/`:** Documentación técnica y pseudocódigo
- **`contexto/`:** Especificaciones y diseño
- **`fdd/`:** Gestión del proyecto y features

**Flujo de Trabajo**:
1. **Desarrollar** en `programa/` con Godot Editor
2. **Documentar** en `codigo/` con pseudocódigo
3. **Especificar** en `contexto/` con detalles técnicos
4. **Gestionar** en `fdd/` con metodología FDD

**Beneficios**:
- **Claridad**: Separación neta entre código y documentación
- **Organización**: Cada carpeta tiene propósito específico
- **Mantenimiento**: Fácil encontrar y modificar código
- **Colaboración**: Estructura clara para equipo de desarrollo

---

### 📦 CHUNKS DE TAMAÑO 10X10
**Fecha de Establecimiento**: 2026-03-11

**Descripción General**: Sistema de chunks de 10x10 unidades para Wild v2.0, optimizado para rendimiento y culling eficiente.

**Estructura del Chunk**:
```
Chunk (10x10 unidades):
├── CoordenadaGlobal: (chunk_x, chunk_z, local_x, local_z)
├── altitudes: float[10, 10]      # Altura de cada punto
├── tipo_terreno: int[10, 10]    # Tipo de terreno
├── vegetacion: List<ElementoVegetacion>
└── recursos: List<ElementoRecurso>
```

**Dimensiones y Escala**:
- **Tamaño**: 10x10 unidades por chunk
- **Resolución**: 1 punto por unidad (1 metro)
- **Total**: 100 puntos de datos por chunk
- **Altura máxima**: Variable según bioma (0-200 metros)

**Ventajas del Tamaño 10x10**:

**🎯 Rendimiento**:
- **Generación rápida**: 100 puntos vs 10,000 (100x100)
- **Carga eficiente**: < 10ms por chunk
- **Memoria baja**: ~1KB por chunk en memoria
- **Streaming**: Carga bajo demanda sin bloqueo

**🔍 Culling Preciso**:
- **Granularidad fina**: Eliminación exacta de no visible
- **Render Distance**: 500-1000 metros (50-100 chunks)
- **Frustum Culling**: Solo chunks visibles
- **Occlusion Culling**: Chunks ocultos por otros

**🚀 Escalabilidad**:
- **Mundo infinito**: Sin límites de generación
- **Lazy Loading**: Solo chunks necesarios
- **Cache Inteligente**: Reutilización de chunks
- **Persistencia**: Guardado automático en .dat

**Sistema de Coordenadas**:
```
CoordenadaGlobal:
├── chunk_x: int      # Coordenada del chunk
├── chunk_z: int      # Coordenada del chunk
├── local_x: int      # Posición dentro del chunk (0-9)
└── local_z: int      # Posición dentro del chunk (0-9)

Conversión:
Vector3 → CoordenadaGlobal: GlobalAVector3()
CoordenadaGlobal → Vector3: GlobalAVector3()
```

**Generación Procedural**:
- **Algoritmo**: Ruido Perlin para alturas naturales
- **Biomas**: Aplicación según coordenadas
- **Vegetación**: Distribución probabilística
- **Recursos**: Generación basada en tipo de terreno

**Persistencia**:
- **Formato**: Binario .dat para eficiencia
- **Ubicación**: `worlds/[mundo]/chunks/x_z.dat`
- **Serialización**: Datos comprimidos
- **Integridad**: Checksum para verificación

**Comparación con Chunks Grandes**:

**❌ Chunks 100x100 (Anterior)**:
- **Datos**: 10,000 puntos por chunk
- **Generación**: 100x más lenta
- **Memoria**: 100x más RAM
- **Culling**: Poco preciso

**✅ Chunks 10x10 (Actual)**:
- **Datos**: 100 puntos por chunk
- **Generación**: Rápida y eficiente
- **Memoria**: Optimizada
- **Culling**: Preciso y granular

**Impacto en el Juego**:

**🎮 Experiencia del Jugador**:
- **Carga instantánea**: Sin esperas notables
- **Movimiento fluido**: 60 FPS constante
- **Mundo vivo**: Generación continua
- **Sin límites**: Exploración infinita

**🔧 Desarrollo**:
- **Testing fácil**: Chunks pequeños para pruebas
- **Debugging**: Problemas localizados
- **Optimización**: Métricas claras
- **Extensión**: Fácil añadir características

---

## 📋 Memorias del Sistema (Grupo 6)

### 🎯 SISTEMA DE COLISIONES 3D
**Fecha de Establecimiento**: 2026-03-11

**Descripción General**: Sistema de colisiones 3D para Wild v2.0 utilizando el motor de física Godot, proporcionando interacción física precisa entre personajes y terreno sin bugs de flotación.

**Componentes Principales**:

**🎮 Jugador (CharacterBody3D)**:
- **Tipo**: CharacterBody3D de Godot
- **Forma de colisión**: Cápsula de 2 metros de altura
- **Radio**: 0.5 metros de diámetro
- **Posición**: Centrada en el personaje
- **Física**: Controlada por motor de física Godot

**🌍 Terreno (StaticBody3D)**:
- **Tipo**: StaticBody3D estático
- **Forma de colisión**: ConcavePolygonShape3D
- **Topografía**: Sigue exactamente el terreno generado
- **Actualización**: Dinámica con cambios de terreno
- **Optimización**: Por chunks para rendimiento

**Características del Sistema**:

**✅ Detección Automática**:
- **Terreno debajo**: Detecta automáticamente la superficie
- **Ajuste continuo**: Mantiene jugador sobre el terreno
- **Sin ajustes manuales**: No requiere AdjustToTerrain()
- **Flujo natural**: Movimiento físico realista

**🚀 Eliminación de Bugs**:
- **Sin flotación**: Elimina completamente bugs de flotación
- **Sin clips**: Evita atravesar el terreno
- **Sin deslizamientos**: Fricción realista
- **Sin teletransportes**: Movimiento continuo y suave

**🎯 Precisión Física**:
- **Colisión precisa**: ConcavePolygonShape3D exacto
- **Respuesta realista**: Física basada en Godot
- **Interacciones naturales**: Rebotes, resbalones, etc.
- **Múltiples objetos**: Soporte para varios personajes

**Implementación Técnica**:

**🔧 Generación de Colisiones de Terreno**:
```pseudo
class SistemaColisionesTerreno:
    function GenerarColisionChunk(chunk: Chunk):
        # Crear StaticBody3D para el chunk
        cuerpo_colision = new StaticBody3D()
        
        # Generar ConcavePolygonShape3D desde datos del chunk
        forma_colision = new ConcavePolygonShape3D()
        vertices = GenerarVerticesDesdeAlturas(chunk.altitudes)
        forma_colision.SetVertices(vertices)
        
        # Asignar forma al cuerpo
        cuerpo_colision.ShapeOwnerAddShape(forma_colision)
        
        return cuerpo_colision
```

**🎮 Sistema de Jugador**:
```pseudo
class JugadorFisico extends CharacterBody3D:
    # Propiedades físicas
    altura_capsula: float = 2.0
    radio_capsula: float = 0.5
    gravedad: float = 9.8
    velocidad_maxima: float = 5.0
    
    function _Ready():
        # Configurar forma de colisión
        forma_capsula = new CapsuleShape3D()
        forma_capsula.Height = altura_capsula
        forma_capsula.Radius = radio_capsula
        
        # Asignar forma al CharacterBody3D
        CollisionShape3D.Shape = forma_capsula
```

**Ventajas del Sistema**:

**✅ Rendimiento**:
- **Optimizado**: Motor de física Godot optimizado
- **Por chunks**: Solo colisiones cercanas activas
- **Culling**: Eliminación de colisiones lejanas
- **Cache**: Formas de colisión reutilizadas

**✅ Estabilidad**:
- **Sin bugs**: Elimina problemas comunes de terreno
- **Robusto**: Manejo de casos extremos
- **Consistente**: Comportamiento predecible
- **Confiable**: Sin ajustes manuales

**✅ Realismo**:
- **Física real**: Basada en leyes físicas
- **Interacciones naturales**: Rebotes, resbalones
- **Gravedad correcta**: 9.8 m/s²
- **Fricción**: Superficies diferentes

---

### 🏗️ ARQUITECTURA UNIFICADA DE JUEGO EN WILD
**Fecha de Establecimiento**: 2026-03-12

**Descripción General**: En Wild v2.0, no hay diferencia práctica entre juego local y multijugador. Ambos modos se ejecutan exactamente igual, con la única diferencia siendo la conexión del cliente al servidor.

**Principio Fundamental**:

**🔧 Arquitectura Cliente-Servidor Siempre**:
- **Juego local**: Cliente se conecta a servidor local
- **Juego multijugador**: Cliente se conecta a servidor remoto
- **Comportamiento idéntico**: Mismo código, misma lógica, misma experiencia
- **Sin distinción**: El juego no sabe ni necesita saber si es "local" o "multijugador"

**Implementación Técnica**:

**🌐 Cliente Siempre Cliente**:
```csharp
// El cliente siempre se comporta igual
public class GameClient
{
    private string serverAddress;
    
    public void ConnectToServer(string address)
    {
        serverAddress = address; // "127.0.0.1" o "192.168.1.100"
        // Lógica de conexión idéntica
        EstablishConnection();
        StartGameLoop();
    }
    
    // El resto del código no sabe ni le importa dónde está el servidor
}
```

**🖥️ Servidor Siempre Servidor**:
```csharp
// El servidor siempre se comporta igual
public class GameServer
{
    public void StartServer()
    {
        // Lógica de servidor idéntica
        StartNetworking();
        ProcessWorldUpdates();
        HandlePlayerConnections();
    }
    
    // El servidor no sabe si es "local" o "remoto"
}
```

**🎮 Mundo Siempre Activo**:
```csharp
// El mundo nunca se detiene, sin importar el modo
public class GameWorld
{
    private void TogglePause()
    {
        _isPaused = !_isPaused;
        
        if (_isPaused)
        {
            // Solo UI local, el mundo continúa
            _pauseMenu.Show();
            Input.MouseMode = Input.MouseModeEnum.Visible;
        }
        else
        {
            // Solo UI local, el mundo continúa
            _pauseMenu.Hide();
            Input.MouseMode = Input.MouseModeEnum.Confined;
        }
        
        // NUNCA pausar el mundo - siempre activo
        // GetTree().Paused = true; // ¡JAMÁS!
    }
}
```

**Flujo de Conexión**:

**🏠 Modo Local**:
```
1. Usuario: "Juego Local"
2. Launcher: Inicia servidor local (127.0.0.1:7777)
3. Launcher: Inicia cliente
4. Cliente: ConnectToServer("127.0.0.1:7777")
5. Cliente: Juega normalmente
```

**🌍 Modo Multijugador**:
```
1. Usuario: "Multijugador"
2. Launcher: Inicia cliente
3. Cliente: ConnectToServer("192.168.1.100:7777")
4. Cliente: Juega normalmente
```

**🔄 Resultado Idéntico**:
- **Mismo código**: Exactamente la misma lógica
- **Misma experiencia**: Jugabilidad idéntica
- **Mismo servidor**: Comportamiento del servidor idéntico
- **Mundo siempre activo**: Nunca se detiene

**Beneficios de esta Arquitectura**:

**✅ Simplicidad**:
- **Una base de código**: No hay que mantener dos versiones
- **Sin condicionales**: No hay "if (isMultiplayer)"
- **Mismo testing**: Pruebas idénticas para ambos modos
- **Mismo debugging**: Problemas y soluciones iguales

**✅ Consistencia**:
- **Comportamiento unificado**: Mismas reglas en todos los modos
- **Sin bugs diferenciales**: Lo que funciona en local funciona en multiplayer
- **Misma experiencia**: El jugador no nota la diferencia
- **Mismas limitaciones**: Las mismas reglas aplican siempre

**✅ Escalabilidad**:
- **Fácil transición**: Local → Multijugador sin cambios
- **Mismos sistemas**: Todo funciona igual
- **Sin reescritura**: No hay que adaptar código
- **Desarrollo eficiente**: Un solo camino

**Errores Comunes a Evitar**:

**❌ Crear dos sistemas**:
```csharp
// INCORRECTO - Duplicación innecesaria
if (isMultiplayer)
{
    HandleMultiplayerLogic();
}
else
{
    HandleSingleplayerLogic();
}
```

**❌ Pausar el mundo localmente**:
```csharp
// INCORRECTO - El mundo nunca se detiene
if (isLocalGame)
{
    GetTree().Paused = true; // ¡PROHIBIDO!
}
```

**❌ Diferenciar comportamientos**:
```csharp
// INCORRECTO - No hay distinción
public void SaveGame()
{
    if (isLocalGame)
    {
        SaveLocally();
    }
    else
    {
        SaveOnServer();
    }
}
```

**Implementación Correcta**:

**✅ Un solo flujo**:
```csharp
// CORRECTO - Un solo camino
public void SaveGame()
{
    // Siempre guardar en el servidor
    // Si es local, el servidor es local
    // Si es multiplayer, el servidor es remoto
    SaveOnServer();
}
```

**✅ Conexión abstracta**:
```csharp
// CORRECTO - La dirección es solo un parámetro
public void StartGame(string serverAddress)
{
    // La lógica es idéntica sin importar la dirección
    ConnectToServer(serverAddress);
    StartGameLoop();
}
```

---

### 🔄 SISTEMA DE PERSISTENCIA DE MUNDOS
**Fecha de Establecimiento**: 2026-03-11

**Descripción General**: Sistema completo de persistencia para mundos de Wild v2.0 con estructura de archivos organizada y robusta.

**Estructura de Directorios**:
```
worlds/
└── [nombre_mundo]/
    ├── seed.txt
    ├── metadata.json
    ├── chunks/
    │   ├── [chunk_x]_[chunk_z].dat
    │   └── ...
    ├── personajes/
    │   ├── [id_personaje]/
    │   │   ├── datos_mundo.json
    │   │   ├── inventario.json
    │   │   └── equipamiento.json
    │   └── ...
    └── logs/
        ├── mundo.log
        └── eventos.json
```

**Archivos por Mundo**:

**🌱️ seed.txt**:
- **Propósito**: Seed numérica para generación procedural
- **Formato**: Texto plano con número entero de 32 bits
- **Tamaño**: ~10 bytes
- **Contenido**: `"123456789"`
- **Importancia**: Fundamental para reproducibilidad exacta

**📊 metadata.json**:
- **Propósito**: Información general y configuración del mundo
- **Formato**: JSON estructurado
- **Tamaño**: ~2 KB
- **Contenido**: Seed, nombre, fechas, estadísticas, configuración

**📦 chunks/ (Directorio)**:
- **Propósito**: Datos de terreno generados
- **Formato**: Binario .dat por chunk
- **Nomenclatura**: `[chunk_x]_[chunk_z].dat`
- **Estructura**: Header + altitudes + tipos terreno + vegetación + recursos + checksum

**👥 personajes/ (Directorio)**:
- **Propósito**: Datos de todos los personajes del mundo
- **Formato**: JSON por personaje
- **Nomenclatura**: `[id_personaje]/` con subdirectorios
- **Archivos**: datos_mundo.json, inventario.json, equipamiento.json

**📝 logs/ (Directorio)**:
- **Propósito**: Registro de eventos y logs del mundo
- **Formato**: Texto plano (.log) y JSON (.json)
- **Archivos**: mundo.log, eventos.json
- **Rotación**: Automática por tamaño

**Sistema de Nomenclatura**:

**📁 Convenciones de Nombres**:
```
Mundos:
├── Formato: [nombre_mundo]
├── Reglas: Sin espacios, caracteres especiales
├── Ejemplos: MiMundo, Aventura, SurvivalWorld
└── Máximo: 50 caracteres

Chunks:
├── Formato: [chunk_x]_[chunk_z].dat
├── Ejemplos: 0_0.dat, -1_1.dat, 10_-5.dat
├── Coordenadas: Enteros (positivos y negativos)
└── Extensión: .dat (binario)
```

**Sistema de Guardado Automático**:

**⏰ Frecuencias de Persistencia**:
```
Jugador Local:
├── Estadísticas (vida, hambre, sed): Cada 30 segundos
├── Inventario: Al cambiar item
├── Equipamiento: Al equipar/desequipar
├── Posición: Cada 60 segundos o al moverse > 10m
└── Experiencia: Al ganar experiencia

Mundo:
├── Metadata: Cada 5 minutos
├── Chunks: Al modificarse
├── Logs: En tiempo real
├── Eventos: En tiempo real
└── Seed: Solo al crear mundo
```

**Ventajas del Sistema**:

**✅ Organización Clara**:
- **Separación por mundo**: Cada mundo independiente
- **Estructura jerárquica**: Fácil navegación
- **Nomenclatura consistente**: Predecible y escalable
- **Modular**: Componentes independientes

**✅ Escalabilidad**:
- **Múltiples mundos**: Soporte ilimitado
- **Chunks dinámicos**: Generados bajo demanda
- **Personajes múltiples**: Sin límite por mundo
- **Logs rotativos**: Sin crecimiento infinito

**✅ Integridad**:
- **Checksums**: Detección de corrupción
- **Backups automáticos**: Recuperación garantizada
- **Validación**: Datos siempre consistentes
- **Recuperación**: Desde múltiples fuentes

---

### 📝 REGLA DE GESTIÓN DE MEMORIAS
**Fecha de Establecimiento**: 2026-03-11

**Regla Principal**: Solo puedo crear memorias bajo orden expresa del usuario.

**Aplicación**:
- No crearé memorias automáticamente
- No crearé memorias por iniciativa propia
- Solo crearé memorias cuando el usuario lo solicite explícitamente

**Excepciones**:
- Ninguna. Esta regla es absoluta.

**Motivo**:
El usuario ha establecido esta regla para tener control total sobre qué información se guarda como memoria persistente.

---

### 🎯 PREFERENCIAS DE FORMATO DE MEMORIAS
**Fecha de Establecimiento**: 2026-03-11

**Regla Principal**: Es preferible que los archivos de contexto tengan más frases cortas, pero concisas, que frases largas.

**Aplicación**:
- **Frases cortas**: Más fáciles de leer y procesar
- **Concisas**: Información densa y directa
- **Estructuradas**: Con formato claro y organizado
- **Eficientes**: Máxima información en mínimo espacio

**Motivo**:
- Mejor comprensión rápida
- Fácil escaneo visual
- Procesamiento más eficiente
- Retención de información mejor

---

## 📋 Memorias del Sistema (Grupo 7)

### 📊 SISTEMA DE BIOMAS
**Fecha de Establecimiento**: 2026-03-11

**Descripción General**: Sistema de biomas para Wild v2.0 que clasifica y aplica diferentes tipos de terreno según las coordenadas y alturas.

**Tipos de Biomas**:
- **Océano**: Aguas profundas (-100 a -20m)
- **Costa**: Playas y aguas poco profundas (-20 a 5m)
- **Pradera**: Terrenos planos y hierba (5 a 50m)
- **Bosque**: Árboles y vegetación densa (50 a 200m)
- **Montaña**: Terrenos elevados y rocosos (200 a 1000m)

**Sistema de Clasificación**:
- **Coordenadas**: Basado en CoordenadaGlobal
- **Altura**: Según altitud del terreno
- **Humedad**: Factor de ruido adicional
- **Temperatura**: Variación por latitud

**Características por Bioma**:
- **Vegetación**: Tipos de plantas y árboles
- **Recursos**: Materiales disponibles
- **Fauna**: Animales específicos
- **Color**: Paleta de colores del terreno

---

### 🎨 SISTEMA DE CALIDAD DINÁMICA
**Fecha de Establecimiento**: 2026-03-11

**Descripción General**: Sistema de calidad adaptativa que ajusta automáticamente la configuración gráfica según el hardware del usuario.

**Niveles de Calidad**:
- **Toaster**: Mínimo (256px texturas, chunks cercanos)
- **Low**: Bajo (512px texturas, distancia media)
- **Medium**: Medio (1024px texturas, distancia normal)
- **High**: Alto (2048px texturas, distancia extendida)
- **Ultra**: Máximo (4096px texturas, distancia máxima)

**Detección Automática**:
- **GPU**: VRAM y capacidad de procesamiento
- **CPU**: Núcleos y velocidad
- **RAM**: Memoria disponible
- **Disco**: Velocidad de almacenamiento

**Ajustes Dinámicos**:
- **Texturas**: Resolución según calidad
- **Distancia**: Render distance ajustable
- **FPS**: Target dinámico (30/60/120)
- **Efectos**: Partículas y post-procesado

---

### 🌐 SISTEMA DE RED
**Fecha de Establecimiento**: 2026-03-11

**Descripción General**: Sistema de red cliente-servidor para multijugador en tiempo real.

**Arquitectura**:
- **Servidor**: Autoritativo, centralizado
- **Cliente**: Ligero, predictivo
- **Protocolo**: TCP/IP confiable
- **Compresión**: Datos optimizados

**Mensajes**:
- **Transacciones**: Cambios de estado
- **Sincronización**: Estado del mundo
- **Eventos**: Interacciones
- **Control**: Conexión y desconexión

**Optimizaciones**:
- **Delta encoding**: Solo cambios
- **Batching**: Agrupar mensajes
- **Priorización**: Críticos primero
- **Culling**: Solo datos visibles

---

### 🎭 SISTEMA DE PERSONAJES
**Fecha de Establecimiento**: 2026-03-11

**Descripción General**: Sistema de creación y gestión de personajes para Wild v2.0.

**Creación de Personajes**:
- **Nombre**: Único por mundo
- **Apariencia**: Customizable
- **Habilidades**: Atributos iniciales
- **Equipamiento**: Básico inicial

**Sistema de Progresión**:
- **Experiencia**: Por acciones
- **Niveles**: Desbloqueo de capacidades
- **Habilidades**: Mejoras específicas
- **Estadísticas**: Vida, hambre, sed

**Persistencia**:
- **Datos**: Guardado automático
- **Inventario**: Items y equipamiento
- **Progreso**: Estadísticas y logros
- **Ubicación**: Última posición

---

### 🖥️ SISTEMA DE MENÚS Y ESCENAS
**Fecha de Establecimiento**: 2026-03-11

**Descripción General**: Sistema de interfaz de usuario y gestión de escenas para Wild v2.0.

**Menús Principales**:
- **Main Menu**: Crear/conectar mundos
- **Settings**: Configuración del juego
- **Character**: Creación de personajes
- **Pause**: Opciones durante juego

**Escenas del Juego**:
- **Game World**: Mundo principal
- **Inventory**: Gestión de items
- **Crafting**: Creación de objetos
- **Build**: Modo construcción

**UI Dinámica**:
- **Adaptativa**: Según resolución
- **Temática**: Consistente con biomas
- **Intuitiva**: Fácil navegación
- **Responsiva**: Diferentes dispositivos

---

### 🎮 SISTEMA DE RENDERIZADO
**Fecha de Establecimiento**: 2026-03-11

**Descripción General**: Sistema de renderizado 3D optimizado para mundos grandes.

**Técnicas de Render**:
- **Forward Rendering**: Rendimiento alto
- **LOD**: Niveles de detalle
- **Culling**: Frustum y oclusión
- **Batching**: Agrupar draw calls

**Optimizaciones**:
- **Instancing**: Múltiples objetos
- **Occlusion**: Ocultar objetos no visibles
- **Distance**: Calidad por distancia
- **Async**: Carga en background

**Efectos Visuales**:
- **Iluminación**: Dinámica y estática
- **Sombras**: Por calidad
- **Partículas**: Efectos atmosféricos
- **Post-procesado**: Color y filtros

---

### 📝 SISTEMA DE LOGGER
**Fecha de Establecimiento**: 2026-03-11

**Descripción General**: Sistema de logging estructurado para depuración y monitoreo.

**Niveles de Log**:
- **DEBUG**: Información de desarrollo
- **INFO**: Eventos importantes
- **WARNING**: Problemas potenciales
- **ERROR**: Errores graves
- **FATAL**: Errores críticos

**Salidas**:
- **Consola**: Tiempo real
- **Archivo**: Persistente
- **Red**: Servidor remoto
- **UI**: Panel de depuración

**Estructura**:
- **Timestamp**: Marca de tiempo
- **Sistema**: Origen del mensaje
- **Nivel**: Importancia
- **Mensaje**: Contenido descriptivo

---

### 📋 SISTEMA FDD - ESTRUCTURA
**Fecha de Establecimiento**: 2026-03-11

**Descripción General**: Estructura del sistema Feature-Driven Development para Wild v2.0.

**Directorios FDD**:
- **active/:** Feature en desarrollo
- **features/:** Features documentadas
- **completed/:** Features finalizadas
- **backlog/:** Features pendientes
- **templates/:** Plantillas
- **metrics/:** Métricas de progreso

**Flujo de Trabajo**:
1. **Definir**: Especificar feature
2. **Desarrollar**: Implementar incremental
3. **Probar**: Verificar funcionamiento
4. **Completar**: Mover a completed

**Plantillas**:
- **Feature.md**: Estructura estándar
- **Daily notes**: Registro diario
- **Metrics**: Seguimiento

---

### 🎯 SISTEMA DE MODELADO 3D Y LOD
**Fecha de Establecimiento**: 2026-03-11

**Descripción General**: Sistema de modelado 3D con niveles de detalle optimizados.

**Modelos 3D**:
- **Formato**: .glb (GLTF Binary)
- **LODs**: Múltiples niveles
- **Texturas**: Diferentes resoluciones
- **Optimización**: Polígonos y vértices

**Niveles de Detalle**:
- **LOD0**: Máxima calidad (cercano)
- **LOD1**: Alta calidad (medio)
- **LOD2**: Media calidad (lejano)
- **LOD3**: Baja calidad (muy lejano)

**Sistema de Carga**:
- **Adaptativa**: Según distancia
- **Progressiva**: Carga gradual
- **Cache**: Reutilización
- **Unload**: Liberación

---

### 🌐 SISTEMA DE NETWORK
**Fecha de Establecimiento**: 2026-03-11

**Descripción General**: Sistema de red para multijugador en tiempo real.

**Protocolo**:
- **Transporte**: TCP/IP
- **Formato**: Mensajes binarios
- **Compresión**: Datos optimizados
- **Seguridad**: Validación

**Arquitectura**:
- **Servidor**: Autoritativo
- **Cliente**: Ligero
- **Sincronización**: Estado compartido
- **Predicción**: Suavizado

**Manejo de Conexiones**:
- **Handshake**: Establecimiento
- **Heartbeat**: Mantenimiento
- **Timeout**: Desconexión
- **Reconexión**: Recuperación

---

## 📋 Memorias del Sistema (Grupo 8 - Final)

### 🔄 SISTEMA DE PERSISTENCIA DE DATOS
**Fecha de Establecimiento**: 2026-03-11

**Descripción General**: Sistema de persistencia de datos con transacciones atómicas y rollback automático.

**Transacciones**:
- **Atómicas**: Todo o nada
- **Secuenciales**: Orden garantizado
- **Reversibles**: Rollback automático
- **Validadas**: Integridad de datos

**Tipos de Datos**:
- **Mundo**: Configuración global
- **Jugador**: Estado individual
- **Chunk**: Datos de terreno
- **Inventario**: Items y equipamiento

**Mecanismos**:
- **Journaling**: Registro de cambios
- **Checkpoint**: Puntos de recuperación
- **Compresión**: Datos optimizados
- **Encriptación**: Seguridad opcional

---

### 🌍 SISTEMA DE COORDENADAS GLOBALES (EXTENDIDO)
**Fecha de Establecimiento**: 2026-03-11

**Descripción General**: Sistema extendido de coordenadas con soporte para múltiples dimensiones.

**Coordenadas Extendidas**:
- **3D**: x, y, z (espaciales)
- **Temporal**: Timestamps
- **Dimensional**: Múltiples mundos
- **Relativa**: Posiciones relativas

**Transformaciones**:
- **Rotación**: Ángulos y quaterniones
- **Escalado**: Factores de escala
- **Proyección**: 2D/3D conversiones
- **Interpolación**: Posiciones intermedias

**Validación**:
- **Rangos**: Límites de coordenadas
- **Tipos**: Validación de datos
- **Consistencia**: Coherencia espacial
- **Integridad**: Sin corrupción

---

### 👥 SISTEMA DE GESTIÓN DE SESIONES (DETALLES)
**Fecha de Establecimiento**: 2026-03-11

**Descripción General**: Sistema avanzado de gestión de sesiones con soporte para reconexión y estado persistente.

**Estados de Sesión**:
- **Inicializando**: Configurando conexión
- **Conectado**: Conexión activa
- **Sincronizando**: Sincronizando estado
- **Activo**: Jugando normalmente
- **Pausado**: Sesión en pausa
- **Desconectando**: Cerrando sesión
- **Error**: Estado de error

**Mecanismos**:
- **Heartbeat**: Mantener vivo
- **Reconexión**: Automática
- **Buffer**: Mensajes pendientes
- **Timeout**: Desconexión automática

**Seguridad**:
- **Autenticación**: Validación de identidad
- **Autorización**: Permisos de acceso
- **Encriptación**: Comunicación segura
- **Auditoría**: Registro de eventos

---

### 🧠 SISTEMA DE OPTIMIZACIÓN DE MEMORIA (IMPLEMENTACIÓN)
**Fecha de Establecimiento**: 2026-03-11

**Descripción General**: Sistema avanzado de gestión de memoria con garbage collection manual.

**Estrategias**:
- **Pooling**: Reutilización de objetos
- **Caching**: Almacenamiento inteligente
- **Compresión**: Reducción de tamaño
- **Streaming**: Carga progresiva

**Mecanismos**:
- **Reference Counting**: Conteo de referencias
- **Weak References**: Referencias débiles
- **Memory Pools**: Pools de memoria
- **Garbage Collection**: Limpieza manual

**Métricas**:
- **Uso**: Memoria utilizada
- **Fragmentación**: Espacio perdido
- **Peak**: Máximo uso
- **Leaks**: Fugas de memoria

---

### ⚡ SISTEMA DE CARGA ASÍNCRONA (DETALLES)
**Fecha de Establecimiento**: 2026-03-11

**Descripción General**: Sistema de carga asíncrona con prioridades y cancelación.

**Prioridades**:
- **Crítica**: Jugador y UI
- **Alta**: Terreno cercano
- **Media**: Objetos lejanos
- **Baja**: Background assets

**Mecanismos**:
- **Threading**: Múltiples hilos
- **Queue**: Cola de tareas
- **Cancelación**: Cancelación de tareas
- **Progress**: Progreso de carga

**Optimizaciones**:
- **Batching**: Agrupar tareas
- **Prefetching**: Carga anticipada
- **Compression**: Datos comprimidos
- **Streaming**: Carga continua

---

### 🎯 SISTEMA DE LOD (IMPLEMENTACIÓN AVANZADA)
**Fecha de Establecimiento**: 2026-03-11

**Descripción General**: Sistema de nivel de detalle con transiciones suaves y adaptación dinámica.

**Niveles de LOD**:
- **LOD0**: Máxima calidad (0-50m)
- **LOD1**: Alta calidad (50-100m)
- **LOD2**: Media calidad (100-200m)
- **LOD3**: Baja calidad (200-500m)
- **LOD4**: Mínima calidad (500m+)

**Transiciones**:
- **Smooth**: Transiciones suaves
- **Crossfade**: Fundidos cruzados
- **Morphing**: Deformación progresiva
- **Instant**: Cambios instantáneos

**Adaptación**:
- **Dynamic**: Según rendimiento
- **Distance**: Por distancia
- **Importance**: Por importancia
- **User**: Preferencias del usuario

---

### ⚔️ SISTEMA DE HABILIDADES (MECÁNICAS AVANZADAS)
**Fecha de Establecimiento**: 2026-03-11

**Descripción General**: Sistema de habilidades con synergies y árboles de progreso.

**Categorías**:
- **Combate**: Lucha y defensa
- **Supervivencia**: Recolección y caza
- **Crafting**: Creación y mejora
- **Exploración**: Navegación y descubrimiento

**Mecánicas**:
- **XP**: Puntos de experiencia
- **Levels**: Niveles de habilidad
- **Synergies**: Bonificaciones combinadas
- **Mastery**: Dominio de habilidades

**Progresión**:
- **Linear**: Progresión lineal
- **Exponential**: Progresión exponencial
- **Diminishing Returns**: Rendimientos decrecientes
- **Caps**: Límites máximos

---

### 📋 METODOLOGÍA FDD (IMPLEMENTACIÓN)
**Fecha de Establecimiento**: 2026-03-11

**Descripción General**: Implementación completa del sistema Feature-Driven Development.

**Proceso**:
1. **Feature Definition**: Especificación detallada
2. **Feature Planning**: Planificación de tareas
3. **Feature Development**: Implementación
4. **Feature Testing**: Pruebas y validación
5. **Feature Completion**: Finalización y documentación

**Herramientas**:
- **Templates**: Plantillas estandarizadas
- **Checklists**: Listas de verificación
- **Metrics**: Métricas de progreso
- **Reviews**: Revisión de código

**Best Practices**:
- **Small Features**: Features pequeñas
- **Fast Delivery**: Entrega rápida
- **Continuous**: Desarrollo continuo
- **Quality**: Calidad garantizada

---

### 📊 SISTEMA DE OPTIMIZACIÓN (MÉTRICAS)
**Fecha de Establecimiento**: 2026-03-11

**Descripción General**: Sistema completo de métricas y optimización de rendimiento.

**Métricas**:
- **FPS**: Frames por segundo
- **Memory**: Uso de memoria
- **CPU**: Uso de procesador
- **Network**: Ancho de banda

**Optimizaciones**:
- **Culling**: Eliminación de objetos
- **Batching**: Agrupación de operaciones
- **LOD**: Niveles de detalle
- **Async**: Operaciones asíncronas

**Monitoreo**:
- **Real-time**: En tiempo real
- **Historical**: Datos históricos
- **Profiling**: Análisis de rendimiento
- **Alerts**: Alertas automáticas

---

### 🔧 REGLAS ADICIONALES DE DESARROLLO
**Fecha de Establecimiento**: 2026-03-11

**Descripción General**: Reglas adicionales para mantener calidad y consistencia.

**Reglas de Código**:
- **Consistente**: Estilo uniforme
- **Documentado**: Código comentado
- **Testeado**: Con pruebas unitarias
- **Refactorizado**: Código limpio

**Reglas de Diseño**:
- **Modular**: Componentes independientes
- **Escalable**: Crecimiento soportado
- **Mantenible**: Fácil de mantener
- **Extensible**: Fácil de extender

**Reglas de Proceso**:
- **Iterativo**: Desarrollo iterativo
- **Incremental**: Entregas incrementales
- **Validado**: Validación continua
- **Documentado**: Documentación actualizada

---

## 📈 Resumen de Memorias

**Total de Memorias Añadidas**: 50
- **Grupo 1**: 5 memorias (Sistemas fundamentales)
- **Grupo 2**: 5 memorias (Sistemas de datos y gestión)
- **Grupo 3**: 5 memorias (Sistemas de red y arquitectura)
- **Grupo 4**: 5 memorias (Sistemas de optimización)
- **Grupo 5**: 5 memorias (Reglas y metodología)
- **Grupo 6**: 5 memorias (Sistemas técnicos)
- **Grupo 7**: 10 memorias (Sistemas de juego)
- **Grupo 8**: 10 memorias (Implementación avanzada)

**Categorías Cubiertas**:
- ✅ **Arquitectura**: Diseño y estructura
- ✅ **Sistemas**: Componentes principales
- ✅ **Reglas**: Directrices de desarrollo
- ✅ **Optimización**: Rendimiento y memoria
- ✅ **Red**: Comunicación multijugador
- ✅ **Persistencia**: Guardado de datos
- ✅ **UI**: Interfaces de usuario
- ✅ **Render**: Visualización 3D
- ✅ **Física**: Simulación física
- ✅ **Calidad**: Adaptación automática

**Estado**: ✅ **Completo** - Todas las memorias fundamentales del proyecto Wild v2.0 han sido documentadas.

---

**Este archivo ahora contiene el registro completo de todas las decisiones arquitectónicas, sistemas y reglas del proyecto Wild v2.0, sirviendo como referencia definitiva para el desarrollo futuro.**
