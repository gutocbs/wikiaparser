# Compara duas pastas de saída do parser arquivo a arquivo (SHA-256).
# Uso: pwsh tools/compare-output.ps1 -Expected <pasta-baseline> -Actual <pasta-nova>
# Exit code 0 = idênticas; 1 = diferenças encontradas.
param(
    [Parameter(Mandatory = $true)][string]$Expected,
    [Parameter(Mandatory = $true)][string]$Actual
)

$expectedFiles = Get-ChildItem -Path $Expected -File | Sort-Object Name
$actualFiles = Get-ChildItem -Path $Actual -File | Sort-Object Name
$differences = 0

$missing = Compare-Object ($expectedFiles.Name) ($actualFiles.Name)
foreach ($m in $missing) {
    $side = if ($m.SideIndicator -eq '<=') { 'only in expected' } else { 'only in actual' }
    Write-Host "DIFF  $($m.InputObject) ($side)"
    $differences++
}

foreach ($file in $expectedFiles) {
    $other = Join-Path $Actual $file.Name
    if (-not (Test-Path $other)) { continue }
    $h1 = (Get-FileHash $file.FullName -Algorithm SHA256).Hash
    $h2 = (Get-FileHash $other -Algorithm SHA256).Hash
    if ($h1 -eq $h2) {
        Write-Host "OK    $($file.Name)"
    }
    else {
        $differences++
        $a = [System.IO.File]::ReadAllBytes($file.FullName)
        $b = [System.IO.File]::ReadAllBytes($other)
        $len = [Math]::Min($a.Length, $b.Length)
        $offset = 0
        while ($offset -lt $len -and $a[$offset] -eq $b[$offset]) { $offset++ }
        $start = [Math]::Max(0, $offset - 80)
        $ctxA = [System.Text.Encoding]::UTF8.GetString($a, $start, [Math]::Min(200, $a.Length - $start))
        $ctxB = [System.Text.Encoding]::UTF8.GetString($b, $start, [Math]::Min(200, $b.Length - $start))
        Write-Host "DIFF  $($file.Name) (sizes $($a.Length) vs $($b.Length), first difference at byte $offset)"
        Write-Host "      expected: $ctxA"
        Write-Host "      actual:   $ctxB"
    }
}

if ($differences -eq 0) { Write-Host "IDENTICAL ($($expectedFiles.Count) files)"; exit 0 }
Write-Host "$differences difference(s)"
exit 1
