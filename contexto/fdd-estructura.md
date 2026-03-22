# Estructura de Carpetas FDD - Wild v2.0

## 📁 **Estructura Propuesta**

```
wild-new/
├── contexto/
│   ├── fdd/                           # Sistema FDD completo
│   │   ├── features/                  # Archivos individuales de features
│   │   │   ├── feature-001-prototipo-base.md
│   │   │   ├── feature-002-biomas.md
│   │   │   ├── feature-003-optimizacion.md
│   │   │   ├── feature-004-red.md
│   │   │   ├── feature-005-modelado.md
│   │   │   ├── feature-006-menus.md
│   │   │   └── feature-007-pulido.md
│   │   ├── active/                    # Feature actual en desarrollo
│   │   │   ├── current-feature.md     # Copia de trabajo
│   │   │   ├── daily-notes/           # Notas diarias
│   │   │   │   ├── 2025-03-10.md
│   │   │   │   ├── 2025-03-11.md
│   │   │   │   └── ...
│   │   │   ├── design-decisions.md    # Decisiones arquitectónicas
│   │   │   └── progress-metrics.md    # Métricas de progreso
│   │   ├── completed/                  # Features completadas
│   │   │   ├── feature-001-prototipo-base/
│   │   │   │   ├── final-report.md
│   │   │   │   ├── lessons-learned.md
│   │   │   │   ├── code-samples/
│   │   │   │   └── screenshots/
│   │   │   └── [demás features completadas]
│   │   ├── backlog/                    # Features pendientes
│   │   │   ├── feature-002-biomas-draft.md
│   │   │   ├── feature-003-optimizacion-draft.md
│   │   │   └── ...
│   │   ├── templates/                 # Plantillas
│   │   │   ├── feature-template.md
│   │   │   ├── daily-note-template.md
│   │   │   └── completion-report.md
│   │   └── metrics/                   # Métricas globales
│   │       ├── velocity-tracking.md
│   │       ├── burndown-charts.md
│   │       └── quality-metrics.md
│   ├── resumen.md                      # Plan principal
│   └── feature-template.md             # Plantilla actual
```

## 🔄 **Flujo de Archivos**

### **1. Inicio de Feature**
```
backlog/feature-XXX-draft.md 
    ↓ (copiar y activar)
active/current-feature.md
    ↓ (trabajo diario)
active/daily-notes/YYYY-MM-DD.md
```

### **2. Durante Desarrollo**
```
active/current-feature.md (actualización continua)
active/design-decisions.md (decisiones importantes)
active/progress-metrics.md (métricas diarias)
```

### **3. Finalización de Feature**
```
active/current-feature.md 
    ↓ (mover al completar)
completed/feature-XXX/
    ├── final-report.md
    ├── lessons-learned.md
    └── code-samples/
```

## 📊 **Archivos de Métricas**

### **Velocity Tracking**
- Features por semana
- Tiempo real vs estimado
- Tendencias de velocidad

### **Quality Metrics**
- Bugs por feature
- Tiempo de resolución
- Complejidad técnica

### **Burndown Charts**
- Progreso diario
- Remaining work
- Sprint completion

## 🎯 **Beneficios de esta Estructura**

1. **Claridad**: Cada feature tiene su espacio dedicado
2. **Trazabilidad**: Historial completo de decisiones
3. **Métricas**: Datos concretos del progreso
4. **Reutilización**: Plantillas y patrones guardados
5. **Auditoría**: Registro completo del desarrollo
