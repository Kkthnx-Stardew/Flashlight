# Draws the item icons: the flashlight (off and on) and the Dry Cell battery. Dot-sourced by Generate-Art.ps1.
#
# Style rules, measured pixel by pixel from the game's own 16x16 icons (Torch, Bomb, Joja Cola, Copper Bar, Watering Can, Glow Ring):
#  - each material has one dark outline colour of its own, never plain black, a touch lighter on the top and left
#  - three or four fill tones, light from the top left, with single stray pixels of the next tone instead of smooth bands
#  - a bright highlight that is only one or two pixels
#  - silhouettes with the corners cut, never a perfect rectangle or oval
#
# The look follows the reference pictures: a brass head with a glass lens and a dark navy body with a small switch,
# and a D-cell battery in the same navy and brass so the two items read as a set.

# one letter per colour; the letters are case sensitive, so the table compares them exactly
$palette = New-Object System.Collections.Hashtable ([StringComparer]::Ordinal)
# brass: Q dark outline, q lighter outline and shadow, m base, l light, h highlight
$palette['Q'] = '#431600'; $palette['q'] = '#692c00'; $palette['m'] = '#b35500'; $palette['l'] = '#d99a3e'; $palette['h'] = '#f3c870'
# navy: O dark outline, P lighter outline, 1 shadow, 2 base, 3 light, 4 highlight
$palette['O'] = '#0b1224'; $palette['P'] = '#16254a'; $palette['1'] = '#16254a'; $palette['2'] = '#26407a'; $palette['3'] = '#3a58b8'; $palette['4'] = '#6a8ce8'
# a lens that is off: x shadow, y base, z light, w glint
$palette['x'] = '#4a6f78'; $palette['y'] = '#8fb0a8'; $palette['z'] = '#b8dfd8'; $palette['w'] = '#ffffff'
# a lens that is on: d edge, e glow, f bright, g hot centre
$palette['d'] = '#ff8900'; $palette['e'] = '#ffd900'; $palette['f'] = '#ffff2b'; $palette['g'] = '#fff9f2'

# The flashlight seen from the side, pointing right, in rows 5 to 13 and columns 0 to 13. The game draws the picture twelve pixels
# higher in the toolbar to leave room for the charge bar, the way it does for the watering can, whose own picture also spans
# rows 4 to 14. The last two columns are kept free for the rays of the lit version.
$flashlightRows = @(
  '................',
  '................',
  '................',
  '................',
  '................',
  '...qq....qqqq...',
  '..qhlQ..qlhllQ..',
  '.qPPPPPqlhzlmQ..',
  'qh43313qlwzymQ..',
  'ql32212qmyyxmQ..',
  'Qm22111QmyxxmQ..',
  '.QOOOOOQmmxqmQ..',
  '........QmmqqQ..',
  '.........QQQQ...',
  '................',
  '................'
)

# The Dry Cell: a navy wrap with brass ends and a small nub, eight pixels wide like the game's Joja Cola bottle.
$batteryRows = @(
  '................',
  '................',
  '.......qq.......',
  '......qhlQ......',
  '....qhllllmQ....',
  '....qmmmmmmQ....',
  '....P342221O....',
  '....P342221O....',
  '....P322221O....',
  '....P322221O....',
  '....P342221O....',
  '....P322221O....',
  '....P322221O....',
  '....qlmmmmqQ....',
  '.....QQQQQQ.....',
  '................'
)

function Convert-RowsToBitmap([string[]]$rows) {
  $bmp = New-Object System.Drawing.Bitmap 16, 16
  for ($y = 0; $y -lt 16; $y++) { for ($x = 0; $x -lt 16; $x++) {
    $ch = [string]$rows[$y][$x]; if ($ch -eq '.') { continue }
    $bmp.SetPixel($x, $y, (C $palette[$ch]))
  } }
  return $bmp
}

function Set-Pixels($bmp, $list, [string]$hex) { foreach ($p in $list) { $bmp.SetPixel($p[0], $p[1], (C $hex)) } }

function New-FlashlightIcon([bool]$on) {
  $rows = $flashlightRows
  if ($on) { $rows = $rows | ForEach-Object { $_.Replace('w', 'g').Replace('z', 'f').Replace('y', 'e').Replace('x', 'd') } }
  $bmp = Convert-RowsToBitmap $rows
  if ($on) {
    Set-Pixels $bmp @(@(14, 9), @(14, 6), @(14, 12)) '#ffff2b'                  # rays leaving the lens
    Set-Pixels $bmp @(@(15, 9), @(15, 5), @(15, 13)) '#ffd900'
  }
  return $bmp
}

function New-BatteryIcon {
  $bmp = Convert-RowsToBitmap $batteryRows
  # a yellow bolt on the navy wrap
  Set-Pixels $bmp @(@(9, 7), @(8, 7), @(8, 8), @(7, 8), @(7, 9), @(8, 9), @(9, 9), @(8, 10), @(9, 10), @(7, 11), @(8, 11)) '#ffd900'
  Set-Pixels $bmp @(@(9, 7), @(8, 7)) '#ffff2b'
  Set-Pixels $bmp @(, @(7, 11)) '#ff8900'
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
