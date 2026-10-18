# Draws the item icons: the flashlight (off and on) and the Dry Cell battery. Dot-sourced by Generate-Art.ps1.
#
# Style rules, measured from the game's own 16x16 icons (Torch, Copper Bar, Iron Bar, Coal, Copper Axe, Glow Ring):
#  - the outline is a darker shade of the object's own colour, never plain black
#  - three to five tones per material, light from the top left
#  - tools are drawn on the diagonal, handle bottom left, working end top right
#
# The look follows the reference pictures: a brass head with a glass lens and a dark navy body with a small switch,
# and a D-cell battery in the same navy and brass so the two items read as a set.

# materials: outline, shadow, base, light, highlight
$mat = @{
  brass = @('#431600', '#692c00', '#b35500', '#d99a3e', '#f3c870')
  navy  = @('#0b1224', '#16254a', '#26407a', '#3a58b8', '#6a8ce8')
  glass = @('#2b3340', '#4a4e5a', '#8fb0a8', '#b8dfd8', '#d6f5ee')   # a lens that is off
  lamp  = @('#c64c00', '#ff8900', '#ffd900', '#ffff2b', '#fff9f2')   # a lens that is on
}

function Get-IconShape([bool]$on) {
  # The game draws the picture three pixels higher than its cell in the toolbar to leave room for the charge bar, the way it does
  # for the watering can. The watering can's own picture fills rows 4 to 14 of its cell and all 16 columns, so this one does too:
  # a flashlight seen from the side, pointing right, in a block 14 wide and 11 tall (rows 4 to 14), with room in the last two
  # columns for the rays of the lit version. The shapes are set out by hand on the pixel grid, because a computed ellipse this
  # small comes out lumpy.
  $grid = New-Object 'object[,]' 16, 16   # each cell: @{m=material; t=-1..1 across the piece} or $null
  # which rows of the head (0 to 10) are filled in each column of it
  $headRows = @{ 7 = @(2, 8); 8 = @(1, 9); 9 = @(0, 10); 10 = @(0, 10); 11 = @(0, 10); 12 = @(0, 10); 13 = @(1, 9) }
  for ($x = 0; $x -lt 14; $x++) { for ($r = 0; $r -lt 11; $r++) {
    $y = $r + 4
    $hit = $null
    if ($x -le 6 -and $r -ge 2 -and $r -le 8) { $hit = @{ m = 'navy'; t = ($r - 5) / 3.5 } }       # the body
    if (($x -eq 2 -or $x -eq 5) -and $r -ge 2 -and $r -le 8) { $hit = @{ m = 'navy'; t = ($r - 5) / 3.5; groove = $true } }   # two grip grooves
    if (($x -eq 1 -or $x -eq 3) -and $r -eq 3) { $hit = @{ m = 'navy'; t = -1.0 } }                # a shine on the upper left of the body
    if ($x -eq 0 -and $r -ge 2 -and $r -le 8) { $hit = @{ m = 'brass'; t = ($r - 5) / 3.5 } }     # the end cap
    if ($headRows.ContainsKey($x) -and $r -ge $headRows[$x][0] -and $r -le $headRows[$x][1]) { $hit = @{ m = 'brass'; t = ($r - 5) / 5.0 } }  # the head
    if (($x -eq 3 -or $x -eq 4) -and $r -eq 1) { $hit = @{ m = 'brass'; t = -0.6 } }               # the switch
    # the lens: a tall oval on the front of the head, with a rim of brass round it
    $lx = ($x + 0.5 - 10.6) / 1.75; $lr = ($r + 0.5 - 5.5) / 3.6
    if ($lx * $lx + $lr * $lr -le 1.0) { $hit = @{ m = $(if ($on) { 'lamp' } else { 'glass' }); t = ($r - 5.5) / 3.6; lens = $true } }
    $grid[$x, $y] = $hit
  } }
  return , $grid
}

function Mix-Colors([string]$a, [string]$b) {
  $ca = C $a; $cb = C $b
  [System.Drawing.Color]::FromArgb([int](([int]$ca.R + $cb.R) / 2), [int](([int]$ca.G + $cb.G) / 2), [int](([int]$ca.B + $cb.B) / 2))
}

