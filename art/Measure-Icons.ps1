# Prints the rows and columns each icon uses. The flashlight must stay inside rows 3 to 13, because the game draws it
# three pixels higher than its cell to leave room for the charge bar.
Add-Type -AssemblyName System.Drawing
$assets = Join-Path (Join-Path (Split-Path -Parent $PSScriptRoot) 'Flashlight') 'assets'
foreach ($sheet in @(@('tools.png', 2), @('objects.png', 1))) {
  $bmp = [System.Drawing.Bitmap]::FromFile((Join-Path $assets $sheet[0]))
  for ($c = 0; $c -lt $sheet[1]; $c++) {
    $minY = 99; $maxY = -1; $minX = 99; $maxX = -1
    for ($y = 0; $y -lt 16; $y++) { for ($x = 0; $x -lt 16; $x++) {
      if ($bmp.GetPixel($c * 16 + $x, $y).A -gt 0) {
        if ($y -lt $minY) { $minY = $y }; if ($y -gt $maxY) { $maxY = $y }
        if ($x -lt $minX) { $minX = $x }; if ($x -gt $maxX) { $maxX = $x }
      }
    } }
    "{0} cell {1}: rows {2} to {3}, columns {4} to {5}" -f $sheet[0], $c, $minY, $maxY, $minX, $maxX
  }
  $bmp.Dispose()
}
