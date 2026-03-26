# 🛠️ Wild v2.0: Documentación Técnica de Sistemas

Este documento sirve como referencia técnica profunda para desarrolladores e IAs sobre el funcionamiento interno de los sistemas de Wild v2.0.

---

## 🏗️ 1. Núcleo del Sistema (Core)

El núcleo de Wild v2.0 está diseñado para proporcionar una base estable y coherente para todos los sistemas de alto nivel. Se rige por el principio de **Single Source of Truth** y comunicación basada en estados y eventos.

### **SessionData** (Gestión de Estado Global)
- **Filosofía**: Actúa como el puente entre la configuración del usuario y el comportamiento del motor. Es el almacén central de parámetros que no pertenecen a un "mundo" específico, sino a la instancia de la aplicación (gráficos, audio, preferencias).
- **Interconexiones**: Emite señales (`OnSettingsApplied`) cuando hay cambios, permitiendo que sistemas como el de Calidad Dinámica o el motor de Audio se reajusten sin dependencia directa.
- **Flujo de Datos**: Carga inicial desde `config.json` -> Modificación en memoria -> Persistencia asíncrona -> Notificación a observadores.

### **Logger** (Trazabilidad Unificada)
- **Filosofía**: Sustituye el logging estándar de Godot por un sistema persistente diseñado para depuración en entornos de producción o ejecución a pantalla completa. Garantiza que cada ejecución deje un rastro legible en `latest.log`.
- **Interconexiones**: Es un sistema transversal (Cross-cutting concern) invocado por todos los demás módulos para reportar estados críticos, advertencias o flujos informativos.
- **Regla Arquitectónica**: Prohibido el uso de `GD.Print()`. El `Logger` debe ser el único medio de salida de texto técnico.

### **MundoManager** (Orquestación de la Sesión)
- **Filosofía**: Gestiona el ciclo de vida de los datos de juego (Mundos). Su responsabilidad principal es garantizar la integridad de la estructura de archivos en el disco y coordinar qué mundo está "activo" en la sesión actual.
- **Interconexiones**: 
  - Colabora con `PersistenceService` para operaciones de bajo nivel.
  - Sirve como proveedor de rutas para el sistema de Terreno (`chunks/`) y Objetos (`objects/`).
  - Vincula la identidad del personaje activo con el contenedor del escenario.
- **Flujo de Datos**: Escaneo de metadatos de mundos disponibles -> Selección de mundo -> Garantía de estructura de carpetas (Seed, logs, etc.) -> Inyección de rutas a los cargadores de terreno.

### **Componentes de Código Clave (Core)**
| Clase | Método Principal | Descripción |
| :--- | :--- | :--- |
| `SessionData` | `SaveConfig() / LoadConfig()` | Persistencia de ajustes globales en `config.json`. |
| `Logger` | `LogInfo / LogWarning / LogError` | Métodos estáticos para trazabilidad unificada. |
| `MundoManager` | `CrearMundo(nombre, semilla)` | Inicializa la estructura física de un nuevo mundo. |
| `MundoManager` | `GuardarDatosJugador(data)` | Serializa la posición y stats del jugador en el mundo activo. |
| `PersistenceService` | `SaveJson<T> / LoadJson<T>` | Utilidad genérica para E/S de archivos JSON con gestión de errores. |

## 🌍 2. Sistema de Terreno Procedural

Wild v2.0 emplea un sistema de terreno dinámico capaz de generar mundos infinitos con variaciones climáticas y geográficas coherentes, optimizado para el rendimiento multihilo.

### **TerrainManager** (Orquestación del Mundo)
- **Filosofía**: Gestiona la presencia física del terreno en la escena mediante un sistema de "ventana deslizante" de chunks. Su prioridad es mantener 60 FPS delegando el trabajo pesado a hilos secundarios.
- **Interconexiones**: 
    - **MundoManager**: Proporciona la semilla (`seed`) única para la generación determinista.
    - **SessionData**: Sincroniza el radio de carga visual con las preferencias del usuario.
    - **QualityManager**: Inyecta parámetros de resolución de malla para equilibrar detalle vs. rendimiento.
- **Regla Arquitectónica (Muros de Seguridad)**: Genera colisionadores temporales de gran escala en chunks en proceso de carga para prevenir el "falling through world" durante el spawn o movimiento rápido.

