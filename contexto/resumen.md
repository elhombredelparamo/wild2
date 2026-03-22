# Wild v2.0 - Plan de Nuevo Proyecto Limpio

## 🎯 Objetivo Principal

Crear una versión limpia y eficiente del juego Wild, aplicando las lecciones aprendidas del proyecto actual para evitar problemas arquitectónicos y de rendimiento.

## 📋 Contexto del Proyecto Actual

### Problemas Identificados
- **Arquitectura compleja:** Múltiples sistemas de renderizado conviviendo (antiguo + nuevo)
- **Deuda técnica:** Código legacy que no se puede eliminar sin romper funcionalidad
- **Coordenadas inconsistentes:** Mezcla de sistemas local/global causando errores
- **Sistemas sobreingenierados:** TerrainManager + DynamicChunkLoader + SubChunks + ChunkRenderer
- **Problemas de inicialización:** BiomaManager con dependencias circulares
- **Rendimiento pobre:** Bucles infinitos y generación ineficiente de chunks (100x100 demasiado grandes)
- **Culling ineficiente:** Chunks grandes dificultan eliminación precisa de contenido no visible

### Lecciones Aprendidas
- **Sistema de coordenadas global:** Funciona mejor y evita conversiones
- **Renderizado unificado:** Un solo sistema es más eficiente que múltiples
- **Inicialización controlada:** Los singletons necesitan inicialización ordenada
- **Generación asíncrona:** Esencial para buen rendimiento
- **Mallas simples:** Mejor empezar con algo visible y luego mejorar
- **Chunks pequeños (10x10):** Permiten culling eficiente y generación rápida
- **Culling granular:** Esencial para buen rendimiento con render distance

## 🏗️ Arquitectura Implementada (Actualización 2026-03-11)

### ✅ **Sistemas Completados**
- **Arquitectura Servidor-Cliente:** Separación clara de responsabilidades
- **Sistema Multi-Personaje:** Gestión de múltiples personajes por cliente (F3 Completado)
- **Persistencia Continua:** Archivos .dat y .json en carpetas de usuario (`user://characters/`)
- **Protocolo de Red:** Comunicación robusta cliente-servidor (Diseñado)
- **Sistema de Calidad:** 5 niveles dinámicos (Toaster → Ultra)
- **Reglas Fundamentales:** Terreno 100% natural, logging propio (F2 Completado)

### 🔄 **Cambios Realizados**
- **Persistencia:** De archivos locales a servidor-cliente con .dat
- **Personajes:** Sistema multi-personaje con datos por mundo
- **Conexión:** IP:puerto en lugar de archivos locales
- **Calidad:** 5 niveles incluyendo "Toaster Quality" para hardware básico
- **Terreno:** 100% natural, eliminado bioma "Ciudad"
- **UI:** Menús de gestión de personajes y conexión a servidor

### 📁 **Estructura de Archivos Actualizada**
```
codigo/
├── core/
│   ├── personajes.pseudo      # Sistema multi-personaje
│   ├── red.pseudo             # Protocolo de red
│   ├── ui.pseudo              # Menús multi-personaje
│   ├── jugador.pseudo         # Sistema de jugador ampliado
│   ├── calidad.pseudo         # Sistema de calidad dinámica
│   └── biomas.pseudo          # Biomas 100% naturales
├── data/
│   ├── persistence.pseudo     # Servidor-cliente .dat
│   └── world-connection.pseudo # Conexión a servidor
└── utils/
    └── logger.pseudo          # Sistema de logging propio
```

### 🎯 **Estado Actual del Proyecto**
- **✅ Arquitectura Completa:** Todos los sistemas definidos
- **✅ Multi-Personaje:** Sistema completo implementado
- **✅ Servidor-Cliente:** Persistencia centralizada
- **✅ Reglas Establecidas:** Terreno natural, logging propio
- **🔄 Listo para Implementación:** Base sólida para Godot/C#

