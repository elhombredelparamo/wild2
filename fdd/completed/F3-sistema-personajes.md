---
feature_id: F3
title: "Feature 3: Sistema de Personajes"
description: "Implementar sistema de selección y creación de personajes para Wild v2.0"
estimated_days: 2
priority: Alta
status: Completado
started: 2026-03-14 14:55 UTC+1 
completed: 2026-03-15 12:00 UTC+1 
progress: 100%

---

# Feature 3: Sistema de Personajes

## 🎯 Estado Actual
**Feature:** F3 - Sistema de Personajes  
**Estado:** ✅ Completado  
**Progreso:** 100%  
**Actividades Completadas:** Feature 2 (Sistema de Logger) completada, Personaje.cs y PersonajeManager.cs implementados

## 🎯 Objetivo Principal
Implementar el sistema completo de gestión de personajes incluyendo selección de personajes existentes, creación de nuevos personajes, y gestión de estado persistente.

## 📋 Componentes a Implementar

### 📝 **Sistema de Datos de Personajes**
- **Personaje.cs** - Clase de datos del personaje
- **PersonajeManager.cs** - Gestor centralizado de personajes
- **SessionData** - Variables globales de personaje activo

### 🎨 **UI de Selección de Personajes**
- **CharacterSelectMenu.cs** - Menú principal de selección
- **CharacterCard.cs** - Componente visual de personaje
- **CharacterList.cs** - Lista de personajes disponibles

### 🎮 **UI de Creación de Personajes**
- **CharacterCreateMenu.cs** - Menú de creación
- **CharacterCustomization.cs** - Sistema de personalización
- **CharacterPreview.cs** - Vista previa 3D del personaje

### 💾 **Persistencia de Personajes**
- **Serialización** - Guardar/cargar personajes en archivos .dat
- **Validación** - Verificación de integridad de datos
- **Garantía** - Personaje por defecto si no hay ninguno

## 🔧 Requisitos Técnicos

### 📊 **Estructura de Datos de Personaje**
```csharp
public class Personaje
{
    public string id;                  // ID único (GUID)
    public string nombre;              // Nombre del personaje
    public string apodo;              // Apodo o apodo
    public DateTime fecha_creacion;     // Fecha de creación
    public DateTime ultimo_acceso;     // Último acceso
    
    // Apariencia
    public Color color_cabello;        // Color de cabello
    public Color color_piel;           // Color de piel
    public Color color_ropa;           // Color de ropa
    public float altura;               // Altura en metros
    public string genero;              // Género (masculino/femenino/neutro)
    
    // Estadísticas
    public int nivel;                  // Nivel actual
    public int experiencia;            // Experiencia acumulada
    public float vida;                 // Vida actual
    public float vida_maxima;          // Vida máxima
    public float mana;                 // Mana actual
    public float mana_maxima;          // Mana máxima
    
    // Habilidades
    public List<string> habilidades;    // IDs de habilidades desbloqueadas
    public Dictionary<string, int> niveles_habilidades; // Niveles por habilidad
    
    // Equipamiento
    public Dictionary<string, string> equipamiento; // Slot -> ID de item
    public Dictionary<string, int> inventario;      // Item ID -> cantidad
}
```

### 🗂️ **Flujo de Creación de Personaje**
1. **CharacterCreateMenu** - Formulario básico
2. **Validación** - Nombre único y válido
3. **Personalización** - Colores, altura, género
4. **Preview 3D** - Vista previa en tiempo real
5. **Confirmación** - Guardar personaje
6. **Selección automática** - Marcar como personaje activo

### 🔄 **Flujo de Selección de Personaje**
1. **CharacterSelectMenu** - Lista de personajes disponibles
2. **CharacterCard** - Vista previa de cada personaje
3. **Detalles** - Estadísticas y equipamiento
4. **Selección** - Elegir personaje activo
5. **Carga** - Cargar datos completos
6. **Transición** - Ir al mundo con personaje

## 🎨 Componentes de UI

