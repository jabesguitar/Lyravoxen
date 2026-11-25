$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing

$root = Split-Path -Parent $PSScriptRoot
$assets = Join-Path $root "src/Lyravoxen.Desktop/Assets"
$source = Join-Path $assets "Lyravoxen_Icone_App_1024.png"
$target = Join-Path $assets "LyravoxenSplash_Compact.png"

if (-not (Test-Path $source)) { throw "Logo source not found: $source" }

$width = 520
$height = 460
$bitmap = [System.Drawing.Bitmap]::new($width, $height)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality

$rect = [System.Drawing.Rectangle]::new(0, 0, $width, $height)
$background = [System.Drawing.Drawing2D.LinearGradientBrush]::new(
    $rect,
    [System.Drawing.Color]::FromArgb(255, 5, 17, 43),
    [System.Drawing.Color]::FromArgb(255, 9, 32, 67),
    [single]90
)
$graphics.FillRectangle($background, $rect)

$logo = [System.Drawing.Image]::FromFile($source)
$graphics.DrawImage($logo, 135, 28, 250, 250)
$logo.Dispose()

$cyan = [System.Drawing.Color]::FromArgb(255, 0, 217, 245)
$white = [System.Drawing.Color]::FromArgb(255, 242, 246, 248)
$muted = [System.Drawing.Color]::FromArgb(255, 181, 200, 231)
$titleFont = [System.Drawing.Font]::new("Segoe UI", 29, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
$tagFont = [System.Drawing.Font]::new("Segoe UI", 10, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
$titleBrush = [System.Drawing.SolidBrush]::new($white)
$tagBrush = [System.Drawing.SolidBrush]::new($muted)
$linePen = [System.Drawing.Pen]::new($cyan, 2)
$title = "Lyravoxen"
$tag = "LETRAS PARA O SEU LOUVOR"
$titleSize = $graphics.MeasureString($title, $titleFont)
$tagSize = $graphics.MeasureString($tag, $tagFont)
$graphics.DrawString($title, $titleFont, $titleBrush, [single](($width - $titleSize.Width) / 2), 304)
$graphics.DrawLine($linePen, 190, 350, 330, 350)
$graphics.DrawString($tag, $tagFont, $tagBrush, [single](($width - $tagSize.Width) / 2), 369)

$bitmap.Save($target, [System.Drawing.Imaging.ImageFormat]::Png)
$linePen.Dispose()
$tagBrush.Dispose()
$titleBrush.Dispose()
$tagFont.Dispose()
$titleFont.Dispose()
$background.Dispose()
$graphics.Dispose()
$bitmap.Dispose()
Write-Host "Compact splash generated: $target"