### **TerrainGenerator** (Lógica de Malla y Datos)
- **Filosofía**: Transforma datos matemáticos de ruido en geometría 3D. Utiliza una técnica de teseleado dinámico donde la densidad de la malla varía según la calidad seleccionada.
- **Flujo de Datos (Splat Map)**: Inyecta pesos de bioma directamente en el color de los vértices de la malla. Esto permite que el shader de terreno realice mezclas suaves (blending) de hasta 4 texturas diferentes sin necesidad de múltiples pasadas de renderizado.
- **Interconexiones**: Consulta al `BiomaManager` en cada vértice para determinar la "personalidad" geológica del terreno (frecuencia de ruido, altura base).

### **BiomaManager** (Distribución de Ecosistemas)
- **Filosofía**: Un motor de reglas que decide qué ecosistema corresponde a cada coordenada basándose en dos ejes: Altitud (Elevación) y Humedad (Variación horizontal).
- **Regla Arquitectónica**: Las transiciones son deterministas pero fluidas, utilizando funciones de ruido fractal para evitar bordes artificiales entre, por ejemplo, una pradera y un bosque.

### **Componentes de Código Clave (Terreno)**
| Clase | Método Principal | Descripción |
| :--- | :--- | :--- |
| `TerrainManager` | `Update(refPos)` | Dispara la carga/descarga de chunks según la posición del jugador. |
| `TerrainManager` | `RemoveVegetationAt(pos, id)` | Elimina una instancia de vegetación y registra su borrado persistente. |
| `TerrainGenerator` | `GenerateChunkData(coord)` | Calcula el mapa de altitudes y biomas para una coordenada dada. |
| `BiomaManager` | `GetBiomeAt(x, z)` | Retorna el tipo de ecosistema basado en ruido de temperatura y humedad. |
| `VegetationSpawner`| `SpawnForChunk(renderer, data)`| Distribuye modelos 3D sobre la malla según las reglas del bioma. |

## 🎮 3. Sistema de Jugador y Controladores

El sistema de jugador en Wild v2.0 prioriza la inmersión y la fiabilidad del movimiento en un mundo proceduralmente accidentado.

### **JugadorController** (Física e Inmersión)
- **Filosofía**: Utiliza un modelo "True First Person" donde la cámara reside dentro del modelo 3D del personaje. Esto permite que las sombras, animaciones y extremidades sean visibles de forma natural.
- **Interconexiones**: 
    - **TerrainManager**: Consume datos de elevación en tiempo real para validar la posición del jugador y evitar que atraviese el terreno (Tunneling).
    - **AnimationTree**: Gestiona un `StateMachine` complejo para transiciones fluidas entre reposo, caminata, carrera y salto.
- **Regla Arquitectónica (Rescate / Unstuck)**: Implementa un sistema de auditoría de posición. Si detecta que el jugador está por debajo del nivel del suelo o bloqueado lateralmente a pesar de tener intención de movimiento, aplica un impulso de eyección hacia una coordenada segura calculada mediante el ruido base del terreno.

### **StatsJugador** (Atributos Vitales)
- **Filosofía**: Gestiona el metabolismo del jugador (Salud, Hambre, Sed, Energía). Es un sistema pasivo que degrada valores con el tiempo y aplica efectos negativos (daño) cuando los niveles críticos llegan a cero.
- **Flujo de Datos**: Tiempo transcurrido -> Cálculo de tasas de desgaste -> Actualización de variables persistentes -> Invocación de `OnMuerte` si la salud es nula.

### **InteraccionJugador** (Raycasting y Acción)
- **Filosofía**: Actúa como la "mano" del jugador en el mundo. Utiliza Raycasts desde el centro de la cámara para identificar objetos interactuables en la capa de colisión 4.
- **Interconexiones**: 
    - **InventoryManager**: Para depositar ítems recolectados del suelo.
    - **TerrainManager**: Para solicitar la eliminación física de objetos del escenario (como setas o piedras) una vez recolectados.
    - **Deployables**: Para activar menús o lógica específica de objetos construidos por el jugador (ej. abrir un cofre).

### **Componentes de Código Clave (Jugador)**
| Clase | Método Principal | Descripción |
| :--- | :--- | :--- |
| `JugadorController`| `_PhysicsProcess(delta)`| Orquesta el movimiento físico, anti-tunneling y el sistema de rescate. |
| `JugadorController`| `SetVisualModel(model)` | Configura el modelo 3D, escala, culling y vincula el `AnimationTree`. |
| `StatsJugador` | `Alimentar / Hidratar / Curar`| Modifica los atributos vitales del metabolismo del personaje. |
| `InteraccionJugador`| `IntersectRay(query)` | Lanza un rayo desde la cámara para detectar triggers y deployables. |
 