### 📋 **CharacterSelectMenu.cs**
```csharp
public partial class CharacterSelectMenu : Control
{
    private VBoxContainer _characterList;
    private Button _buttonCreateNew;
    private Button _buttonDelete;
    private Button _buttonSelect;
    private Button _buttonBack;
    
    public override void _Ready()
    {
        CargarPersonajesDisponibles();
        SetupEventos();
        Logger.LogInfo("CharacterSelectMenu: Menú de selección iniciado");
    }
    
    private void CargarPersonajesDisponibles()
    {
        // Cargar todos los personajes desde archivos .dat
        // Crear CharacterCard para cada personaje
        // Actualizar UI con lista de personajes
    }
    
    private void OnCreateNewPressed()
    {
        GetTree().ChangeSceneToFile("res://scenes/ui/character_create_menu.tscn");
    }
    
    private void OnSelectPressed(string personajeId)
    {
        // Seleccionar personaje y transicionar al mundo
        SessionData.Instance.id_personaje = personajeId;
        // Cargar mundo con personaje seleccionado
    }
}
```

### 📋 **CharacterCreateMenu.cs**
```csharp
public partial class CharacterCreateMenu : Control
{
    private LineEdit _inputNombre;
    private LineEdit _inputApodo;
    private OptionButton _optionGenero;
    private ColorPickerButton _colorCabello;
    private ColorPickerButton _colorPiel;
    private ColorPickerButton _colorRopa;
    private Slider _sliderAltura;
    private SubViewport _preview3D;
    
    public override void _Ready()
    {
        SetupDefaults();
        SetupEventos();
        SetupPreview3D();
        Logger.LogInfo("CharacterCreateMenu: Menú de creación iniciado");
    }
    
    private void OnCreatePressed()
    {
        if (ValidarDatos())
        {
            Personaje nuevoPersonaje = CrearPersonaje();
            GuardarPersonaje(nuevoPersonaje);
            
            // Seleccionar automáticamente y transicionar
            SessionData.Instance.id_personaje = nuevoPersonaje.id;
            GetTree().ChangeSceneToFile("res://scenes/ui/world_select_menu.tscn");
        }
    }
}
```

## 💾 Sistema de Persistencia

### 📁 **Estructura de Archivos**
```
worlds/players/
├── {personaje_id}.dat          # Datos serializados del personaje
├── {personaje_id}_stats.dat    # Estadísticas adicionales
└── {personaje_id}_backup.dat   # Backup automático
```

### 🔧 **Serialización**
```csharp
public class PersonajeManager
{
    public static void GuardarPersonaje(Personaje personaje)
    {
        try
        {
            string ruta = $"worlds/players/{personaje.id}.dat";
            string datosJson = JsonConvert.SerializeObject(personaje);
            File.WriteAllText(ruta, datosJson);
            
            // Crear backup
            string rutaBackup = $"worlds/players/{personaje.id}_backup.dat";
            File.Copy(ruta, rutaBackup, true);
            
            Logger.LogInfo($"PersonajeManager: Personaje {personaje.nombre} guardado");
        }
        catch (Exception ex)
        {
            Logger.LogError($"PersonajeManager: Error guardando personaje: {ex.Message}");
        }
    }
    
    public static Personaje CargarPersonaje(string personajeId)
    {
        try
        {
            string ruta = $"worlds/players/{personajeId}.dat";
            if (File.Exists(ruta))
            {
                string datosJson = File.ReadAllText(ruta);
                return JsonConvert.DeserializeObject<Personaje>(datosJson);
            }
            return null;
        }
        catch (Exception ex)
        {
            Logger.LogError($"PersonajeManager: Error cargando personaje: {ex.Message}");
            return null;
        }
    }
}
```

## 🎯 Características del Sistema

### ✅ **Funcionalidades Principales**
- **Creación de personajes** - Formulario completo con personalización
- **Selección de personajes** - Lista visual con detalles
- **Persistencia automática** - Guardado/carga transparente
- **Validación de datos** - Nombres únicos y datos válidos
- **Preview 3D** - Vista previa en tiempo real
- **Gestión de errores** - Manejo robusto de excepciones

