# Draws the 10 by 10 sunglasses picture that sits beside the "+1 Coolness" line of the flashlight's tooltip.
# The game draws its own stat icons (defense, speed, luck) at 10 by 10 and scales them by four, so this one matches.
# Run: powershell -File art/Generate-Coolness.ps1
Add-Type -AssemblyName System.Drawing

$palette = New-Object System.Collections.Hashtable ([StringComparer]::Ordinal)
$palette['O'] = '#0b1224'; $palette['1'] = '#16254a'; $palette['w'] = '#ffffff'   # the same navy as the flashlight body, and a glint
$palette['h'] = '#f3c870'; $palette['l'] = '#d99a3e'                             # the same brass as the flashlight head, for a sparkle

$rows = @(
  '........l.',
  '.......hlh',
  'OOOOOOOOOO',
  'O1w1OO1w1O',
  'O111OO111O',
  '.O1O..O1O.',
  '..O....O..',
  '..........',
  '..........',
  '..........'
)

$bitmap = New-Object System.Drawing.Bitmap 10, 10, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
for ($y = 0; $y -lt 10; $y++) {
    if ($rows[$y].Length -ne 10) { throw "Row $y is $($rows[$y].Length) wide, not 10." }
    for ($x = 0; $x -lt 10; $x++) {
        $key = [string]$rows[$y][$x]
        if ($key -eq '.') { $bitmap.SetPixel($x, $y, [System.Drawing.Color]::Transparent) }
        else { $bitmap.SetPixel($x, $y, [System.Drawing.ColorTranslator]::FromHtml($palette[$key])) }
    }
}
$out = Join-Path (Split-Path -Parent $PSScriptRoot) 'Flashlight\assets\coolness.png'
$bitmap.Save($out, [System.Drawing.Imaging.ImageFormat]::Png)
$bitmap.Dispose()
Write-Host "Wrote $out"
