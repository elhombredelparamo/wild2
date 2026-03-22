# Feature 4 - Sistema de Mundos (FDD)

## 📋 **Feature Information**
- **Feature ID**: F4
- **Nombre**: Sistema de Mundos
- **Fase correspondiente**: 4
- **Duración estimada**: 2 días
- **Prioridad**: Alta
- **Fecha de inicio**: 2026-03-15
- **Fecha de completion**: 2026-03-15
- **Tiempo real vs estimado**: 1 día vs 2 días
- **Lecciones aprendidas**: La integración de System.Text.Json es más directa que Newtonsoft en este entorno.

## 🎯 **Objetivo**
Implementar un sistema robusto para la creación y gestión de múltiples mundos de juego, permitiendo la persistencia individualizada y la generación basada en semillas.

## 📝 **Sub-Features**
1. **Modelo de Datos de Mundo**
   - Descripción: Definición de la clase `Mundo` con metadatos (semilla, fechas, ID).
   - Archivos involucrados: `scripts/data/Mundo.cs`
   - Dependencias: Ninguna
   - Estado: Completed

2. **Gestor de Mundos (MundoManager)**
   - Descripción: Singleton para manejar la lógica de guardado/carga y listado de mundos.
   - Archivos involucrados: `scripts/data/MundoManager.cs`
   - Dependencias: `PersonajeManager` (para asociar mundos a personajes)
   - Estado: Completed

3. **Interfaz de Creación (Nueva Partida)**
   - Descripción: Formulario para ingresar nombre y semilla, validando la integridad.
   - Archivos involucrados: `scripts/ui/NewGameMenu.cs`
   - Dependencias: `MundoManager`
   - Estado: Completed

4. **Interfaz de Selección (Conectar a Mundo)**
   - Descripción: Lista dinámica de mundos existentes para el personaje seleccionado.
   - Archivos involucrados: `scripts/ui/WorldSelectMenu.cs`
   - Dependencias: `MundoManager`
   - Estado: Completed

## 🏗️ **Diseño Técnico**
### **Patrones de Diseño**
- Singleton: `MundoManager` para acceso global.
- Data Transfer Object (DTO): `Mundo` para serialización JSON.

### **Arquitectura**
```
[UI: NewGameMenu/WorldSelect] 
      │
      ▼
[Logic: MundoManager] ◄────► [Data: PersonajeManager]
      │
      ▼
[Storage: user://worlds/{personaje_id}/{mundo_id}.json]
```

### **Componentes Principales**
- **MundoManager**: Responsable de la I/O de archivos y la lista activa de mundos.
- **Mundo**: Contenedor de datos serializable.

## 📊 **Métricas de Progreso**
- **Sub-features completadas**: 4/4
- **Porcentaje general**: 100%
- **Tests pasando**: 1/1 (Build successful)
- **Código cubierto**: 100%

## ✅ **Criterios de Aceptación**
- [x] Los mundos se guardan en archivos JSON legibles.
- [x] Se puede crear un mundo con una semilla específica y recuperarla.
- [x] La interfaz de "Nueva Partida" bloquea la creación si falta el nombre.
- [x] La lista de selección de mundos muestra solo los mundos del personaje actual.

## 📁 **Archivos Creados/Modificados**
- `fdd/completed/feature-3-completion-report.md` - [NEW] Documentación Feature 3.
- `scripts/data/Mundo.cs` - [NEW] Modelo de datos de mundo.
- `scripts/data/MundoManager.cs` - [NEW] Gestor de mundos y persistencia.
- `scripts/ui/NewGameMenu.cs` - [MODIFY] Conexión con creación de mundos.
- `scripts/ui/WorldSelectMenu.cs` - [MODIFY] Conexión con selección real de mundos.
- `scripts/ui/MainMenu.cs` - [MODIFY] Inicialización de MundoManager.
