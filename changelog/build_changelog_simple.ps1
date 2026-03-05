# Script de compilación de CHANGELOG para Wild
# Genera el archivo CHANGELOG.txt en el directorio raíz a partir de los archivos de versión individuales

$ProjectRoot = ".."
$OutputFile = "..\CHANGELOG.txt"
$ChangelogDir = "."

# Encabezado del changelog
$Header = @"
# CHANGELOG - Wild - Videojuego 3D de Simulación Realista

⚠️  **ARCHIVO GENERADO AUTOMATICAMENTE** - No editar manualmente
Para añadir cambios, crea nuevos archivos de versión en la carpeta `changelog/`
y ejecuta este script: `.\changelog\build_changelog.ps1`

---

"@

Write-Host "Construyendo CHANGELOG desde archivos de versión..." -ForegroundColor Green

# Obtener todos los archivos de versión ordenados por nombre (versión)
$VersionFiles = Get-ChildItem -Path "v*.md" | Sort-Object Name -Descending

if ($VersionFiles.Count -eq 0) {
    Write-Host "No se encontraron archivos de versión en $ChangelogDir" -ForegroundColor Red
    exit 1
}

Write-Host "Encontrados $($VersionFiles.Count) archivos de versión:" -ForegroundColor Cyan
$VersionFiles | ForEach-Object { Write-Host "   - $($_.Name)" -ForegroundColor Gray }

# Construir el contenido del changelog
$ChangelogContent = $Header

foreach ($File in $VersionFiles) {
    Write-Host "Procesando $($File.Name)..." -ForegroundColor Yellow
    
    $Content = Get-Content -Path $File.FullName -Raw
    $ChangelogContent += $Content + "`n`n---`n`n"
}

# Eliminar el último separador si existe
if ($ChangelogContent.EndsWith("---`n`n")) {
    $ChangelogContent = $ChangelogContent.Substring(0, $ChangelogContent.Length - 5)
}

# Escribir el archivo final
$ChangelogContent | Out-File -FilePath $OutputFile -Encoding UTF8

Write-Host "CHANGELOG.txt generado exitosamente en $OutputFile" -ForegroundColor Green
Write-Host "Estadísticas:" -ForegroundColor Cyan
Write-Host "   - Versiones procesadas: $($VersionFiles.Count)" -ForegroundColor Gray
$fileSize = [math]::Round((Get-Item $OutputFile).Length / 1KB, 2)
Write-Host "   - Tamaño del archivo: $fileSize KB" -ForegroundColor Gray

# Mostrar preview del resultado
Write-Host "`nPreview del CHANGELOG generado:" -ForegroundColor Cyan
Get-Content -Path $OutputFile | Select-Object -First 10 | ForEach-Object { Write-Host "   $_" -ForegroundColor Gray }
Write-Host "   ..." -ForegroundColor Gray
