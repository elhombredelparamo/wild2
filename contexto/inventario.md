# 🎒 SISTEMA DE INVENTARIO

## Fecha de Creación
2026-03-16

## Descripción General
Sistema de inventario para Wild v2.0 que permite a los jugadores gestionar objetos, recursos y equipamiento de manera eficiente e intuitiva.

## 📋 Estado Actual
- **Estado:** En desarrollo
- **Feature:** Feature 8 - Sistema de Inventario ⏳ (próxima)
- **Prioridad:** Alta

---

## 🎯 Objetivos del Sistema

### Funcionalidad Principal
- [ ] Gestión de objetos y recursos
- [ ] Sistema de equipamiento
- [ ] Interfaz intuitiva de inventario
- [ ] Persistencia de datos
- [ ] Integración con otros sistemas

### Metas de Rendimiento
- [ ] Operaciones < 100ms
- [ ] Memoria eficiente
- [ ] UI responsiva
- [ ] Sincronización multijugador

---

## 🏗️ Arquitectura del Sistema

### Componentes Principales
- **InventarioManager:** Gestión central del inventario
- **SlotSystem:** Sistema de slots y apilamiento
- **ItemDatabase:** Base de datos de objetos
- **UIInventory:** Interfaz de usuario
- **PersistenceLayer:** Persistencia de datos

### Integración con Otros Sistemas
- **SessionData:** Variables globales
- **Logger:** Sistema de logging
- **Red:** Sincronización multijugador
- **Personajes:** Estadísticas y equipamiento
- **Terreno:** Recolección de recursos
- **Sistema de Mundos:** Asociación inventario-mundo-personaje
- **Sistema de Objetos 3D:** Inventario integrado en objetos del mapa

---

## 🎮 Características del Sistema

### Gestión de Objetos
- [ ] Apilamiento automático
- [ ] Clasificación por categorías
- [ ] Búsqueda y filtrado
- [ ] Arrastrar y soltar
- [ ] Acciones rápidas

### Sistema de Equipamiento
- [ ] Slots de equipamiento
- [ ] Bonificaciones por equipo
- [ ] Durabilidad de objetos
- [ ] Sistema de mejoras

### Interfaz de Usuario
- [ ] Vista de inventario principal
- [ ] Vista rápida de hotbar
- [ ] Tooltips informativos
- [ ] Atajos de teclado
- [ ] Modo ordenación
- [ ] Sistema de overlay con TAB
- [ ] Panel de contenedores inferior
- [ ] Panel de contenido centrado
- [ ] Sistema de cuadrícula con scroll

---

## 📊 Estructura de Datos

### Objeto (Item)
```
Item:
├── id: string
├── nombre: string
├── descripción: string
├── tipo: ItemType
├── categoría: ItemCategory
├── stack_size: int
├── peso: float
├── durabilidad: int
├── utilidad: string
├── dificultad_obtención: int
├── timestamp_mundo: float
├── propiedades: Dictionary
└── texturas: Dictionary
```

### Slot de Inventario
```
InventorySlot:
├── id: int
├── item: Item
├── cantidad: int
├── peso_actual: float
├── bloqueado: bool
├── tipo_restricción: SlotType
├── capacidad_máxima_objetos: int
├── capacidad_máxima_peso: float
└── estado: SlotState
```

### Inventario
```
Inventory:
├── slots: List<InventorySlot>
├── capacidad: int
├── peso_actual: float
├── peso_máximo: float
├── propietario: string
├── mundo: string
├── última_modificación: DateTime
└── objetos_del_mundo_accesibles: List<string>
```

### Inventario de Objeto del Mundo
```
WorldObjectInventory:
├── id_objeto: string
├── slots: List<InventorySlot>
├── contenido_inicial: Dictionary<string, int>
├── estado_acceso: AccessState
├── herramientas_requeridas: List<ToolType>
├── regeneración_habilitada: bool
├── tiempo_regeneración: float
└── persistencia: bool
```

### Estructura de Persistencia
```
personajes/[id_personaje]/inventario.json
├── inventario_principal: Inventory
├── hotbar_slots: List<InventorySlot>
├── equipamiento: Dictionary<string, InventorySlot>
├── estadísticas_inventario: InventoryStats
└── última_actualización: DateTime
```

### Reglas de Asociación
- **Un inventario por jugador-mundo:** Cada personaje tiene un inventario único por mundo
- **Sin compartición:** Inventarios no se comparten entre mundos
- **Aislamiento completo:** Distintos personajes en mismo mundo tienen inventarios separados
- **Persistencia local:** Guardado en carpeta del personaje dentro del mundo

