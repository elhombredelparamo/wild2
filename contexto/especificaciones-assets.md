# 📏 Especificaciones Técnicas de Assets - Wild v2.0

Este documento define el estándar técnico para la creación, optimización y exportación de activos gráficos (3D y 2D), asegurando la compatibilidad con el sistema de **Calidad Dinámica**.

---

## 🏗️ 1. Estándares de Modelado 3D (Poligonización)

El presupuesto de polígonos se basa en un **factor de decimación de 0.5** entre niveles correlativos.

### 📋 Tabla de Presupuesto por Categoría

| Categoría | Ultra (1.0) | High (0.5) | Medium (0.25) | Low (0.125) | Toaster (0.06) |
| :--- | :---: | :---: | :---: | :---: | :---: |
| **Grandes** (Árboles, Casas) | 8k - 12k | 4k - 6k | 2k - 3k | 1k - 1.5k | 500 - 800 |
| **Medianos** (Personajes, Rocas) | 15k - 20k | 7k - 10k | 3.5k - 5k | 1.8k - 2.5k | 1k |
| **Pequeños** (Herramientas, Flora) | 1k - 2k | 500 - 1k | 250 - 500 | 125 - 250 | < 100 |

> [!IMPORTANT]
> **Toaster Level**: Para el nivel Toaster, se permite el uso de **Billboards** (planos con textura) o **Proxy Meshes** extremadamente simplificadas que conserven solo la silueta básica.

---

## 🎨 2. Estándares de Texturizado

Todas las texturas deben seguir el esquema PBR (Albedo, Normal, Roughness, Ambient Occlusion/Metallic).

### 📋 Resoluciones y Formatos

| Nivel | Resolución Máxima | Normal Map | MIPMaps |
| :--- | :---: | :---: | :---: |
| **Ultra** | 4K (4096px) | 16-bit (Alta Fidelidad) | ✅ Sí |
| **High** | 2K (2048px) | 16-bit | ✅ Sí |
| **Medium** | 1K (1024px) | 8-bit (Comprimido) | ✅ Sí |
| **Low** | 512px | 8-bit | ✅ Sí |
| **Toaster** | 256px | Desactivado / 8-bit | ❌ No |

### 🛠️ Formatos de Compresión Recomendados
- **VRAM**: Usar formatos `VRAM Compressed` (S3TC/BC7) para optimizar el consumo de memoria de video.
- **Normal Maps**: Deben exportarse con el eje Y invertido (estilo DirectX o según configuración de Godot) si es necesario.

---

## 📐 3. Niveles de Detalle (LOD)

La transición entre calidades está vinculada a la distancia de visualización.

| Salto de LOD | Umbral de Distancia | Acción Técnica |
| :--- | :---: | :--- |
| **LOD 0 -> 1** | 25m - 50m | Cambio de Ultra a High. |
| **LOD 1 -> 2** | 50m - 100m | Cambio a Medium. |
| **LOD 2 -> 3** | 100m - 200m | Cambio a Low. |
| **LOD 3 -> 4** | 200m+ | Cambio a Toaster / Billboard. |

---

## 🖼️ 4. Interfaz de Usuario e Iconos

Los iconos de los ítems deben generarse en las 5 calidades para evitar desperdicio de textura en la UI.

- **Ultra/High**: 512x512 o 256x256 (PNG con Alpha).
- **Medium**: 128x128.
- **Low/Toaster**: 64x64 o 32x32.

---

## 🗒️ 5. Notas Generales de Exportación

1. **Uv Mapping**: Maximizar el uso del espacio UV (0 a 1). Evitar solapamientos a menos que sea para simetría intencionada.
2. **Origen**: El `Pivot Point` de los objetos debe estar en la base (0,0,0) para assets de suelo (árboles, rocas) o centrado para ítems.
3. **Escala**: 1 unidad Godot = 1 metro real.