## 📡 4. Sistema de Red (Networking)

*Nota: Este sistema se encuentra actualmente en fase de **Diseño Técnico** (Feature 13 en el backlog) y no está implementado en la rama principal de código.*

### **Arquitectura Planeada (Cliente-Servidor)**
- **Filosofía**: Wild v2.0 está diseñado para ser multijugador desde su base arquitectónica. El servidor será la autoridad única sobre el estado del mundo y la física, mientras que los clientes serán representaciones ligeras y predictivas.
- **Protocolo de Red**: Basado en mensajes JSON estructurados sobre TCP/IP (Puerto 7777). Incluye tipos de mensajes específicos para movimiento de jugadores, datos de chunks y actualizaciones de biomas.
- **Interconexiones (Diseño)**:
    - **Servidor**: Orquestará el `MundoManager` centralizado y validará las acciones de todos los clientes.
    - **Cliente**: Implementará sistemas de predicción y compensación de latencia para mantener una experiencia de juego fluida.

### **Componentes de Código Clave (Red - Diseño)**
| Clase | Método Principal | Descripción |
| :--- | :--- | :--- |
| `GameServer` | `BroadcastStateUpdate()` | Envía periódicamente la posición de jugadores y cambios en chunks a los clientes. |
| `GameClient` | `PredictMovement()` | Calcula la posición local inmediata antes de recibir la confirmación del servidor. |
| `NetworkManager` | `ProcessMessage(json)` | Deserializa y despacha las acciones de red al sistema correspondiente. |

## 📦 5. Sistema de Inventario y Objetos

El ecosistema de ítems de Wild v2.0 es altamente modular y basado en datos, permitiendo una expansión sencilla de contenido sin modificar la lógica central.

### **InventoryManager** (Gestión de Contenedores)
- **Filosofía (Data-Driven)**: Las propiedades de los objetos (nombre, peso, icono, tipo) no están codificadas, sino que se cargan desde recursos `.tres`. El sistema gestiona múltiples contenedores dinámicos que representan las manos, la mochila o el equipamiento.
- **Interconexiones**: 
    - **InteraccionJugador**: Actúa como el puente de entrada de ítems desde el mundo físico al sistema lógico de contenedores.
    - **PersistenceService**: Serializa el estado de cada slot (`ID_Item`, `Cantidad`) para garantizar que el progreso se guarde con el personaje.
- **Regla Arquitectónica**: Las "Manos" se tratan como contenedores de un solo slot con priorización de entrada, simplificando la lógica de recolección inmediata.

### **Deployables** (Objetos Construibles e Interactivos)
- **Filosofía**: Proporciona un marco de trabajo para objetos que el jugador coloca en el mundo y que mantienen un estado propio (ej. el contenido de un cofre). Utiliza una clase base abstracta (`DeployableBase`) para estandarizar la persistencia.
- **Interconexiones**: 
    - **TerrainManager**: Es el encargado de instanciar estos objetos cuando el chunk correspondiente entra en el radio de carga y de persistir sus datos específicos en el JSON del chunk.
    - **InteraccionJugador**: Activa la lógica específica de cada objeto (abrir UI, procesar materiales) mediante Raycasting.

### **Componentes de Código Clave (Inventario)**
| Clase | Método Principal | Descripción |
| :--- | :--- | :--- |
| `InventoryManager`| `GiveItem(id, qty)` | Intenta añadir un ítem a los contenedores activos (prioridad manos). |
| `InventoryContainer`| `AddItem(item, qty)`| Gestiona la lógica de slots, apilamiento y límites de peso. |
| `DeployableBase` | `Interact()` | Método virtual para definir el comportamiento al pulsar 'E' sobre el objeto. |
| `WorldObjectRegistrar`| `RegistrarObjeto(id, json)`| Puente para persistir estados de objetos dinámicos en el disco. |

## 💾 6. Sistema de Persistencia y Datos

El sistema de persistencia asegura que cada acción del jugador y cada cambio en el mundo se traduzca en una estructura de archivos sólida y recuperable.

### **PersistenceService** (Capa de Abstracción de Archivos)
- **Filosofía**: Wrapper sobre Godot y .NET para serialización JSON segura, con creación automática de directorios y manejo de excepciones centralizado.
- **Regla Arquitectónica**: Prohibido usar `FileAccess` directamente; todas las operaciones de disco deben pasar por este servicio.

