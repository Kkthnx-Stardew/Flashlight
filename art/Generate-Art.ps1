# Draws the item art for the flashlight mod. Run: powershell -File art\Generate-Art.ps1
# Output: Flashlight\assets\*.png (item sheets and beam textures) and art\preview*.png (enlarged, for looking at).
#
# Style rules, measured from the game's own 16x16 icons (Torch, Copper Bar, Iron Bar, Coal, Copper Axe, Glow Ring):
#  - the outline is a darker shade of the object's own colour, never plain black
#  - three to five tones per material, light from the top left
#  - tools are drawn on the diagonal, handle bottom left, working end top right
Add-Type -AssemblyName System.Drawing
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$assets = Join-Path (Join-Path $root 'Flashlight') 'assets'
New-Item -ItemType Directory -Force $assets | Out-Null

function C([string]$hex) { [System.Drawing.ColorTranslator]::FromHtml($hex) }

# ---- materials: outline, shadow, base, light, highlight
$mat = @{
  copper = @('#431600', '#692c00', '#b35500', '#d87304', '#ff9f2a')
  wood   = @('#3a2008', '#5f3a17', '#76481d', '#b57335', '#df8e42')
  steel  = @('#305371', '#82a5c3', '#c2e5ff', '#e0f2ff', '#f7ffff')
  glass  = @('#305371', '#4f7fa6', '#7fb0d6', '#b4dcf5', '#ffffff')
  brass  = @('#431600', '#5b2a00', '#8a4200', '#b35500', '#d87304')
  lampOn = @('#c64c00', '#ff8900', '#ffd900', '#ffff2b', '#fff9f2')
  gold   = @('#6a2c00', '#c8761f', '#f2a52b', '#ffd45e', '#fff0a8')
}

# ---- the flashlight, built from rotated pieces and shaded like a cylinder
function New-Flashlight([bool]$on) {
  $cx = 7.0; $cy = 8.8; $r2 = [Math]::Sqrt(2)
  # pieces along the axis: from u0 to u1, half width at each end, material
  $pieces = @(
    @{ u0 = -7.2; u1 = -6.2; h0 = 1.5; h1 = 1.5; m = 'steel' },
    @{ u0 = -6.2; u1 = -1.0; h0 = 1.6; h1 = 1.6; m = 'wood' },
    @{ u0 = -1.0; u1 = 0.1;  h0 = 2.0; h1 = 2.0; m = 'brass' },
    @{ u0 = 0.1;  u1 = 3.6;  h0 = 2.0; h1 = 2.0; m = 'copper' },
    @{ u0 = 3.6;  u1 = 5.6;  h0 = 2.0; h1 = 3.3; m = 'copper' },
    @{ u0 = 5.6;  u1 = 7.8;  h0 = 3.3; h1 = 3.3; m = 'steel' }
  )
  $lens = @{ u0 = 6.2; u1 = 7.8; h = 2.6; m = $(if ($on) { 'lampOn' } else { 'glass' }) }
  $grid = New-Object 'object[,]' 16, 16   # each cell: @{m=material; t=shade 0..4} or $null
  for ($y = 0; $y -lt 16; $y++) { for ($x = 0; $x -lt 16; $x++) {
    $dx = $x + 0.5 - $cx; $dy = $y + 0.5 - $cy
    $u = ($dx - $dy) / $r2; $v = ($dx + $dy) / $r2
    $hit = $null
    foreach ($p in $pieces) {
      if ($u -ge $p.u0 -and $u -lt $p.u1) {
        $hw = $p.h0 + ($p.h1 - $p.h0) * (($u - $p.u0) / ($p.u1 - $p.u0))
        if ([Math]::Abs($v) -le $hw) { $hit = @{ m = $p.m; t = $v / $hw } }
      }
    }
    if ($u -ge $lens.u0 -and $u -lt $lens.u1 -and [Math]::Abs($v) -le $lens.h) { $hit = @{ m = $lens.m; t = $v / $lens.h; lens = $true } }
    $grid[$x, $y] = $hit
  } }
  return ,$grid
}

function Render-Grid($grid, [bool]$on, [bool]$bands) {
  $bmp = New-Object System.Drawing.Bitmap 16, 16
  for ($y = 0; $y -lt 16; $y++) { for ($x = 0; $x -lt 16; $x++) {
    $cell = $grid[$x, $y]; if ($null -eq $cell) { continue }
    $edge = $false
    foreach ($d in @(@(1,0), @(-1,0), @(0,1), @(0,-1))) {
      $nx = $x + $d[0]; $ny = $y + $d[1]
      if ($nx -lt 0 -or $ny -lt 0 -or $nx -gt 15 -or $ny -gt 15 -or $null -eq $grid[$nx, $ny]) { $edge = $true }
    }
    $ramp = $mat[$cell.m]
    if ($edge) { $bmp.SetPixel($x, $y, (C $ramp[0])); continue }
    $t = $cell.t
    $tone = if ($t -lt -0.55) { 4 } elseif ($t -lt -0.1) { 3 } elseif ($t -lt 0.45) { 2 } else { 1 }
    if ($cell.lens) { $tone = if ($t -lt -0.3) { 4 } elseif ($t -lt 0.3) { 3 } else { 2 } }
    if ($cell.m -eq 'wood' -and ([int][Math]::Floor(($x + $y) / 2)) % 2 -eq 0 -and $tone -gt 1) { $tone-- }
    $bmp.SetPixel($x, $y, (C $ramp[$tone]))
  } }
  return $bmp
}

