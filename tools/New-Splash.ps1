$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing

$root = Split-Path -Parent $PSScriptRoot
$assets = Join-Path $root "src/Lyravoxen.Desktop/Assets"
$source = Join-Path $assets "Lyravoxen_Icone_App_1024.png"
$target = Join-Path $assets "LyravoxenSplash_Compact.png"

if (-not (Test-Path $source)) { throw "Logo source not found: $source" }

$width = 520
$height = 460
$bitmap = New-Object System.Drawing.Bitmap($width, $height)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality

$rect = New-Object System.Drawing.Rectangle(0, 0, $width, $height)
$background = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
    $rect,
    [System.Drawing.Color]::FromArgb(255, 5, 17, 43),
    [System.Drawing.Color]::FromArgb(255, 9, 32, 67),
    90
)
$graphics.FillRectangle($background, $rect)

$logo = [System.Drawing.Image]::FromFile($source)
$graphics.DrawImage($logo, 135, 28, 250, 250)
$logo.Dispose()

$cyan = [System.Drawing.Color]::FromArgb(255, 0, 217, 245)
$white = [System.Drawing.Color]::FromArgb(255, 242, 246, 248)
$muted = [System.Drawing.Color]::FromArgb(255, 181, 200, 231)
$titleFont = New-Object System.Drawing.Font("Segoe UI", 29, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
$tagFont = New-Object System.Drawing.Font("Segoe UI", 10, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
$title = "Lyravoxen"
$tag = "LETRAS PARA O SEU LOUVOR"
$titleSize = $graphics.MeasureString($title, $titleFont)
$tagSize = $graphics.MeasureString($tag, $tagFont)
$graphics.DrawString($title, $titleFont, (New-Object System.Drawing.SolidBrush($white)), [single](($width - $titleSize.Width) / 2), 304)
$lineBrush = New-Object System.Drawing.SolidBrush($cyan)
$graphics.FillRoundedRectangle
$graphics.FillRectangle($lineBrush, 190, 350, 140, 3)
$graphics.DrawString($tag, $tagFont, (New-Object System.Drawing.SolidBrush($muted)), [single](($width - $tagSize.Width) / 2), 369)

$bitmap.Save($target, [System.Drawing.Imaging.ImageFormat]::Png)
$graphics.Dispose()
$background.Dispose()
$titleFont.Dispose()
$tagFont.Dispose()
$lineBrush.Dispose()
$bitmap.Dispose()
Write-Host "Compact splash generated: $target"
