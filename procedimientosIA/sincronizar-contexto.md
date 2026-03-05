---
description: Sincronizar contexto del proyecto con el código fuente
---

# Procedimiento: Sincronizar Contexto

## Propósito

Mantener los archivos de contexto sincronizados con el estado actual del código fuente del proyecto.

## Algoritmo de Ejecución

### 0. PASO PRELIMINAR OBLIGATORIO: Refrescar Procedimiento

```
// Al inicio de cada ejecución, leer COMPLETAMENTE este archivo para refrescar pasos
INFORMAR usuario: "🔄 Refrescando procedimiento desde archivo..."

procedimiento_completo = LEER "procedimientosIA/sincronizar-contexto.md" COMPLETO usando read_file()

SI procedimiento_completo está VACÍO o NO SE PUEDE LEER:
    INFORMAR usuario: "❌ Error crítico: No se puede leer el procedimiento"
    TERMINAR ejecución
FIN SI

INFORMAR usuario: "✅ Procedimiento refrescado. Iniciando sincronización..."
```

### 1. Detección de listacomprobaciones.txt

```
VERIFICAR si existe archivo "listacomprobaciones.txt" en raíz del proyecto

SI EXISTE:
    INFORMAR usuario: "✅ listacomprobaciones.txt encontrado"
    
    // Leer primera línea del archivo
    primera_linea = LEER primera línea de listacomprobaciones.txt
    
    SI primera_linea está VACÍA:
        INFORMAR usuario: "ℹ️ listacomprobaciones.txt está vacío. Eliminando archivo..."
        BORRAR archivo listacomprobaciones.txt
        INFORMAR usuario: "✅ listacomprobaciones.txt eliminado. Proceso finalizado."
        TERMINAR procedimiento
    FIN SI
    
    // Extraer nombres de archivo de la línea usando el separador "***"
    partes = DIVIDIR primera_linea por "***"
    
    SI LONGITUD(partes) == 2:
        archivo_programacion = partes[0]
        archivo_contexto = partes[1]
        INFORMAR usuario: "🔄 Procesando: " + archivo_programacion + " con " + archivo_contexto
        
        // REGLA OBLIGATORIA: Leer completamente ambos archivos
        INFORMAR usuario: "📖 Leyendo archivos completos..."
        contenido_programacion = LEER archivo_programacion COMPLETO usando read_file() SIN LÍMITES
        contenido_contexto = LEER archivo_contexto COMPLETO usando read_file() SIN LÍMITES
        
        // ADVERTENCIA CRÍTICA: NO usar parámetro 'limit' en read_file()
        // AMBOS archivos deben leerse COMPLETAMENTE para análisis honesto
        
        // Buscar discrepancias entre ambos archivos
        INFORMAR usuario: "🔍 Analizando discrepancias..."
        
        // MÉTODO DE ANÁLISIS: Comparación manual y honesta
        // 1. Leer ambos archivos completamente (ya hecho)
        // 2. Comparar contenido relevante entre archivo de programación y contexto
        // 3. Identificar diferencias reales que necesiten actualización
        // 4. Contar el número EXACTO de discrepancias encontradas
        // 5. Reportar el número real (0, 1, 2, 3, 4, etc.) SIN FORZAR CIFRAS
        
        // EJEMPLO DE ANÁLISIS:
        // - Si NetworkManager.cs tiene nuevas clases/métodos no documentados -> discrepancia
        // - Si estructura-inicial.txt no menciona NetworkManager.cs -> discrepancia  
        // - Si tamaños de archivos (líneas) no coinciden -> discrepancia
        // - Si funcionalidades nuevas no están descritas -> discrepancia
        
        // IMPORTANTE: Ser HONESTO sobre el número real de diferencias
        // NO asumir siempre 3 discrepancias
        // NO forzar agrupaciones artificiales
        // Reportar el número exacto encontrado
        
        discrepancias = ANALIZAR discrepancias ENTRE contenido_programacion Y contenido_contexto
        
        // IMPORTANTE: Reportar el número REAL de discrepancias encontradas
        SI discrepancias.length > 0:
            INFORMAR usuario: "⚠️ Se encontraron " + discrepancias.length + " discrepancias"
            
            // Filtrar discrepancias relevantes según el tipo de archivo de contexto
            discrepancias_relevantes = FILTRAR discrepancias RELEVANTES PARA archivo_contexto
            
            // IMPORTANTE: Reportar el número REAL de discrepancias relevantes
            SI discrepancias_relevantes.length > 0:
                INFORMAR usuario: "🔄 Actualizando " + archivo_contexto + " con " + discrepancias_relevantes.length + " discrepancias relevantes..."
                
                // Actualizar automáticamente el archivo de contexto
                ACTUALIZAR archivo_contexto CON discrepancias_relevantes DESDE contenido_programacion
                
                // Añadir timestamp de actualización
                AÑADIR al final de archivo_contexto: "=== ÚLTIMA ACTUALIZACIÓN: " + FECHA_Y_HORA_ACTUAL + " ==="
                
                INFORMAR usuario: "✅ " + archivo_contexto + " actualizado correctamente"
            SI NO:
                INFORMAR usuario: "ℹ️ No hay discrepancias relevantes para actualizar"
            FIN SI
        SI NO:
            INFORMAR usuario: "⚠️ Se encontraron 0 discrepancias"
        FIN SI
        
        // Eliminar la línea procesada de listacomprobaciones.txt
        
        // MÉTODO ROBUSTO POR COMANDOS (evita errores de archivo bloqueado)
        // Paso 1: Crear archivo temporal sin la primera línea
        EJECUTAR comando: "powershell -Command \"Get-Content 'listacomprobaciones.txt' | Select-Object -Skip 1 | Set-Content 'listacomprobaciones_temp.txt'\""
        
        // Paso 2: Verificar que el archivo temporal se creó correctamente
        SI EXISTE "listacomprobaciones_temp.txt":
            // Paso 3: Eliminar archivo original
            EJECUTAR comando: "powershell -Command \"Remove-Item 'listacomprobaciones.txt'\""
            
            // Paso 4: Renombrar temporal a original
            EJECUTAR comando: "powershell -Command \"Rename-Item 'listacomprobaciones_temp.txt' 'listacomprobaciones.txt'\""
            
            // Paso 5: Verificar resultado final
            SI EXISTE "listacomprobaciones.txt":
                // Contar líneas restantes
                lineas_restantes = CONTAR líneas en listacomprobaciones.txt
                
                SI lineas_restantes == 0:
                    INFORMAR usuario: "📋 listacomprobaciones.txt queda vacío. Eliminando archivo..."
                    EJECUTAR comando: "powershell -Command \"Remove-Item 'listacomprobaciones.txt'\""
                    INFORMAR usuario: "✅ listacomprobaciones.txt eliminado. Todas las parejas procesadas."
                SI NO:
                    INFORMAR usuario: "📊 Quedan " + lineas_restantes + " parejas por procesar en listacomprobaciones.txt"
                FIN SI
            SI NO:
                INFORMAR usuario: "❌ Error: No se pudo recrear listacomprobaciones.txt"
                INFORMAR usuario: "🔄 Refrescando procedimiento para continuar correctamente..."
                
                // REFRESCAR AUTOMÁTICAMENTE Y REINICIAR
                VOLVER al Paso 0 (Refrescar Procedimiento)
            FIN SI
        SI NO:
            INFORMAR usuario: "❌ Error: No se pudo crear archivo temporal"
            INFORMAR usuario: "🔄 Refrescando procedimiento para continuar correctamente..."
            
            // REFRESCAR AUTOMÁTICAMENTE Y REINICIAR
            VOLVER al Paso 0 (Refrescar Procedimiento)
        FIN SI
        
        INFORMAR usuario: "🔄 Proceso completado para esta pareja"
        // Continuar con el siguiente paso
    SI NO:
        INFORMAR usuario: "❌ Formato de línea inválido: " + primera_linea
        INFORMAR usuario: "🔄 Refrescando procedimiento para continuar correctamente..."
        
        // REFRESCAR AUTOMÁTICAMENTE Y REINICIAR
        VOLVER al Paso 0 (Refrescar Procedimiento)
    FIN SI
    // Continuar con el siguiente paso
SI NO EXISTE:
    INFORMAR usuario: "⚠️ No se encontró listacomprobaciones.txt. Creando listas..."
    
    // Obtener todos los archivos de programación del juego
    lista_programacion = BUSCAR archivos con patrón:
        - "**/*.cs" (archivos C# únicamente)
    
    // Excluir archivos temporales y de sistema
    FILTRAR lista_programacion EXCLUYENDO:
        - archivos en carpetas ".git", "bin", "obj", ".vs"
        - archivos con "~" o ".tmp" al inicio/final
        - archivos "AssemblyInfo.cs"
    
    // Obtener todos los archivos de contexto
    lista_contexto = BUSCAR archivos en carpeta "contexto/" con patrones:
        - "*.txt"
    
    INFORMAR usuario: "✅ Listas creadas:"
    INFORMAR usuario: "   - Archivos de programación: X"
    INFORMAR usuario: "   - Archivos de contexto: X"
    
    // Crear archivo listacomprobaciones.txt con combinaciones filtradas
    CREAR archivo "listacomprobaciones.txt"
    
    PARA CADA archivo_programacion I EN lista_programacion:
        PARA CADA archivo_contexto J EN lista_contexto:
         # CÓMO USAR EL SISTEMA DE SINCRONIZACIÓN DE CONTEXTO

## ADVERTENCIA IMPORTANTE SOBRE ANÁLISIS DE DISCREPANCIAS
--------------------------------------------------------------------------------

### REGLA CRÍTICA: NO FORZAR UN NÚMERO ESPECÍFICO DE DISCREPANCIAS
- El análisis debe ser HONESTO sobre el número real de diferencias encontradas
- NO asumir que siempre habrá 3 discrepancias
- Reportar el número exacto: 0, 1, 2, 4, 5, o cualquier cantidad real
- Si no hay diferencias, reportar "0 discrepancias"
- Si hay 1 diferencia, reportar "1 discrepancia"
- Si hay múltiples diferencias, reportar el número real

### MÉTODO CORRECTO DE ANÁLISIS
1. Leer ambos archivos COMPLETAMENTE (SIN usar parámetro 'limit')
2. Comparar contenido línea por línea o sección por sección
3. Contar el número REAL de diferencias encontradas
4. Filtrar solo las discrepancias relevantes para el contexto específico
5. Reportar el número REAL de discrepancias relevantes
6. NO redondear, NO aproximar, NO forzar números

### EJEMPLOS CORRECTOS
- "⚠️ Se encontraron 0 discrepancias" (si no hay diferencias)
- "⚠️ Se encontraron 1 discrepancia" (si hay una diferencia)
- "⚠️ Se encontraron 4 discrepancias" (si hay cuatro diferencias)
- "⚠️ Se encontraron 7 discrepancias" (si hay siete diferencias)

### EJEMPLOS INCORRECTOS (PROHIBIDOS)
- "⚠️ Se encontraron 3 discrepancias" (cuando realmente hay 0, 1, 2, 4, 5, etc.)
- Forzar agrupar diferencias en 3 puntos
- Asumir que siempre habrá el mismo número de discrepancias

--------------------------------------------------------------------------------

## PROPÓSITONES
            SI ES_COMBINACION_RELEVANTE(archivo_programacion[I], archivo_contexto[J]):
                ESCRIBIR línea: archivo_programacion[I] + "***" + archivo_contexto[J] en listacomprobaciones.txt
            FIN SI
        FIN PARA CADA
    FIN PARA CADA
    
    INFORMAR usuario: "✅ listacomprobaciones.txt creado con X combinaciones relevantes"
    // Continuar con el siguiente paso
FIN SI

//--------------------------------------------------------------------------------
FUNCIÓN ES_COMBINACION_RELEVANTE(archivo_programacion, archivo_contexto)
//--------------------------------------------------------------------------------

// OBTENER categoría del archivo de programación
SI archivo_programacion CONTIENE "network/":
    categoria_programacion = "RED"
SI NO SI archivo_programacion CONTIENE "player/":
    categoria_programacion = "JUGADOR"
SI NO SI archivo_programacion CONTIENE "terrain/":
    categoria_programacion = "TERRENO"
SI NO SI archivo_programacion CONTIENE "ui/":
    categoria_programacion = "UI"
SI NO SI archivo_programacion CONTIENE "systems/":
    categoria_programacion = "SISTEMAS"
SI NO SI archivo_programacion CONTIENE "world/":
    categoria_programacion = "MUNDOS"
SI NO SI archivo_programacion CONTIENE "character/":
    categoria_programacion = "PERSONAJES"
SI NO SI archivo_programacion CONTIENE "autoload/":
    categoria_programacion = "GLOBAL"
SI NO:
    categoria_programacion = "GENERAL"
FIN SI

// OBTENER categoría del archivo de contexto
SI archivo_contexto CONTIENE "menus-y-servidor":
    categoria_contexto = "RED"
SI NO SI archivo_contexto CONTIENE "mundos-y-partidas":
    categoria_contexto = "MUNDOS"
SI NO SI archivo_contexto CONTIENE "mecanicas":
    categoria_contexto = "JUGADOR"
SI NO SI archivo_contexto CONTIENE "generacion-terreno":
    categoria_contexto = "TERRENO"
SI NO SI archivo_contexto CONTIENE "estado-actual":
    categoria_contexto = "GENERAL"
SI NO SI archivo_contexto CONTIENE "estructura-inicial":
    categoria_contexto = "GENERAL"
SI NO SI archivo_contexto CONTIENE "resumen":
    categoria_contexto = "GENERAL"
SI NO:
    categoria_contexto = "GENERAL"
FIN SI

// REGLAS DE RELEVANCIA
RETORNAR verdadero SI:
    - categoria_programacion == categoria_contexto
    - categoria_programacion == "GENERAL"
    - categoria_contexto == "GENERAL"
    - categoria_programacion == "SISTEMAS" Y categoria_contexto == "GENERAL"
    - categoria_programacion == "GLOBAL" Y categoria_contexto == "GENERAL"
    - categoria_programacion == "RED" Y categoria_contexto == "GENERAL"
    - categoria_programacion == "UI" Y categoria_contexto == "GENERAL"

SI NO:
    RETORNAR falso
FIN SI
```

// Preguntar si desea ejecutar el algoritmo completo otra vez (fuera de cualquier condición)
PREGUNTAR usuario: "¿Desea ejecutar el algoritmo completo otra vez? (S/N)"

SI respuesta es "S":
    INFORMAR usuario: "🔄 Reiniciando algoritmo completo..."
    VOLVER al inicio del Paso 1
SI NO:
    INFORMAR usuario: "🛑 Algoritmo detenido por el usuario"
    TERMINAR procedimiento
FIN SI

*Documento en construcción...*
