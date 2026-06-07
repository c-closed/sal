param([string]$Version = "3.4.0.6")
$ErrorActionPreference = "Stop"
$scriptDir = if ($PSCmdlet) { Split-Path -Parent $PSCmdlet.Path } else { Get-Location }
$projDir = Resolve-Path (Join-Path $scriptDir "..")

# Parse 4-part version: 3.4.0.4 -> major=3, minor=4, patch=0, build=4
$vParts = $Version.Split('.')
if ($vParts.Length -ne 4) { Write-Error "Version must be 4-part (e.g. 3.4.0.4)"; exit 1 }
$major = $vParts[0]; $minor = $vParts[1]; $patch = $vParts[2]; $build = $vParts[3]

# Find publish dir
$boardDir = Get-ChildItem -Path $projDir -Filter "Sboard*" -Directory | Where-Object { $_.Name -notlike "*.Services" } | Select-Object -First 1
if (-not $boardDir) { Write-Error "Sboard directory not found!"; exit 1 }
$publishDir = Join-Path $boardDir.FullName "bin/Release/net8.0-windows/publish"
if (-not (Test-Path $publishDir)) { Write-Error "Publish dir not found!"; exit 1 }

$productName = $boardDir.Name

$tempDir = Join-Path $env:TEMP "sal_msi_build"
if (Test-Path $tempDir) { Remove-Item $tempDir -Recurse -Force }
New-Item $tempDir -ItemType Directory -Force | Out-Null

Write-Host "=== Step 1: Update Setup project version ==="
$vdprojPath = Resolve-Path (Join-Path $scriptDir "Setup.vdproj")
(Get-Content $vdprojPath) -replace '"ProductVersion" = "8:[^"]*"', '"ProductVersion" = "8:$Version"' | Set-Content $vdprojPath -Encoding ASCII
Write-Host "ProductVersion set to $Version"

Write-Host "=== Step 2: Rebuild VS Setup project ==="
$devenv = "C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\devenv.com"
$slnPath = Resolve-Path (Join-Path $projDir "sal.sln")
$buildLog = Join-Path $tempDir "vs_build.log"
$proc = Start-Process -FilePath $devenv -ArgumentList "`"$slnPath`" /Build Release /Out:`"$buildLog`"" -Wait -PassThru -NoNewWindow
if ($proc.ExitCode -ne 0) {
    Write-Error "VS build failed (exit $(.ExitCode))"
    Get-Content $buildLog | Select-String -Pattern "error|Error|ERROR|fail" | ForEach-Object { Write-Host  }
    exit 1
}

$outputMsi = Resolve-Path (Join-Path $scriptDir "Release\Setup.msi")
if (-not (Test-Path $outputMsi)) { Write-Error "MSI not found"; exit 1 }
Write-Host "MSI: $outputMsi (0 MB)"

Write-Host "=== Step 3: Post-process MSI via VBS ==="
$vbsScriptPath = Resolve-Path (Join-Path $scriptDir "post-process.vbs")
$productCode = [guid]::NewGuid().ToString("B").ToUpper()
$upgradeCode = "{B2E7A57D-3B2A-4F8C-9E1D-5A2F7C8B9E1A}"
$title = "$productName v$Version"
$escProductName =  -replace '"', '""'

$vbsResult = & cscript.exe //nologo "$vbsScriptPath" "$productCode" "$upgradeCode" "$title" "$escProductName" 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Error "VBScript failed: "
    exit 1
}
Write-Host "VBS post-processing completed"

Write-Host "=== Step 4: Done ==="
Write-Host "MSI: $outputMsi (0 MB)"

Remove-Item $tempDir -Recurse -Force -ErrorAction SilentlyContinue
Write-Host "Done!"