## 📋 Contexto del Proyecto Actual
### Problemas Identificados
- **Arquitectura compleja:** Múltiples sistemas de renderizado conviviendo (antiguo + nuevo)
- **Deuda técnica:** Código legacy que no se puede eliminar sin romper funcionalidad
- **Coordenadas inconsistentes:** Mezcla de sistemas local/global causando errores
- **Sistemas sobreingenierados:** TerrainManager + DynamicChunkLoader + SubChunks + ChunkRenderer
- **Problemas de inicialización:** BiomaManager con dependencias circulares
- **Rendimiento pobre:** Bucles infinitos y generación ineficiente de chunks (100x100 demasiado grandes)
- **Culling ineficiente:** Chunks grandes dificultan eliminación precisa de contenido no visible

### Lecciones Aprendidas
- **Sistema de coordenadas global:** Funciona mejor y evita conversiones
- **Renderizado unificado:** Un solo sistema es más eficiente que múltiples
- **Inicialización controlada:** Los singletons necesitan inicialización ordenada
- **Generación asíncrona:** Esencial para buen rendimiento
- **Mallas simples:** Mejor empezar con algo visible y luego mejorar
- **Chunks pequeños (10x10):** Permiten culling eficiente y generación rápida
- **Culling granular:** Esencial para buen rendimiento con render distance

## 🏗️ Arquitectura Propuesta para Wild v2.0

### Principios de Diseño
1. **Simplicidad:** Menos componentes, mejor integrados
2. **Modularidad:** Sistemas desacoplados con responsabilidades claras
3. **Coherencia:** Un solo sistema de coordenadas global
4. **Eficiencia:** Generación asíncrona y culling inteligente
5. **Escalabilidad:** Diseño que permita añadir funcionalidad sin refactorización

### Estructura de Componentes

#### 🌍 Sistema de Terreno (Unificado)
```
TerrainSystem
├── ChunkGenerator (generación procedural)
├── BiomaSystem (integrado)
├── Renderer (único)
└── CacheSystem
```

#### 🎮 Sistema de Juego
```
GameWorld
├── PlayerController
├── CameraController
├── NetworkManager (simplificado)
└── UIController
```

#### 🌐 Sistema de Red
```
NetworkSystem
├── Server (lógica centralizada)
├── Client (representación local)
└── Protocol (mensajes estandarizados)
```

## 🔄 Flujo de Renderizado Propuesto

### Generación de Chunks
1. **Player Movement** → Detectar posición del jugador
2. **Chunk Request** → Solicitar chunks en radio visible
3. **Async Generation** → Generar terreno con biomas en background
4. **Mesh Creation** → Crear mallas optimizadas
5. **Material Application** → Aplicar materiales según bioma
6. **Culling** → Eliminar chunks fuera del radio visible

### Sistema de Coordenadas
- **Único sistema:** Coordenadas globales para todo
- **Chunk Grid:** 10x10 unidades por chunk (optimizado para culling)
- **Player Position:** Vector3 global
- **No conversiones:** Eliminar transformaciones local↔global
- **Render Distance:** 500-1000 unidades (50-100 chunks radio)
- **Culling Eficiente:** Granularidad precisa con chunks pequeños

## 📦 Componentes a Reutilizar

### ✅ Adaptar del Proyecto Actual
- **BiomaManager:** Simplificar inicialización, eliminar dependencias
- **ChunkRenderer:** Corregir para Godot 4, optimizar generación
- **MaterialCache:** Simplificar, eliminar complejidad innecesaria
- **BiomaPreProcessor:** Integrar directamente en generación
- **SubChunkCuller:** Adaptar para nuevo sistema
- **ModelSpawner:** Integrar con sistema de calidad dinámica
- **QualityManager:** Crear nuevo gestor de calidad adaptativa
- **SceneManager:** Reutilizar sistema de menús y transiciones
- **MenuManager:** Aprovechar estructura de menús existente
- **HUD:** Adaptar interfaz a calidad dinámica

### ❌ Descartar
- **TerrainManager:** Demasiado complejo, reemplazar con sistema simple
- **DynamicChunkLoader:** Lógica innecesaria con nuevo diseño
- **SubChunk system:** Problemas de coordenadas, rediseñar
- **Sistemas duales:** Mantener solo uno de cada tipo
- **Carga estática de recursos:** Reemplazar con carga dinámica

### 🆕 Crear desde Cero
- **TerrainSystem:** Unificado y simple
- **ChunkManager:** Control centralizado de chunks
- **CoordinateSystem:** Único sistema global
- **NetworkProtocol:** Diseñado para arquitectura limpia
- **DynamicResourceLoader:** Carga adaptativa de recursos
- **QualityManager:** Gestión de calidad dinámica
- **PauseMenu:** Menú de pausa integrado con GameWorld

