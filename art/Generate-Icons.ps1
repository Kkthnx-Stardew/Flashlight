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
  # The picture is drawn three pixels higher than its cell in the toolbar to leave room for the charge bar, as the game does
  # for the watering can, so everything must sit in rows 3 to 13 of the 16 to stay clear of the bar and the top of the slot.
  $cx = 7.5; $cy = 8.25; $r2 = [Math]::Sqrt(2)
  $grid = New-Object 'object[,]' 16, 16   # each cell: @{m=material; t=-1..1 across the piece} or $null
  for ($y = 0; $y -lt 16; $y++) { for ($x = 0; $x -lt 16; $x++) {
    $dx = $x + 0.5 - $cx; $dy = $y + 0.5 - $cy
    $u = ($dx - $dy) / $r2    # along the flashlight, towards the top right
    $v = ($dx + $dy) / $r2    # across it, towards the bottom right
    $hit = $null
    # the body (navy), then the brass end cap
    if ($u -ge -5.4 -and $u -lt 1.4 -and [Math]::Abs($v) -le 2.0) { $hit = @{ m = 'navy'; t = $v / 2.0 } }
    if ($u -ge -6.3 -and $u -lt -5.4 -and [Math]::Abs($v) -le 2.0) { $hit = @{ m = 'brass'; t = $v / 2.0 } }
    # the neck that widens into the head
    if ($u -ge 1.4 -and $u -lt 2.7) { $hw = 2.1 + ($u - 1.4) * 0.7; if ([Math]::Abs($v) -le $hw) { $hit = @{ m = 'brass'; t = $v / $hw } } }
    # the head: a brass bezel, an ellipse seen at an angle
    $eu = ($u - 4.0) / 2.4; $ev = $v / 3.05
    if ($eu * $eu + $ev * $ev -le 1.0) { $hit = @{ m = 'brass'; t = $v / 3.05 } }
    # the lens, a smaller ellipse nearer the tip
    $lu = ($u - 4.6) / 1.45; $lv = $v / 2.2
    if ($lu * $lu + $lv * $lv -le 1.0) { $hit = @{ m = $(if ($on) { 'lamp' } else { 'glass' }); t = $v / 2.2; lens = $true } }
    # the switch: a small bump on the upper side of the body
    $su = $u + 2.2; $sv = $v + 2.3
    if ($su * $su + $sv * $sv -le 0.75) { $hit = @{ m = 'brass'; t = -0.5 } }
    $grid[$x, $y] = $hit
  } }
  return , $grid
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
    if ($edge) { $bmp.SetPixel($x, $y, (C $ramp[0])); continue }
    $t = $cell.t
    if ($cell.lens) { $tone = if ($t -lt -0.4) { 4 } elseif ($t -lt 0.1) { 3 } elseif ($t -lt 0.6) { 2 } else { 1 } }
    else { $tone = if ($t -lt -0.55) { 4 } elseif ($t -lt -0.1) { 3 } elseif ($t -lt 0.45) { 2 } else { 1 } }
    $bmp.SetPixel($x, $y, (C $ramp[$tone]))
  } }
  return $bmp
}

function Set-Pixels($bmp, $list, [string]$hex) { foreach ($p in $list) { $bmp.SetPixel($p[0], $p[1], (C $hex)) } }

function New-FlashlightIcon([bool]$on) {
  $bmp = Convert-ShapeToBitmap (Get-IconShape $on)
  if ($on) {
    Set-Pixels $bmp @(, @(9, 4)) '#fff9f2'                                   # the hot centre of the lens
    Set-Pixels $bmp @(@(14, 3), @(15, 5)) '#ffff2b'                           # rays leaving the lens, kept inside rows 3 to 13
    Set-Pixels $bmp @(@(15, 4), @(14, 6)) '#ffd900'
  } else {
    Set-Pixels $bmp @(, @(9, 4)) '#ffffff'                                  # a glint on the glass
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
