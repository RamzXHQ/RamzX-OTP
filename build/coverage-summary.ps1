# Prints a per-class line coverage table (Markdown) from a Cobertura report.
# Usage: pwsh build/coverage-summary.ps1 <folder with coverage.cobertura.xml>
param([Parameter(Mandatory = $true)][string]$ResultsDir)

$file = Get-ChildItem $ResultsDir -Recurse -Filter coverage.cobertura.xml |
    Sort-Object LastWriteTime | Select-Object -Last 1
if (-not $file) { throw "No coverage.cobertura.xml found in $ResultsDir" }

# Classes that need the KeePass UI or the screen cannot run on a CI runner.
$uiClasses = 'AddOtpForm', 'Ui', 'ScreenScanner'

$xml = [xml](Get-Content $file.FullName)
$rows = $xml.coverage.packages.package.classes.class |
    Group-Object { (($_.name -split '[+/<]')[0]) -replace '^RamzXOtp\.', '' } |
    ForEach-Object {
        $lines = @($_.Group | ForEach-Object { $_.lines.line })
        $hit = @($lines | Where-Object { [int]$_.hits -gt 0 }).Count
        [pscustomobject]@{ Class = $_.Name; Hit = $hit; Total = $lines.Count; Ui = $uiClasses -contains $_.Name }
    } | Sort-Object Ui, Class

$core = @($rows | Where-Object { -not $_.Ui })
$coreHit = ($core | Measure-Object Hit -Sum).Sum
$coreTotal = ($core | Measure-Object Total -Sum).Sum

"## Test coverage"
""
"**Core logic: {0} / {1} lines ({2:P1})**" -f $coreHit, $coreTotal, ($coreHit / [math]::Max(1, $coreTotal))
""
"| Class | Lines covered | Coverage | Note |"
"|---|---:|---:|---|"
foreach ($r in $rows) {
    $note = if ($r.Ui) { 'needs KeePass UI / screen - tested manually' } else { '' }
    "| {0} | {1} / {2} | {3:P0} | {4} |" -f $r.Class, $r.Hit, $r.Total, ($r.Hit / [math]::Max(1, $r.Total)), $note
}