## 🚀 Metodología de Desarrollo: Feature-Driven Development (FDD)

### 📋 **Sistema de Gestión de Features**
Este proyecto utiliza **Feature-Driven Development (FDD)** para garantizar un desarrollo estructurado y trazable. Cada fase del plan se implementa como una feature independiente con documentación completa.

### 🗂️ **Estructura de Gestión FDD**
```
wild-new/
├── fdd/               # Sistema de gestión FDD
│   ├── features/      # Archivos individuales de features
│   ├── active/        # Feature actual en desarrollo
│   │   ├── current-feature.md
│   │   ├── daily-notes/   # Notas diarias
│   │   └── design-decisions.md
│   ├── completed/     # Features finalizadas
│   ├── backlog/       # Features pendientes
│   ├── templates/     # Plantillas estandarizadas
│   └── metrics/       # Métricas de velocidad y calidad
└── contexto/          # Documentación del proyecto
    ├── resumen.md     # Guía de desarrollo (este archivo)
    └── [demás archivos de documentación]
```

### 🔄 **Flujo de Trabajo FDD**
1. **Asignación de Feature**: Seleccionar del backlog y## 🚀 Plan de Desarrollo (Features FDD)

### Feature 1: Sistema de Interfaces Básico ✅
- [x] Crear proyecto Godot + C# limpio
- [x] Implementar estructura base de escenas y menús
- [x] Configuración inicial de UI responsiva

### Feature 2: Sistema de Logger ✅
- [x] Implementar clase Logger estática
- [x] Soporte para persistencia en archivo `latest.log`
- [x] Integración en menús principales

### Feature 3: Sistema de Personajes ✅
- [x] Crear `PersonajeManager` (Singleton)
- [x] Implementar creación y selección de múltiples personajes
- [x] Persistencia en `characters/` y selección persistente en `selected.dat`
- [x] Garantía de personaje por defecto y protección contra borrado accidental
- [x] Integración visual en menús de conexión y nueva partida

### Feature 4: Sistema de Mundos ✅
- [x] Gestión de archivos de mundo por personaje
- [x] Menú de creación de mundo con seeds
- [x] Persistencia de estado de mundo independiente

### Feature 5: Sistema de Terreno y Biomas (3 días)
#### Feature 5.1: Bioma Océano (<-20m)
- [ ] Adaptar base procedural para chunks 10x10
- [ ] Implementar bioma Océano

#### Feature 5.2: Bioma Costa (-20 a 5m)
- [ ] Implementar bioma Costa

#### Feature 5.3: Bioma Pradera (5-50m)
- [ ] Implementar bioma Pradera

#### Feature 5.4: Bioma Bosque (50-200m)
- [ ] Implementar bioma Bosque

- [ ] Sistema de guardado/carga
- [ ] Creación de personaje
- [ ] Configuración completa
- [ ] Testing de navegación

### Feature 7: Pulido y Contenido (2 días)
- [ ] Mejorar materiales y texturas
- [ ] Añadir variación de terreno
- [ ] UI mejorada con configuración de calidad
- [ ] Testing final
- [ ] Documentación
- [ ] Testing de compatibilidad

## 🎯 Metas de Calidad

### Rendimiento
- **60 FPS** constante en movimiento
- **Carga rápida** de chunks (< 10ms para 10x10)
- **Memoria eficiente** (< 200MB para mundo básico)
- **Culling efectivo** con chunks pequeños
- **Calidad dinámica** adaptativa según hardware

### Estabilidad
- **Sin crashes** por coordenadas
- **Sin memory leaks**
- **Inicialización limpia** sin dependencias circulares
- **Reinicio automático** al cambiar calidad

### Mantenibilidad
- **Código modular** y desacoplado
- **Documentación clara** de cada sistema
- **Testing unitario** de componentes clave

### Compatibilidad
- **Hardware diverso:** Soporte para 2GB a 32GB RAM
- **GPU integrada:** Compatible con gráficas básicas
- **Calidad adaptable:** 5 niveles de calidad dinámica (Toaster, Low, Medium, High, Ultra)
- **Detección automática:** Configuración según hardware
- **Modo tostadora:** Funcional en sistemas extremadamente básicos