### 🎨 **Personalización**
- **Nombre y apodo** - Campos de texto con validación
- **Género** - Masculino/Femenino/Neutro
- **Colores** - Cabello, piel, ropa personalizables
- **Altura** - Slider para altura (1.5m - 2.2m)
- **Preview** - Vista 3D actualizada en tiempo real

### 📊 **Estadísticas y Progresión**
- **Nivel y experiencia** - Sistema de progresión básico
- **Vida y mana** - Atributos básicos del personaje
- **Habilidades** - Sistema de desbloqueo
- **Equipamiento** - Slots de equipamiento básicos
- **Inventario** - Sistema de inventario inicial

## 🔄 Integración con Wild v2.0

### 🌍 **Conexión con Sistema de Terreno**
- **Spawn inicial** - Personaje aparece en punto seguro
- **Posición persistente** - Guardar última posición
- **Integración física** - Colisiones con CharacterBody3D

### 🎮 **Conexión con Sistema de UI**
- **Navegación fluida** - Menús conectados entre sí
- **Consistencia visual** - Mismo estilo que otros menús
- **Responsive** - Adaptable a diferentes resoluciones

### 💾 **Conexión con Persistencia**
- **SessionData** - Variables globales de personaje
- **Mundos** - Personajes asociados a mundos
- **Backup automático** - Protección contra pérdida de datos

## 📊 Métricas de Éxito

### ✅ **Criterios de Completitud**
- [x] **CharacterSelectMenu** funcional y navegable
- [x] **CharacterCreateMenu** completo con validación
- [x] **Sistema de persistencia** funcionando (incluyendo `selected.dat`)
- [x] **Preview 3D** (Label integrado en NewGameMenu entre separadores)
- [x] **Integración** con SessionData y mundos
- [x] **Validación** de nombres únicos
- [x] **Garantía** de personaje por defecto

### 🎯 **Testing y Validación**
- **Creación** - Crear personaje con todos los campos
- **Selección** - Seleccionar personaje existente
- **Persistencia** - Reiniciar y verificar datos guardados
- **Validación** - Probar nombres duplicados y datos inválidos
- **UI/UX** - Probar flujo completo de usuario

## 🔄 Flujo de Usuario

### 📋 **Flujo Principal**
1. **MainMenu** → CharacterSelectMenu
2. **Ver lista de personajes** → Elegir existente o crear nuevo
3. **Si crear nuevo** → CharacterCreateMenu
4. **Personalizar personaje** → Confirmar creación
5. **Seleccionar personaje** → WorldSelectMenu
6. **Cargar mundo** → Iniciar juego con personaje

### 🔄 **Flujo Alternativo**
1. **Sin personajes** → Creación automática de personaje por defecto
2. **Error en carga** → Recuperación con personaje por defecto
3. **Corrupción de datos** → Backup automático o personaje por defecto

## 🎯 Dependencias

### 🔧 **Sistemas Requeridos**
- **Logger (F2)** - Para logging y depuración
- **SessionData** - Para variables globales
- **Sistema de UI** - Para menús responsivos
- **Sistema de Terreno** - Para spawn y posición

### 📋 **Componentes Externos**
- **Godot CharacterBody3D** - Para física del personaje
- **Godot SubViewport** - Para preview 3D
- **Newtonsoft.Json** - Para serialización (si se usa JSON)

## 📈 Estimación de Tiempo

### ⏱️ **Desglose por Componente**
- **Personaje.cs** - 0.5 días
- **PersonajeManager.cs** - 0.5 días
- **CharacterSelectMenu.cs** - 0.5 días
- **CharacterCreateMenu.cs** - 0.5 días
- **Componentes UI** - 0.5 días
- **Integración y testing** - 0.5 días

### 🎯 **Total Estimado**
- **Duración:** 2 días
- **Complejidad:** Media
- **Prioridad:** Alta
- **Dependencias:** Logger (completado)
