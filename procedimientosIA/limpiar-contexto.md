---
description: Limpiar y reestructurar archivos de contexto del proyecto
---

# Procedimiento: Limpiar Contexto

## Propósito

Limpiar y reestructurar los archivos de contexto del proyecto Wild para eliminar información irrelevante, desactualizada o especulativa, manteniendo solo la información relevante y actualizada sobre sistemas implementados.

## Prerrequisitos

- Acceso de lectura a todos los archivos de contexto
- Conocimiento de los criterios de relevancia del procedimiento sincronizar-contexto.md
- Permisos para modificar archivos de contexto
- Sistema de archivos estable (no cambios durante la ejecución)

## Algoritmo de Ejecución

### 1. Selección Interactiva de Archivo
```
// Mostrar lista de archivos de contexto disponibles
lista_contexto = OBTENER_ARCHIVOS_CONTEXTO()

INFORMAR usuario: "📁 Archivos de contexto disponibles:"
PARA CADA archivo EN lista_contexto:
    MOSTRAR "- [archivo]"
FIN PARA CADA

PREGUNTAR usuario: "¿Qué archivo de contexto desea limpiar? (nombre del archivo)"

archivo_seleccionado = OBTENER_RESPUESTA_USUARIO()

SI archivo_seleccionado NO ESTÁ EN lista_contexto:
    INFORMAR usuario: "❌ Archivo no encontrado. Archivos disponibles:"
    MOSTRAR lista_contexto
    PREGUNTAR usuario: "¿Desea intentar con otro archivo? (S/N)"
    SI respuesta es "N":
        INFORMAR usuario: "🛑 Procedimiento cancelado por el usuario"
        TERMINAR procedimiento
    SI NO:
        VOLVER al inicio del paso 1
    FIN SI
FIN SI

INFORMAR usuario: "🔄 Analizando [archivo_seleccionado]..."
```

### 2. Análisis del Archivo Seleccionado
```
LEER archivo_seleccionado completo

// Analizar contenido según criterios de relevancia
contenido_relevante = ANALIZAR_RELEVANCIA(archivo_seleccionado)
contenido_irrelevante = IDENTIFICAR_IRRELEVANCIA(archivo_seleccionado)

MOSTRAR resumen del análisis:
- Líneas totales: X
- Contenido relevante estimado: Y%
- Contenido irrelevante estimado: Z%
- Principales áreas de mejora: [lista de áreas]

SI contenido_irrelevante > contenido_relevante:
    INFORMAR usuario: "⚠️ El archivo tiene más contenido irrelevante que relevante"
    MOSTRAR ejemplos de contenido irrelevante detectado
FIN SI

PREGUNTAR usuario: "¿Desea continuar con la limpieza de [archivo_seleccionado]? (S/N)"
SI respuesta es "N":
    INFORMAR usuario: "🛑 Procedimiento cancelado por el usuario"
    TERMINAR procedimiento
FIN SI

INFORMAR usuario: "🔄 Iniciando limpieza de [archivo_seleccionado]..."
```

### 3. Limpieza y Reestructuración del Archivo
```
// Aplicar reglas de limpieza específicas para el tipo de archivo
contenido_limpio = APLICAR_REGLAS_LIMPIEZA(contenido_relevante, archivo_seleccionado)

CREAR archivo_limpio con contenido_limpio

// Reemplazar archivo original
BORRAR archivo_seleccionado
RENOMBRAR archivo_limpio a archivo_seleccionado

INFORMAR usuario: "✅ [archivo_seleccionado] limpiado y reestructurado"
MOSTRAR estadísticas:
- Líneas originales: X
- Líneas finales: Y
- Reducción: Z%
- Cambios principales: [resumen de cambios]
```

### 4. Criterios de Relevancia por Archivo

#### contexto/estado-actual.txt
**RELEVANTE:**
- Estado actual de sistemas implementados
- Cambios en arquitectura modular
- Nuevos sistemas implementados
- Bugs resueltos y mejoras importantes
- Información técnica básica del proyecto

**NO RELEVANTE:**
- Detalles de implementación de métodos específicos
- Estadísticas de desarrollo (líneas de código, tiempo)
- Logs históricos de versiones antiguas
- Detalles técnicos de backups

#### contexto/estructura-inicial.txt
**RELEVANTE:**
- Estructura básica de carpetas y archivos
- Rutas importantes del proyecto
- Configuración general
- Flujo actual simplificado

**NO RELEVANTE:**
- Detalles de implementación específicos
- Versiones desactualizadas de Godot y .NET
- Flujo antiguo que ya no aplica
- Próximos pasos obsoletos

#### contexto/menus-y-servidor.txt
**RELEVANTE:**
- Descripción de menús implementados
- Flujo de usuario entre menús
- Arquitectura cliente-servidor implementada
- Flujo de ejecución actual
- Ventajas del enfoque servidor local + cliente