function Convert-ShapeToBitmap($grid) {
  $bmp = New-Object System.Drawing.Bitmap 16, 16
  for ($y = 0; $y -lt 16; $y++) { for ($x = 0; $x -lt 16; $x++) {
    $cell = $grid[$x, $y]; if ($null -eq $cell) { continue }
    $edge = $false
    foreach ($d in @(@(1, 0), @(-1, 0), @(0, 1), @(0, -1))) {
      $nx = $x + $d[0]; $ny = $y + $d[1]
      if ($nx -lt 0 -or $ny -lt 0 -or $nx -gt 15 -or $ny -gt 15 -or $null -eq $grid[$nx, $ny]) { $edge = $true }
    }
    $ramp = $mat[$cell.m]
    if ($edge) {
      # the game's own icons outline the top and left a little lighter than the bottom and right, as if lit from the top left
      $lightSide = ($x -gt 0 -and $null -eq $grid[($x - 1), $y]) -or ($y -gt 0 -and $null -eq $grid[$x, ($y - 1)])
      $bmp.SetPixel($x, $y, $(if ($lightSide) { Mix-Colors $ramp[0] $ramp[1] } else { C $ramp[0] }))
      continue
    }
    $t = $cell.t
    if ($cell.lens) { $tone = if ($t -lt -0.4) { 4 } elseif ($t -lt 0.1) { 3 } elseif ($t -lt 0.6) { 2 } else { 1 } }
    else { $tone = if ($t -lt -0.55) { 4 } elseif ($t -lt -0.1) { 3 } elseif ($t -lt 0.45) { 2 } else { 1 } }
    if ($cell.groove -and $tone -gt 1) { $tone-- }
    $bmp.SetPixel($x, $y, (C $ramp[$tone]))
  } }
  return $bmp
}

function Set-Pixels($bmp, $list, [string]$hex) { foreach ($p in $list) { $bmp.SetPixel($p[0], $p[1], (C $hex)) } }

function New-FlashlightIcon([bool]$on) {
  $bmp = Convert-ShapeToBitmap (Get-IconShape $on)
  if ($on) {
    Set-Pixels $bmp @(, @(10, 7)) '#fff9f2'                                   # the hot centre of the lens
    Set-Pixels $bmp @(@(14, 9), @(14, 6), @(14, 12)) '#ffff2b'                  # rays leaving the lens
    Set-Pixels $bmp @(@(15, 9), @(15, 5), @(15, 13)) '#ffd900'
  } else {
    Set-Pixels $bmp @(, @(10, 7)) '#ffffff'                                  # a glint on the glass
  }
  return $bmp
}

function New-BatteryIcon {
  $rows = @(
    '................',
    '......OOOO......',
    '......OecO......',
    '....OeccbbdO....',
    '....OcbbbddO....',
    '....12344451....',
    '....12344451....',
    '....12344451....',
    '....12344451....',
    '....12344451....',
    '....12344451....',
    '....12344451....',
    '....12344451....',
    '....OcbbbddO....',
    '....OOOOOOOO....',
    '................'
  )
  $map = @{
    'O' = '#431600'; 'e' = '#f3c870'; 'c' = '#d99a3e'; 'b' = '#b35500'; 'd' = '#692c00'
    '1' = '#0b1224'; '2' = '#6a8ce8'; '3' = '#3a58b8'; '4' = '#26407a'; '5' = '#16254a'
  }
  $bmp = New-Object System.Drawing.Bitmap 16, 16
  for ($y = 0; $y -lt 16; $y++) { for ($x = 0; $x -lt 16; $x++) {
    $ch = [string]$rows[$y][$x]; if ($ch -eq '.') { continue }
    $bmp.SetPixel($x, $y, (C $map[$ch]))
  } }
  # a yellow bolt on the navy wrap
  Set-Pixels $bmp @(@(9, 6), @(8, 6), @(8, 7), @(7, 7), @(7, 8), @(8, 8), @(9, 8), @(8, 9), @(9, 9), @(7, 10), @(8, 10), @(7, 11)) '#ffd23a'
  return $bmp
}

$off = New-FlashlightIcon $false
$onBmp = New-FlashlightIcon $true
$bat = New-BatteryIcon

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
"icons done"
