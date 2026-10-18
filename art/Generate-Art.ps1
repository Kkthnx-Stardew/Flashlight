# Draws every picture the mod uses. Run: powershell -ExecutionPolicy Bypass -File art\Generate-Art.ps1
# Output: Flashlight\assets\*.png (item sheets and beam textures) and art\preview*.png (enlarged, for looking at).
Add-Type -AssemblyName System.Drawing
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$assets = Join-Path (Join-Path $root 'Flashlight') 'assets'
New-Item -ItemType Directory -Force $assets | Out-Null

function C([string]$hex) { [System.Drawing.ColorTranslator]::FromHtml($hex) }

. (Join-Path $PSScriptRoot 'Generate-Icons.ps1')
. (Join-Path $PSScriptRoot 'Generate-Held.ps1')

# ---- the light beam: a cone with a bright middle and soft sides with nothing around the farmer (an optional soft light around them is a second light in code), 128 by 128, white with the light in the alpha channel.
# The light sits at the centre of the picture. One file per way the farmer can face.
function New-Beam([int]$turns) {
  $size = 128; $half = $size / 2
  $bmp = New-Object System.Drawing.Bitmap $size, $size
  $range = 62.0; $spread = 30.0 * [Math]::PI / 180.0; $glow = 0.0
  for ($y = 0; $y -lt $size; $y++) { for ($x = 0; $x -lt $size; $x++) {
    $dx = $x + 0.5 - $half; $dy = $y + 0.5 - $half
    # turn the picture so the beam points right, 0 = right, 1 = down, 2 = left, 3 = up
    switch ($turns) { 0 { $rx = $dx; $ry = $dy } 1 { $rx = $dy; $ry = -$dx } 2 { $rx = -$dx; $ry = -$dy } 3 { $rx = -$dy; $ry = $dx } }
    $d = [Math]::Sqrt($rx * $rx + $ry * $ry)
    $coneA = 0.0
    if ($rx -gt 0 -and $d -lt $range) {
      $ang = [Math]::Abs([Math]::Atan2($ry, $rx))
      if ($ang -lt $spread) {
        $edge = 1.0 - ($ang / $spread); $edge = [Math]::Min(1.0, $edge * 3.0)   # soft sides, a wide bright middle
        $fall = [Math]::Pow(1.0 - ($d / $range), 0.9)
        $coneA = $edge * $fall * 1.0
      }
    }
    $glowA = 0.0
    if ($d -lt $glow) { $glowA = [Math]::Pow(1.0 - ($d / $glow), 1.6) * 0.55 }
    $a = [Math]::Min(1.0, $coneA + $glowA)
    $bmp.SetPixel($x, $y, [System.Drawing.Color]::FromArgb([int][Math]::Round($a * 255), 255, 255, 255))
  } }
  return $bmp
}
$beamNames = 'right', 'down', 'left', 'up'
$beams = for ($i = 0; $i -lt 4; $i++) { $bm = New-Beam $i; $bm.Save((Join-Path $assets ('beam-' + $beamNames[$i] + '.png'))); $bm }

# a preview of the beams as warm light on a dark night picture
$night = New-Object System.Drawing.Bitmap (128 * 4 + 5 * 8), (128 + 16)
$g2 = [System.Drawing.Graphics]::FromImage($night); $g2.Clear((C '#141a2e'))
$tint = New-Object System.Drawing.Imaging.ColorMatrix
$tint.Matrix33 = 1.0
$attr = New-Object System.Drawing.Imaging.ImageAttributes
$cm = New-Object System.Drawing.Imaging.ColorMatrix (, @(
  @(1.0, 0, 0, 0, 0), @(0, 0.92, 0, 0, 0), @(0, 0, 0.55, 0, 0), @(0, 0, 0, 1.0, 0), @(0, 0, 0, 0, 1.0)))
$attr.SetColorMatrix($cm)
for ($i = 0; $i -lt 4; $i++) {
  $rect = New-Object System.Drawing.Rectangle ((8 + $i * 136), 8, 128, 128)
  $g2.DrawImage($beams[$i], $rect, 0, 0, 128, 128, [System.Drawing.GraphicsUnit]::Pixel, $attr)
  $g2.FillRectangle([System.Drawing.Brushes]::White, (8 + $i * 136 + 62), (8 + 62), 4, 4)   # the farmer's spot
}
$g2.Dispose(); $night.Save((Join-Path $PSScriptRoot 'preview-beams.png'))
"beams done"