## 🎨 Interfaz de Inventario

### Sistema de Overlay
- **Apertura:** Tecla TAB (inventario propio) / Tecla E (contenedores del mundo)
- **Cierre:** Tecla TAB o ESC
- **Modo overlay:** Se muestra sobre el juego sin pausarlo
- **Transparencia:** Semi-transparente para mantener visibilidad del juego

### Modos de Interfaz
#### **Modo Inventario Propio (TAB)**
- Panel superior: Contenido del contenedor seleccionado
- Barra inferior: Solo contenedores del personaje

#### **Modo Contenedor del Mundo (E)**
- Panel superior: Contenido del contenedor del mundo o del personaje
- Barra inferior: Contenedores del personaje + contenedor del mundo abierto

### Estructura de la Interfaz
```
┌─────────────────────────────────────────────────────────────┐
│                    PANEL DE CONTENIDO                       │
│  ┌─────┐ ┌─────┐ ┌─────┐ ┌─────┐ ┌─────┐ ┌─────┐ ┌─────┐    │
│  │ Obj1│ │ Obj2│ │ Obj3│ │ Obj4│ │ Obj5│ │ Obj6│ │ Obj7│    │
│  └─────┘ └─────┘ └─────┘ └─────┘ └─────┘ └─────┘ └─────┘    │
│  ┌─────┐ ┌─────┐ ┌─────┐ ┌─────┐ ┌─────┐ ┌─────┐ ┌─────┐    │
│  │ Obj8│ │ Obj9│ │Obj10│ │Obj11│ │Obj12│ │Obj13│ │Obj14│    │
│  └─────┘ └─────┘ └─────┘ └─────┘ └─────┘ └─────┘ └─────┘    │
│                    [BARRAS DE SCROLL]                       │
└─────────────────────────────────────────────────────────────┘
│                  BARRA INFERIOR DE                          │
│                CONTENEDORES ACTIVOS                         │
│  ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌──────┐   │
│  │Mano Izq.│ │Mano Der.│ │Mochila  │ │Riñonera │ │Zurron│   │
│  └─────────┘ └─────────┘ └─────────┘ └─────────┘ └──────┘   │
└─────────────────────────────────────────────────────────────┘
```

### Barra Inferior de Contenedores
- **Contenedores activos:** Muestra todos los contenedores disponibles
- **Slots del jugador:** Manos derecha e izquierda siempre visibles
- **Contenedores equipados:** Mochilas, bolsas, cinturones
- **Contenedores cercanos:** Cajas y cofres en proximidad (solo en modo E)
- **Click para abrir:** Al hacer clic en un contenedor, muestra su contenido
- **Transferencia directa:** Arrastrar entre cualquier contenedor visible

### Flujo de Transferencia entre Contenedores
- **Ejemplo práctico:**
  1. **Abrir cofre (E):** Panel superior muestra contenido del cofre
  2. **Barra inferior:** Muestra manos, mochila, riñonera, zurrón + cofre abierto
  3. **Tomar baya:** Arrastrar baya del cofre al zurrón
  4. **Abrir mochila:** Click en mochila en barra inferior
  5. **Panel superior:** Ahora muestra contenido de la mochila
  6. **Transferir:** Arrastrar seta de la mochila al cofre (visible en barra inferior)
- **Intercambio rápido:** Sin necesidad de cerrar y abrir interfaces
- **Visión completa:** Todos los contenedores relevantes siempre accesibles

### Panel de Contenido Centrado
- **Cuadrícula de objetos:** Visualización en grid del contenedor seleccionado
- **Scroll automático:** Barras de desplazamiento si hay muchos objetos
- **Tooltips:** Información al pasar el cursor sobre objetos
- **Arrastrar y soltar:** Entre contenedores y al mundo
- **Doble clic:** Uso rápido de objetos
- **Click derecho:** Menú contextual de acciones disponibles

### Menú Contextual de Objeto
- **Apertura:** Click derecho sobre cualquier objeto
- **Menú desplegable:** Opciones contextuales según tipo de objeto
- **Posición:** Cerca del cursor, sin obstaculizar la vista
- **Opciones dinámicas:** Varían según el tipo de objeto y contexto

#### **Opciones por Tipo de Objeto**
**Consumibles (Comida, Bebida, Medicina):**
- Usar ahora
- Tirar al suelo
- Mover a...

**Herramientas:**
- Equipar en mano
- Tirar al suelo
- Mover a...
- Reparar (si está dañada)

**Armas:**
- Equipar en mano
- Tirar al suelo
- Mover a...
- Reparar (si está dañada)

**Materiales (Madera, Piedra, Metal):**
- Tirar al suelo
- Mover a...
- Dividir stack (si hay más de 1)

