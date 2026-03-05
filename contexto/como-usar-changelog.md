# CÓMO USAR EL SISTEMA DE CHANGELOG

## PROPÓSITO
Sistema modular para gestionar el changelog del proyecto Wild de forma organizada y escalable.

## CUÁNDO USAR ESTE SISTEMA
Siempre que necesites añadir cambios al changelog del proyecto Wild.

## PASOS A SEGUIR

### 1. VERIFICAR VERSIÓN ACTUAL
- Leer `contexto/resumen.txt` para conocer la versión actual
- Versión actual: "v0.2.1-ALPHA"
- **REGLA IMPORTANTE**: Solo el usuario puede cambiar de versión

### 2. AÑADIR CAMBIOS

#### Para cambios en versión actual:
- Editar archivo existente: `changelog/v0.2.1-ALPHA.md`
- Añadir nuevos cambios al final del archivo

#### Para nueva versión (solo usuario):
- Si el usuario indica cambiar de versión, crear nuevo archivo:
- Nombre: `vX.Y.Z-VERSION.md` en carpeta `changelog/`
- Formato:
```markdown
# Versión X.Y.Z-VERSION
**Fecha:** YYYY-MM-DD  
**Estado:** Breve descripción

### 📋 CATEGORÍA
- ✅ **Cambio implementado** - Descripción
```

### 3. GENERAR CHANGELOG FINAL
- Ejecutar: `.\changelog\build_changelog_simple.ps1`
- Esto actualiza `CHANGELOG.txt` automáticamente

## REGLAS FUNDAMENTALES

- **NO EDITAR CHANGELOG.txt directamente** - Es generado automáticamente
- **Siempre verificar versión actual** antes de crear archivos nuevos
- **Usar formato consistente** en todos los archivos
- **Ejecutar script después de cambios** para actualizar changelog

## EJEMPLOS DE USO

### CASO 1: Añadir cambios a versión actual
```
Usuario: "añade al changelog que implementamos ModelSpawner"
IA: 
1. Edita changelog/v0.2.1-ALPHA.md
2. Añade: "### 🚀 SISTEMA DE SPAWN DE MODELOS - ✅ **ModelSpawner implementado** - ..."
3. Ejecuta script de compilación
```

### CASO 2: Cambiar a nueva versión (solo usuario)
```
Usuario: "cambia la versión a 0.3.0-ALPHA"
IA:
1. Verifica que el usuario lo solicita explícitamente
2. Crea changelog/v0.3.0-ALPHA.md
3. Actualiza resumen.txt y estado-actual.txt
```

## COMANDOS ÚTILES

```powershell
# Generar changelog completo
.\changelog\build_changelog_simple.ps1

# Ver archivos de versión existentes
Get-ChildItem changelog\v*.md | Sort-Object Name -Descending
```

## VERIFICACIÓN

Después de generar el changelog:
1. Verificar que `CHANGELOG.txt` se actualizó
2. Confirmar que contiene los cambios añadidos
3. Revisar que el formato es correcto

## ESTRUCTURA DE ARCHIVOS

### Carpeta `changelog/`
- `v0.1.0-ALPHA.md` - Cambios de la versión 0.1.0
- `v0.1.1-ALPHA.md` - Cambios de la versión 0.1.1
- `v0.1.2-ALPHA.md` - Cambios de la versión 0.1.2
- `v0.1.3-ALPHA.md` - Cambios de la versión 0.1.3
- `v0.2.0-ALPHA.md` - Cambios de la versión 0.2.0
- `v0.2.1-ALPHA.md` - Cambios de la versión 0.2.1 (versión actual)
- `build_changelog_simple.ps1` - Script de compilación

### Archivo raíz
- `CHANGELOG.txt` - Archivo generado automáticamente (NO EDITAR MANUALMENTE)

---

Este sistema mantiene el changelog organizado y escalable sin importar el tamaño del proyecto.

Última actualización: 2026-03-05 - Añadida integración con GameServer.cs (red independiente)