function Add-Pixels($bmp, $list, [string]$hex) { foreach ($p in $list) { $bmp.SetPixel($p[0], $p[1], (C $hex)) } }

$grid = New-Flashlight $false
$off = Render-Grid $grid $false $false
# a glint on the glass
Add-Pixels $off @(, @(11, 4)) '#ffffff'

$gridOn = New-Flashlight $true
$onBmp = Render-Grid $gridOn $true $false
# a few short rays leaving the lens
Add-Pixels $onBmp @(@(13, 1), @(15, 3)) '#ffff2b'
Add-Pixels $onBmp @(@(14, 0), @(15, 2), @(15, 4)) '#ffd900'

# ---- the battery, drawn by hand on a 16 by 16 grid
$batteryRows = @(
  '................',
  '.....NNNNNN.....',
  '.....NDCBBN.....',
  '....NDCCBBBN....',
  '....NCBBBSSN....',
  '....NSSSSSSN....',
  '....12333341....',
  '....12333341....',
  '....12333341....',
  '....12333341....',
  '....12333341....',
  '....12333341....',
  '....12333341....',
  '....NSSSSSSN....',
  '....NNNNNNNN....',
  '................'
)
$map = @{
  'N' = '#305371'; 'D' = '#f7ffff'; 'C' = '#e0f2ff'; 'B' = '#c2e5ff'; 'S' = '#82a5c3'
  '1' = '#6a2c00'; '2' = '#ffd45e'; '3' = '#f2a52b'; '4' = '#c8761f'
}
$bat = New-Object System.Drawing.Bitmap 16, 16
for ($y = 0; $y -lt 16; $y++) { for ($x = 0; $x -lt 16; $x++) {
  $ch = $batteryRows[$y][$x]
  if ($ch -eq '.') { continue }
  $bat.SetPixel($x, $y, (C $map[[string]$ch]))
} }
# the bolt on the label, in the dark outline colour
Add-Pixels $bat @(@(8,6), @(9,6), @(8,7), @(7,8), @(8,8), @(7,9), @(8,9), @(7,10), @(7,11), @(6,11)) '#6a2c00'

# ---- save icons and a preview sheet
# the game reads item pictures from sheets of 16 by 16 cells: tools.png has off then on, objects.png has the battery
$tools = New-Object System.Drawing.Bitmap 32, 16
$tg = [System.Drawing.Graphics]::FromImage($tools); $tg.DrawImageUnscaled($off, 0, 0); $tg.DrawImageUnscaled($onBmp, 16, 0); $tg.Dispose()
$tools.Save((Join-Path $assets 'tools.png'))
$bat.Save((Join-Path $assets 'objects.png'))

$scale = 16; $pad = 8
$sheet = New-Object System.Drawing.Bitmap ((16 * $scale + $pad) * 3 + $pad), (16 * $scale + 2 * $pad)
$g = [System.Drawing.Graphics]::FromImage($sheet)
$g.Clear((C '#d9c08a'))   # a plain tan, close to the inventory slot colour
$g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
$g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
$i = 0
foreach ($b in @($off, $onBmp, $bat)) {
  $g.DrawImage($b, ($pad + $i * (16 * $scale + $pad)), $pad, (16 * $scale), (16 * $scale))
  $i++
}
$g.Dispose()
$sheet.Save((Join-Path $PSScriptRoot 'preview.png'))
"done"

# ---- the light beam: a soft cone plus a small glow around the farmer, 128 by 128, white with the light in the alpha channel.
# The light sits at the centre of the picture. One file per way the farmer can face.
function New-Beam([int]$turns) {
  $size = 128; $half = $size / 2
  $bmp = New-Object System.Drawing.Bitmap $size, $size
  $range = 62.0; $spread = 30.0 * [Math]::PI / 180.0; $glow = 15.0
  for ($y = 0; $y -lt $size; $y++) { for ($x = 0; $x -lt $size; $x++) {
    $dx = $x + 0.5 - $half; $dy = $y + 0.5 - $half
    # turn the picture so the beam points right, 0 = right, 1 = down, 2 = left, 3 = up
    switch ($turns) { 0 { $rx = $dx; $ry = $dy } 1 { $rx = $dy; $ry = -$dx } 2 { $rx = -$dx; $ry = -$dy } 3 { $rx = -$dy; $ry = $dx } }
    $d = [Math]::Sqrt($rx * $rx + $ry * $ry)
    $coneA = 0.0
    if ($rx -gt 0 -and $d -lt $range) {
      $ang = [Math]::Abs([Math]::Atan2($ry, $rx))
      if ($ang -lt $spread) {
        $edge = 1.0 - ($ang / $spread); $edge = [Math]::Min(1.0, $edge * 2.2)   # soft sides
        $fall = [Math]::Pow(1.0 - ($d / $range), 1.35)
        $coneA = $edge * $fall * 0.95
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