**Contenedores (Mochilas, Bolsas):**
- Abrir contenedor
- Tirar al suelo
- Mover a...

#### **Opciones Universales**
- **Tirar al suelo:** Lanza objeto al mundo en posición del jugador
- **Mover a...:** Submenú con todos los contenedores disponibles
- **Información:** Muestra detalles del objeto (peso, durabilidad, etc.)
- **Cancelar:** Cierra el menú sin acción

### Comportamiento de la Interfaz
- **Sin pausa:** El juego continúa mientras el inventario está abierto
- **Respuesta rápida:** Apertura y cierre instantáneos
- **Intuitivo:** Navegación simple con mouse y teclado
- **Contextual:** Solo muestra contenedores relevantes

### Controles de Navegación
- **TAB:** Abrir inventario propio (solo contenedores del personaje)
- **E:** Abrir contenedor del mundo (contenedores del personaje + contenedor del mundo)
- **ESC:** Cerrar inventario
- **Click izquierdo:** Seleccionar/mover objetos
- **Click derecho:** Menú contextual de acciones disponibles
- **Doble clic:** Uso rápido de objetos
- **Arrastrar:** Mover objetos entre cualquier contenedor visible
- **Rueda del mouse:** Scroll en panel de contenido

---

## 🎨 Tipos de Objetos

### Categorías Principales
- **Materiales:** Madera, piedra, metal
- **Herramientas:** Hachas, picos, martillos
- **Armas:** Espadas, arcos, lanzas
- **Armaduras:** Cascos, pechos, botas
- **Consumibles:** Comida, pociones, antorchas
- **Recursos:** Minerales, plantas, animales

### Características de Objetos
- **Calidad del material:** Determina durabilidad y efectividad
- **Condición:** Estado actual del objeto (nuevo, usado, dañado)
- **Origen:** Natural, fabricado, encontrado
- **Propiedades especiales:** Habilidades únicas según material o construcción

---

## 🔧 Mecánicas del Sistema

### Sistema de Capacidades por Slot
- **Límite por cantidad:** Máximo número de objetos por slot
- **Límite por peso:** Máximo peso total por slot
- **Regla restrictiva:** Se alcanza el límite que primero se cumpla
- **Ejemplo práctico:** 
  - Cesta: 1000 objetos o 5kg (lo que primero se alcance)
  - Bolsa: 50 objetos o 10kg
  - Mochila: 20 objetos o 25kg

### Inventario Inicial del Jugador
- **Estado inicial:** Los jugadores empiezan desnudos
- **Slots disponibles:** Solo 2 slots al comenzar
- **Slot mano derecha:** 1 objeto / 10kg máximo
- **Slot mano izquierda:** 1 objeto / 10kg máximo
- **Sin inventario principal:** Debe ser construido o encontrado
- **Progresión:** Ampliación mediante mochilas, bolsas, cajas

### Tipos de Slots
- **Slots de manos:** Derecha e izquierda, 1 objeto / 10kg cada una
- **Slots básicos:** Contenedores simples del inventario (requieren mochila/bolsa)
- **Slots de equipamiento:** Para armas, armaduras y herramientas
- **Slots especiales:** Objetos únicos o de propósito específico
- **Contenedores externos:** Cajas, cofres, barriles, etc.
- **Slots de objetos del mundo:** Inventario integrado en objetos 3D del mapa

### Expansión de Inventario
- **Mochilas:** Añaden slots adicionales al inventario principal
- **Bolsas:** Contenedores portátiles con capacidades específicas
- **Cajas:** Almacenamiento estático en el mundo
- **Cinturones:** Slots rápidos para herramientas
- **Equipamiento especial:** Ropa con bolsas integradas

### Inventario de Objetos del Mundo
- **Objetos con inventario propio:** Árboles, rocas, arbustos, etc.
- **Contenido por defecto:** Generado al crear el objeto
- **Acceso condicional:** Requiere herramientas o acciones específicas
- **Ejemplos:**
  - Árbol: Hojas (X), ramas (Y), frutos (Z) - madera solo al talar
  - Roca: Piedras pequeñas - mineral solo con pico
  - Arbusto: Bayas, hojas - raíz solo con pala
- **Persistencia:** Se guarda con el estado del objeto
- **Regeneración:** Algunos recursos pueden regenerarse con el tiempo

### Lógica de Apilamiento
- **Verificación por cantidad:** `cantidad_actual + cantidad_nueva <= capacidad_máxima_objetos`
- **Verificación por peso:** `peso_actual + (peso_unitario * cantidad_nueva) <= capacidad_máxima_peso`
- **Rechazo automático:** Si se excede cualquiera de los límites
- **Redistribución inteligente:** Buscar slots con capacidad disponible

