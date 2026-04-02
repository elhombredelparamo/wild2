# 🗺️ Mapa del Proyecto Wild v2.0

**Este documento es la guía principal para Cascade en nuevas conversaciones.**

## 🎯 Visión General
- **Proyecto:** Wild v2.0 - Juego multijugador supervivencia con terreno procedural
- **Tecnología:** Godot 4.6.1 + C# (.NET 8.0)
- **Metodología:** Feature-Driven Development (FDD)
- **Objetivo:** Versión limpia aplicando lecciones del proyecto actual

## 📂 Estructura Clave

### 🎮 Proyecto de Programación
- `project.godot` - Configuración del proyecto Godot (Raíz)
- `Wild.csproj` - Proyecto C# (.NET 8.0)
- `Wild.sln` - Solución Visual Studio
- `scripts/` - **Directorio principal de scripts C#**
- `scripts/data/MundoManager.cs` - Gestor de mundos y persistencia
- `scripts/Core/GameLoader.cs` - Cargador de escenas asíncrono
- `scenes/ui/splash_screen.tscn` - Escena de inicio (Main Scene)

### Sistema FDD (Gestión del Proyecto)
- `fdd/active/` - Feature actual en desarrollo
- `fdd/features/` - Features individuales documentadas
- `fdd/completed/` - Features finalizadas
- `fdd/backlog/` - Features pendientes
- `fdd/templates/` - Plantillas estandarizadas
- `fdd/metrics/` - Métricas de progreso

### 📚 **Documentación Técnica**
- `contexto/resumen.md` - Plan completo del proyecto
- `contexto/diseño.md` - Arquitectura y diseño técnico
- `contexto/biomas.md` - Sistema de biomas
- `contexto/calidad.md` - Sistema de calidad dinámica
- `contexto/network.md` - Sistema de red
- `contexto/modelado3d.md` - Modelado 3D y LOD
- `contexto/menus-y-escenas.md` - Sistema de menús
- `contexto/render.md` - Sistema de renderizado
- `contexto/personajes.md` - Sistema de personajes
- `contexto/logger.md` - Sistema de logging
- `contexto/fdd-estructura.md` - Estructura del sistema FDD

### Código de Referencia
- `codigo/` - **Documentación técnica y pseudocódigo**
- `codigo/core/` - Sistemas centrales (terreno, red, biomas)
- `codigo/data/` - Sistemas de datos (persistencia, conexión)
- `codigo/utils/` - Utilidades (logger, coordenadas)
- `codigo/systems/` - Sistemas de juego (jugador, UI)

### Estructura de Aplicación
- `scripts/Core/` - Sistemas centrales (Terrain, Quality, Player)
- `scripts/data/` - Persistencia y gestión de datos (Mundos, Personajes)
- `scripts/Utils/` - Utilidades generales y Logger
- `scenes/` - Escenas de Godot (.tscn)
- `assets/` - Recursos visuales y sonoros

## 🔄 Flujo de Trabajo Actual

### 1. Trabajar en el Proyecto Principal
Desarrollar en la raíz del repositorio con Godot Editor (4.6.1) y Visual Studio.

### 2. Verificar Feature Activa
Revisar `fdd/active/current-feature.md` para saber qué se está desarrollando.

### 3. Consultar Notas Diarias
Ver `fdd/active/daily-notes/` para progreso reciente y decisiones.

### 4. Referencia Técnica
Usar archivos en `contexto/` y `codigo/` según necesidad específica.

## 🚀 Arquitectura Principal

### Sistema de Terreno Unificado
- Coordenadas globales únicas
- Chunks de 10x10 para culling eficiente
- Generación procedural asíncrona
- Sistema de biomas integrado

### Sistema de Calidad Dinámica
- 5 niveles: Toaster, Low, Medium, High, Ultra
- Detección automática de hardware
- Adaptación en tiempo real

### Sistema de Red Cliente-Servidor
- Protocolo estandarizado
- Sincronización de terreno
- Gestión de jugadores