### **MundoManager** (Orquestador de Ciclo de Vida)
- **Filosofía**: Gestiona la jerarquía de carpetas física en `user://`. Cada mundo es un ecosistema aislado con subcarpetas para chunks, objetos y personajes.
- **Interconexiones**: Sincroniza el ID del mundo seleccionado con la sesión activa y proporciona rutas para que el `TerrainManager` guarde datos localmente.

### **Componentes de Código Clave (Persistencia)**
| Clase | Método Principal | Descripción |
| :--- | :--- | :--- |
| `PersistenceService`| `SaveJson<T> / LoadJson<T>`| Centraliza la E/S de archivos con validación y manejo de errores. |
| `MundoManager` | `ObtenerRutaChunksActual()`| Resuelve la ruta absoluta de almacenamiento según el mundo activo. |
| `ChunkStateData` | `ToBinary() / FromBinary()`| Optimiza el almacenamiento de mallas y altitudes en archivos `.dat`. |

## 🎨 7. Sistema de Calidad y Rendimiento

Wild v2.0 implementa una capa de escalabilidad dinámica que permite al juego adaptarse a una amplia variedad de hardware.

### **QualityManager** (Gestor de GPU y Shaders)
- **Filosofía**: Centraliza parámetros como resolución de sombras, MSAA y render scale. Utiliza perfiles (Toaster a Ultra) para aplicar ajustes coherentes.
- **Interconexiones**: Emite señales (`OnVegetationQualityChanged`) para que el `TerrainManager` regenere elementos visuales dinámicamente.
- **HardwareCapabilities**: Realiza una auditoría del sistema (RAM, CPU, GPU) para proponer un perfil de rendimiento óptimo.

### **Componentes de Código Clave (Calidad)**
| Clase | Método Principal | Descripción |
| :--- | :--- | :--- |
| `QualityManager` | `ApplyCurrentSettings()`| Ejecuta los cambios en el motor (VSync, FPS, Render Scale, MSAA). |
| `QualityManager` | `ApplyToEnvironment(env)`| Configura post-procesado (Glow, SSAO, SSR, SDFGI) dinámicamente. |
| `QualitySettings` | `ApplyPresetProfile(type)`| Sobrescribe todos los parámetros individuales con un perfil predefinido. |
| `HardwareCapabilities`| `Detect()` | Interroga al `OS` y `DisplayServer` para identificar el hardware del usuario. |

## 🖥️ 8. Interfaces y UI

El sistema de interfaz de Wild v2.0 está diseñado para ser desacoplado y reactivo, separando la navegación de alto nivel de la interacción directa en el mundo.

### **Arquitectura de Menús**
- **Filosofía**: Utiliza escenas de Godot independientes para cada estado del juego (Menú Principal, Selección de Personaje, Juego). La transición entre ellas está orquestada por el `GameLoader`, que gestiona la carga asíncrona de recursos.
- **Interconexiones**: 
    - **Manager Injections**: Los menús de selección (Mundo/Personaje) inyectan las identidades activas en los Singletons globales antes de iniciar la escena `GameWorld`.
    - **QualityManager**: El menú de opciones actúa como un cliente directo de las APIs de calidad para aplicar cambios en tiempo real.

### **HUD e Interfaces de Juego**
- **Filosofía**: El `GameWorld` actúa como el controlador central de las capas de UI superpuestas. Gestiona las prioridades de entrada (input layering) para que, por ejemplo, el menú de pausa tenga prioridad sobre el inventario.
- **Regla Arquitectónica**: El estado del ratón (`Captured` vs `Visible`) está determinado estrictamente por la presencia de interfaces bloqueantes en la pila del `GameWorld`.

### **Componentes de Código Clave (Interfaces)**
| Clase | Método Principal | Descripción |
| :--- | :--- | :--- |
| `GameLoader` | `LoadSceneAsync(path)` | Gestiona la carga en segundo plano de mundos y menús pesados. |
| `GameWorld` | `_Input(@event)` | Captura atajos globales (Escape para Pausa, I para Inventario). |
| `MainMenu` | `StartGame()` | Inyecta la configuración seleccionada e inicia la transición a la escena de juego. |

---
*Nota: Este documento es la fuente de verdad técnica para Wild v2.0 y debe actualizarse ante cualquier cambio estructural en los sistemas descritos.*