### Pesos y Límites
- **Peso por objeto:** Según material y tamaño
- **Límite de peso:** Basado en estadísticas del personaje
- **Penalizaciones:** Movimiento lento con exceso de peso
- **Bonificaciones:** Mejoras de capacidad

### Sistema de Degradación por Tiempo
- **Timestamp de mundo:** Tiempo en que se obtuvo el objeto
- **Procesos temporales:** Putrefacción, secado, curado, fermentación
- **Estados de maduración:** Crudo, en proceso, listo, podrido
- **Factores ambientales:** Temperatura, humedad, almacenamiento
- **Ejemplos:**
  - Carne: Fresca → En descomposición → Podrida
  - Hierbas: Frescas → Secándose → Secas
  - Cuero: Crudo → Curándose → Curado
  - Fruta Verde: Inmadura → Madura → Pasada

### Durabilidad
- **Desgaste por uso:** Herramientas y armas
- **Reparación:** Con materiales específicos
- **Rotura:** Pérdida total del objeto
- **Mantenimiento:** Sistema de cuidado

---

## 🔄 Flujo de Trabajo

### Operaciones Básicas
1. **Recoger objeto:** Verificar capacidad de slots disponibles y añadir con timestamp actual
2. **Mover objeto:** Transferir entre slots respetando límites
3. **Usar objeto:** Consumir o equipar
4. **Tirar objeto:** Eliminar del inventario y lanzarlo al mundo
5. **Trueque:** Intercambio directo con otros jugadores
6. **Redistribución:** Reorganizar objetos automáticamente al recoger
7. **Extraer de objeto del mundo:** Acceder a inventario de objetos del mapa
8. **Procesar objeto:** Transformar recursos con herramientas
9. **Verificar estado temporal:** Comprobar putrefacción, maduración, etc.

### Sistema de Comercio
- **Trueque directo:** Intercambio de objetos entre jugadores
- **Sin moneda:** No existe sistema monetario
- **Valor subjetivo:** Cada jugador decide qué vale cada objeto
- **Negociación:** Acuerdo mutuo entre las partes
- **Proporciones:** Intercambios basados en utilidad y escasez

### Persistencia
1. **Guardado automático:** Cada cambio significativo
2. **Validación:** Verificación de integridad
3. **Carga:** Recuperación al iniciar sesión
4. **Sincronización:** Actualización multijugador
5. **Aislamiento por mundo:** Cada mundo mantiene inventarios separados por personaje
6. **Localización:** Guardado en `personajes/[id_personaje]/inventario.json` dentro del mundo

---

## 🎯 Consideraciones de Diseño

### Experiencia del Usuario
- **Intuitivo:** Fácil de aprender y usar
- **Rápido:** Operaciones fluidas
- **Informativo:** Información clara y útil
- **Consistente:** Comportamiento predecible

### Rendimiento
- **Eficiente:** Uso óptimo de memoria
- **Rápido:** Respuesta inmediata
- **Escalable:** Soporte para muchos objetos
- **Optimizado:** Sin impacto en FPS

### Multijugador
- **Sincronizado:** Estado consistente
- **Seguro:** Prevención de cheats
- **Confiable:** Sin pérdida de datos
- **Eficiente:** Mínimo ancho de banda

---

## 📝 Notas de Desarrollo

### Decisiones de Implementación
- **Base de datos:** JSON para objetos definidos
- **Cache:** Objetos frecuentes en memoria
- **Eventos:** Sistema de notificaciones
- **Validación:** Reglas estrictas de integridad
- **Persistencia por mundo:** Cada mundo maneja sus propios archivos de inventario
- **Identificación única:** Combinación personaje-mundo para identificar inventarios

### Integración Pendiente
- [ ] Con sistema de crafting
- [ ] Con sistema de comercio
- [ ] Con sistema de misiones
- [ ] Con sistema de construcción

---

## 🚀 Próximos Pasos

### Desarrollo Inmediato
1. [ ] Definir estructura de datos completa
2. [ ] Implementar sistema de slots
3. [ ] Crear base de datos de objetos
4. [ ] Desarrollar interfaz básica
5. [ ] Integrar con SessionData

### Testing y Validación
1. [ ] Pruebas unitarias de componentes
2. [ ] Testing de rendimiento
3. [ ] Validación multijugador
4. [ ] Pruebas de usabilidad
5. [ ] Integración completa

---

**Estado:** En espera de especificaciones detalladas del usuario
**Próxima actualización:** Según feedback del usuario