## 📂 Estructura Final del Proyecto:
```
wild-new/
├── programa/                   # **Proyecto Godot C# principal**
│   ├── project.godot          # Configuración del proyecto Godot
│   ├── Wild.csproj            # Proyecto C# (.NET 8.0)
│   ├── Wild.sln               # Solución Visual Studio
│   ├── Main.cs                # Clase principal del juego
│   └── [scripts C#]           # Código fuente del juego
├── fdd/                        # Sistema de gestión FDD
│   ├── features/              # Archivos de features individuales
│   ├── active/                # Feature actual en desarrollo
│   ├── completed/             # Features finalizadas
│   ├── backlog/               # Features pendientes
│   ├── templates/             # Plantillas FDD
│   └── metrics/               # Métricas de progreso
├── codigo/                     # **Documentación técnica y pseudocódigo**
│   ├── core/                  # Sistemas centrales (terreno, red, biomas)
│   ├── data/                  # Sistemas de datos (persistencia, conexión)
│   ├── utils/                 # Utilidades (logger, coordenadas)
│   └── systems/               # Sistemas de juego (jugador, UI)
└── contexto/
    ├── resumen.md              # Guía de desarrollo actualizada (este archivo)
    ├── memorias.md             # Sistema de conocimiento persistente
    ├── nuevo-flujo.md          # Flujo optimizado del juego
    ├── render.md               # Sistema de renderizado con física
    ├── network.md              # Sistema de red optimizado
    ├── logger.md               # Sistema de logging estándar (user://latest.log)
    ├── personajes.md           # Sistema de personajes realista (user://characters/)
    ├── biomas.md               # Sistema de biomas realista (user://worlds/[character_id]/[world_id]/)
    ├── modelado3d.md           # Sistema de modelado 3D con calidad dinámica
    ├── calidad.md              # Sistema de calidad dinámica con nivel Toaster
    └── menus-y-escenas.md      # Sistema de menús y escenas reutilizado
```

## 🔗 Referencias del Proyecto Actual

### Archivos Útiles para Consulta
- `contexto/resumen.txt` - Visión general del proyecto actual
- `scripts/render/` - Componentes de renderizado a adaptar
- `scripts/biomas/` - Sistema de biomas a simplificar

### Patrones a Mantener
- Singleton pattern para managers (con inicialización controlada)
- Sistema de logging estándar del proyecto
- Coordenadas globales como estándar
- Generación procedural con biomas

### Patrones a Evitar
- Sistemas duales conviviendo
- Inicialización desordenada de singletons
- Conversión constante entre sistemas de coordenadas
- Dependencias circulares entre componentes

## 📝 Notas de Desarrollo

### Tecnologías
- **Godot 4.6.1** + **C# (.NET 8.0)**
- **Arquitectura servidor-cliente** con persistencia centralizada
- **Generación procedural** de terreno 100% natural
- **Sistema de biomas** extensibles y modulares
- **Multi-personaje** con datos separados por mundo

### Estándares del Proyecto
- **Metodología:** Feature-Driven Development (FDD) con seguimiento estructurado
- **Logging:** Logger propio del proyecto (no GD.Print)
- **Coordenadas:** Global únicamente para todo el sistema
- **Nomenclatura:** Consistente con proyecto actual
- **Documentación:** .pseudo files para cada componente + tracking FDD
- **Gestión:** Sistema de archivos en `fdd/` para progreso y métricas
- **Reglas:** Terreno 100% natural, solo logging propio, memorias bajo orden

### 🎯 **Estado Actual (Marzo 2026)**
- **✅ Feature 0 Completada:** Arquitectura y pseudocódigo completo
- **🔄 Sistemas Listos:** Base sólida para implementación en Godot/C#
- **📋 Próximo Paso:** Feature 1 - Prototipo Base Terreno
- **🏗️ Arquitectura:** Servidor-cliente con persistencia .dat
- **🎮 Características:** Multi-personaje, calidad dinámica, terreno natural

---

**Este documento sirve como guía para recrear Wild con una arquitectura limpia y eficiente, aplicando todas las lecciones aprendidas del desarrollo actual. El proyecto utiliza Feature-Driven Development (FDD) para garantizar un desarrollo estructurado, trazable y con métricas claras de progreso.**