## 📋 Features Planificadas

1. **Feature 1:** Sistema de Interfaces Básico ✅
2. **Feature 2:** Sistema de Logger ✅
3. **Feature 3:** Sistema de Personajes ✅
4. **Feature 4:** Sistema de Mundos ✅
5. **Feature 5:** Sistema de Terreno y Biomas ✅
   - 5.1 **Feature 5.1:** Bioma Océano (<-20m)
   - 5.2 **Feature 5.2:** Bioma Costa (<10m, requiere proximidad al mar)
   - 5.3 **Feature 5.3:** Bioma Pradera (Tierra firme, baja humedad)
   - 5.4 **Feature 5.4:** Bioma Bosque (Tierra firme, alta humedad)
   - 5.5 **Feature 5.5:** Bioma Montaña (>=50m)
6. **Feature 6:** Sistema de Jugador ✅ (física y controles)
7. **Feature 7:** Sistema de Objetos 3D ✅ (completada)
8. **Feature 8:** Sistema de Calidad ✅ (completada)
9. **Feature 9:** Sistema de Inventario ✅ (completada)
10. **Feature 10:** Sistema de Deployables ✅ (completada)
11. **Feature 11:** Sistema de Crafteos (En progreso - Materiales base integrados: Piedra, Rama, Fibra)
12. **Feature 12:** Sistema de Salud (2 días)
13. **Feature 13:** Red y Multijugador (3 días)
14. **Feature 14:** Pulido Previo Beta (2 días)
15. **Feature 15:** Contenido de Océano y Sistema de Agua (3 días)
16. **Feature 16:** Contenido de Costa (2 días)
17. **Feature 17:** Contenido de Pradera (2 días)
18. **Feature 18:** Contenido de Bosque (2 días)
19. **Feature 19:** Contenido de Montaña (2 días)
20. **Feature 20:** Deployables y Crafteos Iniciales (3 días)

## 🎯 Estándares del Proyecto

### Código
- Singleton pattern con inicialización controlada
- Sistema de logging estándar (no GD.Print)
- Coordenadas globales únicamente
- Nomenclatura consistente

### Desarrollo
- Feature-Driven Development (FDD)
- Documentación .pseudo para componentes
- Métricas de progreso en `fdd/metrics/`
- Plantillas estandarizadas

### Rendimiento
- 60 FPS constante
- Carga rápida de chunks (< 10ms)
- Memoria eficiente (< 200MB)
- Compatible con hardware diverso

## 🔗 Referencias Rápidas

### Para Consultar el Estado Actual:
1. `fdd/active/current-feature.md` - Feature en desarrollo
2. `fdd/active/daily-notes/` - Progreso diario
3. `fdd/metrics/velocity-tracking.md` - Velocidad de desarrollo

### Para Decisiones Técnicas:
1. `contexto/diseño.md` - Arquitectura completa
2. `contexto/resumen.md` - Plan del proyecto
3. `contexto/fdd-estructura.md` - Sistema de gestión

### Para Implementación Específica:
- Biomas: `contexto/biomas.md`
- Red: `contexto/network.md`
- Render: `contexto/render.md`
- Calidad: `contexto/calidad.md`

## ⚡ Inicio Rápido

Al iniciar una nueva conversación:
1. **Abrir proyecto Godot:** Abrir la raíz del repositorio en Godot Editor.
2. Revisar feature activa en `fdd/active/`
3. Consultar notas diarias recientes
4. Verificar métricas de progreso
5. Usar documentación técnica según necesidad

### 🎯 Para Desarrollo
- **Proyecto principal:** Raíz del repositorio (Godot + C#)
- **Scripts:** `scripts/`
- **Referencia técnica:** `codigo/` (pseudocódigo y diseño)
- **Documentación:** `contexto/` (especificaciones)
- **Gestión:** `fdd/` (features y progreso)

---
**Este mapa proporciona toda la información necesaria para entender y trabajar en Wild v2.0 sin necesidad de exploración adicional.**
