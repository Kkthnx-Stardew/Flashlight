# Draws the small flashlight the farmer holds in the hand while it is the selected tool. Dot-sourced by Generate-Art.ps1
# after Generate-Icons.ps1, which defines the colours ($mat) and the helpers.
#
# held.png is a row of 12 by 12 cells, three views of the flashlight and for each an unlit and a lit one:
#   cell 0, 1: from the side, pointing right (it is flipped for left)
#   cell 2, 3: from the front, the lens facing the viewer (for a farmer facing down)
#   cell 4, 5: from the back, the end cap facing the viewer (for a farmer facing up)

function New-HeldGrid([string]$view, [bool]$on) {
  $size = 12
  $grid = New-Object 'object[,]' $size, $size
  $lensMat = $(if ($on) { 'lamp' } else { 'glass' })
  for ($y = 0; $y -lt $size; $y++) { for ($x = 0; $x -lt $size; $x++) {
    $hit = $null
    switch ($view) {
      'side' {
        if ($x -ge 2 -and $x -le 7 -and $y -ge 4 -and $y -le 7) { $hit = @{ m = 'navy'; t = ($y - 5.5) / 2.0 } }
        if ($x -le 1 -and $y -ge 4 -and $y -le 7) { $hit = @{ m = 'brass'; t = ($y - 5.5) / 2.0 } }
        if ($x -ge 8 -and $x -le 10 -and $y -ge 3 -and $y -le 8) { $hit = @{ m = 'brass'; t = ($y - 5.5) / 3.0 } }
        if ($x -eq 10 -and $y -ge 4 -and $y -le 7) { $hit = @{ m = $lensMat; t = ($y - 5.5) / 2.0; lens = $true } }
        if ($x -eq 4 -and $y -eq 3) { $hit = @{ m = 'brass'; t = -0.5 } }   # the switch
      }
      'front' {
        $dx = $x + 0.5 - 6.0; $dy = $y + 0.5 - 6.0; $d = [Math]::Sqrt($dx * $dx + $dy * $dy)
        if ($d -le 4.0) { $hit = @{ m = 'brass'; t = $dy / 4.0 } }
        if ($d -le 2.4) { $hit = @{ m = $lensMat; t = $dy / 2.4; lens = $true } }
      }
      'back' {
        $dx = $x + 0.5 - 6.0; $dy = $y + 0.5 - 6.0; $d = [Math]::Sqrt($dx * $dx + $dy * $dy)
        if ($d -le 4.3) { $hit = @{ m = 'navy'; t = $dy / 4.3 } }
        if ($d -le 2.1) { $hit = @{ m = 'brass'; t = $dy / 2.1 } }
      }
    }
    $grid[$x, $y] = $hit
  } }
  return , $grid
}

function Convert-HeldGridToBitmap($grid) {
  $size = 12
  $bmp = New-Object System.Drawing.Bitmap $size, $size
  for ($y = 0; $y -lt $size; $y++) { for ($x = 0; $x -lt $size; $x++) {
    $cell = $grid[$x, $y]; if ($null -eq $cell) { continue }
    $edge = $false
    foreach ($d in @(@(1, 0), @(-1, 0), @(0, 1), @(0, -1))) {
      $nx = $x + $d[0]; $ny = $y + $d[1]
      if ($nx -lt 0 -or $ny -lt 0 -or $nx -ge $size -or $ny -ge $size -or $null -eq $grid[$nx, $ny]) { $edge = $true }
    }
    $ramp = $mat[$cell.m]
    if ($edge) { $bmp.SetPixel($x, $y, (C $ramp[0])); continue }
    $t = $cell.t
    if ($cell.lens) { $tone = if ($t -lt -0.3) { 4 } elseif ($t -lt 0.2) { 3 } elseif ($t -lt 0.6) { 2 } else { 1 } }
    else { $tone = if ($t -lt -0.5) { 4 } elseif ($t -lt -0.1) { 3 } elseif ($t -lt 0.5) { 2 } else { 1 } }
    $bmp.SetPixel($x, $y, (C $ramp[$tone]))
  } }
  return $bmp
}

$heldViews = 'side', 'front', 'back'
$heldSheet = New-Object System.Drawing.Bitmap (12 * 6), 12
$hg = [System.Drawing.Graphics]::FromImage($heldSheet)
$heldCells = @()
for ($v = 0; $v -lt 3; $v++) {
  foreach ($on in @($false, $true)) {
    $cellBmp = Convert-HeldGridToBitmap (New-HeldGrid $heldViews[$v] $on)
    if ($heldViews[$v] -eq 'front') { $cellBmp.SetPixel($(if ($on) { 5 } else { 5 }), 4, (C '#ffffff')) }   # a glint on the lens
    $hg.DrawImageUnscaled($cellBmp, ((($v * 2) + [int]$on) * 12), 0)
    $heldCells += $cellBmp
  }
}
$hg.Dispose()
$heldSheet.Save((Join-Path $assets 'held.png'))

# an enlarged preview on a tan background, for looking at
$hs = 14
$hp = New-Object System.Drawing.Bitmap (6 * (12 * $hs + 8) + 8), (12 * $hs + 16)
$hpg = [System.Drawing.Graphics]::FromImage($hp)
$hpg.Clear((C '#d9c08a')); $hpg.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor; $hpg.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
for ($i = 0; $i -lt 6; $i++) { $hpg.DrawImage($heldCells[$i], (8 + $i * (12 * $hs + 8)), 8, (12 * $hs), (12 * $hs)) }
$hpg.Dispose(); $hp.Save((Join-Path $PSScriptRoot 'preview-held.png'))
"held done"
