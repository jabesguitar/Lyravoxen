$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing

$projectAssets = Join-Path $PSScriptRoot "..\src\Lyravoxen.Desktop\Assets"
$artifactDir = Join-Path $PSScriptRoot "..\artifacts"
New-Item -ItemType Directory -Force $projectAssets, $artifactDir | Out-Null

$size = 256
$bitmap = [System.Drawing.Bitmap]::new($size, $size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$graphics.Clear([System.Drawing.Color]::Transparent)

$path = [System.Drawing.Drawing2D.GraphicsPath]::new()
$path.AddArc(8, 8, 48, 48, 180, 90)
$path.AddArc(200, 8, 48, 48, 270, 90)
$path.AddArc(200, 200, 48, 48, 0, 90)
$path.AddArc(8, 200, 48, 48, 90, 90)
$path.CloseFigure()
$background = [System.Drawing.Drawing2D.LinearGradientBrush]::new(
  [System.Drawing.Rectangle]::new(0,0,$size,$size),
  [System.Drawing.Color]::FromArgb(13,24,36),
  [System.Drawing.Color]::FromArgb(27,57,72),
  45
)
$graphics.FillPath($background, $path)
$accent = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(57,214,197))
$penStrong = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(190,239,248,250), 5)
$penSoft = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(115,239,248,250), 5)
$heights = @(42,78,112,142,108,72,48)
for ($i=0; $i -lt $heights.Length; $i++) {
  $x = 49 + ($i * 23)
  $y = 120 - [int]($heights[$i] / 2)
  $graphics.FillRectangle($accent, $x, $y, 12, $heights[$i])
}
$graphics.DrawLine($penStrong, 70, 190, 186, 190)
$graphics.DrawLine($penSoft, 88, 207, 168, 207)

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
$penStrong.Dispose()
$penSoft.Dispose()
$accent.Dispose()
$background.Dispose()
$graphics.Dispose()
$bitmap.Dispose()
Write-Host "Generated $icoPath and $artifactIco"
