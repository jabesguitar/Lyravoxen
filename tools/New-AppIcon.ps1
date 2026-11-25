$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing

$projectAssets = Join-Path $PSScriptRoot "..\src\Lyravoxen.Desktop\Assets"
$artifactDir = Join-Path $PSScriptRoot "..\artifacts"
New-Item -ItemType Directory -Force $projectAssets, $artifactDir | Out-Null

# Use the approved Lyravoxen brand artwork instead of drawing a generic placeholder.
$brandPng = Join-Path $projectAssets "Lyravoxen_Icone_App_1024.png"
if (-not (Test-Path $brandPng)) {
  throw "Missing approved brand icon: $brandPng. Add Lyravoxen_Icone_App_1024.png to src/Lyravoxen.Desktop/Assets."
}

$size = 256
$source = [System.Drawing.Image]::FromFile($brandPng)
$bitmap = [System.Drawing.Bitmap]::new($size, $size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
$graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$graphics.Clear([System.Drawing.Color]::Transparent)
$graphics.DrawImage($source, 0, 0, $size, $size)

$pngPath = Join-Path $projectAssets "Lyravoxen.png"
$icoPath = Join-Path $projectAssets "Lyravoxen.ico"
$artifactIco = Join-Path $artifactDir "Lyravoxen.ico"
$bitmap.Save($pngPath, [System.Drawing.Imaging.ImageFormat]::Png)
$pngBytes = [System.IO.File]::ReadAllBytes($pngPath)

$stream = [System.IO.MemoryStream]::new()
$writer = [System.IO.BinaryWriter]::new($stream)
$writer.Write([UInt16]0)
$writer.Write([UInt16]1)
$writer.Write([UInt16]1)
$writer.Write([Byte]0)
$writer.Write([Byte]0)
$writer.Write([Byte]0)
$writer.Write([Byte]0)
$writer.Write([UInt16]1)
$writer.Write([UInt16]32)
$writer.Write([UInt32]$pngBytes.Length)
$writer.Write([UInt32]22)
$writer.Write($pngBytes)
[System.IO.File]::WriteAllBytes($icoPath, $stream.ToArray())
[System.IO.File]::Copy($icoPath, $artifactIco, $true)

$writer.Dispose()
$stream.Dispose()
$graphics.Dispose()
$bitmap.Dispose()
$source.Dispose()
Write-Host "Generated branded icon $icoPath and $artifactIco"