**NO RELEVANTE:**
- Detalles de implementación específicos
- Información técnica muy detallada de arquitectura modular
- Detalles específicos de GameFlowExtensions

#### contexto/mundos-y-partidas.txt
**RELEVANTE:**
- Concepto fundamental de mundos independientes
- Estructura de directorios
- Metadatos del mundo
- Flujo de usuario básico
- Sistema de gestión implementado
- Estado actual de implementación

**NO RELEVANTE:**
- Detalles de implementación de WorldManager
- Código específico de métodos y clases
- Detalles de integración con GameFlow
- Ejemplos de código específicos

#### contexto/mecanicas.txt
**RELEVANTE:**
- Controles implementados (WASD, mouse, ESC)
- Física implementada (CharacterBody3D, CollisionShape3D)
- Sistema de cámara implementado
- Movimiento WASD y rotación de cámara
- Sistema de colisiones básico

**NO RELEVANTE:**
- Especificaciones de sistemas no implementados
- Detalles de inventario, crafteo, construcción
- Especificaciones de salud, estamina, nutrición
- Detalles de hidratación y temperatura

#### contexto/generacion-terreno.txt
**RELEVANTE:**
- Sistema de chunks modular implementado
- Configuración técnica de chunks
- Algoritmo base (ruido Perlin)
- Rango de alturas implementado
- Sistema de coordenadas y escala
- Sistema de almacenamiento de chunks
- Sistema de barreras físicas

**NO RELEVANTE:**
- Especificaciones detalladas de biomas
- Catálogos completos de modelos 3D por bioma
- Detalles de implementación de objetos específicos
- Especificaciones técnicas de renderizado avanzado
- Sistemas de recolección y persistencia de objetos

#### contexto/como-usar-changelog.md
**RELEVANTE:**
- Proceso de uso del sistema de changelog
- Pasos a seguir para añadir cambios
- Reglas importantes
- Comandos útiles
- Ejemplos de uso

**NO RELEVANTE:**
- Detalles técnicos de implementación específica

#### contexto/sistema-changelog.md
**RELEVANTE:**
- Descripción del problema resuelto
- Estructura de archivos implementada
- Flujo de trabajo actual
- Características del sistema
- Reglas importantes
- Estado actual de implementación

**NO RELEVANTE:**
- Detalles muy específicos del script de compilación
- Ejemplos de código específicos
- Detalles técnicos de implementación interna

### 5. Continuación del Procedimiento
```
// Después de limpiar un archivo, preguntar si desea continuar
PREGUNTAR usuario: "¿Desea limpiar otro archivo de contexto? (S/N)"

SI respuesta es "S":
    VOLVER al inicio del paso 1
SI NO:
    INFORMAR usuario: "🛑 Procedimiento limpiar-contexto finalizado"
    MOSTRAR resumen general:
    - Archivos procesados: X
    - Líneas totales eliminadas: Y
    - Reducción promedio: Z%
    TERMINAR procedimiento
FIN SI
```

### 6. Reglas de Limpieza

#### Preservación de Formato
- Mantener estructura y estilo del archivo de contexto
- Añadir fecha de última actualización al final
- Ser conservador con el contenido existente

#### Eliminación de Contenido
- Eliminar detalles de implementación específicos
- Eliminar contenido especulativo no implementado
- Eliminar información histórica desactualizada
- Eliminar estadísticas de desarrollo innecesarias

### 7. Enfoque en Relevancia
- Mantener solo información sobre sistemas implementados
- Eliminar planes futuros no implementados
- Eliminar detalles técnicos que pertenecen a otros archivos

#### Consistencia
- Usar terminología consistente con otros archivos de contexto
- Mantener formato markdown claro y profesional
- Añadir timestamp de actualización

### 8. Informe Final

Por cada archivo procesado:
```
[HH:MM:SS] ARCHIVO: nombre_archivo_contexto
ESTADO: LIMPIADO Y REESTRUCTURADO
LÍNEAS_ORIGINALES: X
LÍNEAS_FINALES: Y
REDUCCIÓN: Z%
CAMBIOS_PRINCIPALES: Resumen de cambios principales
```

## Resultados Esperados

- **Contexto limpio y relevante**: Todos los archivos de contexto contienen solo información actualizada
- **Estructura consistente**: Formato unificado y profesional
- **Eliminación de ruido**: Sin detalles técnicos innecesarios ni contenido especulativo
- **Documentación clara**: Fácil de entender y mantener

## Comandos de Ejecución

Para iniciar este procedimiento:
```
"lee procedimientosIA/limpiar-contexto.md"
"ejecutar procedimiento limpiar-contexto"
```

## Notas Importantes

- Este procedimiento debe ejecutarse cuando los archivos de contexto se ensucien con información irrelevante
- Siempre hacer backup antes de realizar cambios masivos
- Verificar el resultado después de cada archivo procesado
- Mantener coherencia con el procedimiento sincronizar-contexto.md
