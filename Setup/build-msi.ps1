param([string]$Version = "3.4.0.6")
$ErrorActionPreference = "Stop"
$scriptDir = if ($PSCommandPath) { Split-Path -Parent $PSCommandPath } else { Get-Location }
$projDir = Resolve-Path (Join-Path $scriptDir "..")

# Parse 4-part version: 3.4.0.4 -> major=3, minor=4, patch=0, build=4
$vParts = $Version.Split('.')
if ($vParts.Length -ne 4) { Write-Error "Version must be 4-part (e.g. 3.4.0.4)"; exit 1 }
$major = $vParts[0]; $minor = $vParts[1]; $build = $vParts[3]

# Find publish dir
$publishDir = $null
foreach ($c in (Get-ChildItem $projDir -Directory)) {
    $t = Join-Path $c.FullName "bin/Release/net8.0-windows/publish"
    if (Test-Path $t) { $publishDir = $t; break }
}
if (-not $publishDir) { Write-Error "Publish dir not found!"; exit 1 }
$publishDir = [System.IO.Path]::GetFullPath($publishDir)

$pd = $publishDir
for ($i = 0; $i -lt 4; $i++) { $pd = [System.IO.Path]::GetDirectoryName($pd) }
$productName = [System.IO.Path]::GetFileName($pd)

$tempDir = Join-Path $env:TEMP "sal_msi_build"
if (Test-Path $tempDir) { Remove-Item $tempDir -Recurse -Force }
New-Item $tempDir -ItemType Directory -Force | Out-Null

Write-Host "=== Step 1: Update Setup project version ==="
$vdprojPath = Resolve-Path (Join-Path $scriptDir "Setup.vdproj")
$version3 = "$major.$minor.$build"
(Get-Content $vdprojPath) -replace '"ProductVersion" = "8:[^"]*"', "`"ProductVersion`" = `"8:$version3`"" | Set-Content $vdprojPath -Encoding ASCII
Write-Host "ProductVersion set to $version3"

Write-Host "=== Step 2: Rebuild VS Setup project ==="
$devenv = "C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\devenv.com"
$slnPath = Resolve-Path (Join-Path $projDir "sal.sln")
$buildLog = Join-Path $tempDir "vs_build.log"
$proc = Start-Process -FilePath $devenv -ArgumentList "`"$slnPath`" /Build Release /Out `"$buildLog`"" -Wait -PassThru -NoNewWindow
if ($proc.ExitCode -ne 0) {
    Write-Error "VS build failed (exit $($proc.ExitCode))"
    Get-Content $buildLog | Select-String -Pattern "error|Error|ERROR|fail" | ForEach-Object { Write-Host $_ }
    exit 1
}

$outputMsi = Resolve-Path (Join-Path $scriptDir "Release\Setup.msi")
if (-not (Test-Path $outputMsi)) { Write-Error "MSI not found"; exit 1 }
Write-Host "MSI: $outputMsi ($([math]::Round((Get-Item $outputMsi).Length/1MB,2)) MB)"

Write-Host "=== Step 3: Post-process MSI via VBS ==="
$vbsScriptPath = Resolve-Path (Join-Path $scriptDir "post-process.vbs")
$vbsResult = & cscript.exe //nologo "`"$vbsScriptPath`"" "`"$outputMsi`"" 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "VBS warning (non-fatal): $vbsResult"
}

Write-Host "=== Step 4: Update dynamic MSI properties ==="
$productCode = [guid]::NewGuid().ToString("B").ToUpper()
$upgradeCode = "{B2E7A57D-3B2A-4F8C-9E1D-5A2F7C8B9E1A}"
$title = "$productName v$Version"
$escTitle = $title -replace '"', '""'
$escProductName = $productName -replace '"', '""'

try {
    $installer = New-Object -ComObject WindowsInstaller.Installer
    $db = $installer.GetType().InvokeMember("OpenDatabase", [System.Reflection.BindingFlags]::InvokeMethod, $null, $installer, @($outputMsi, 1))
    
    $view = $db.GetType().InvokeMember("OpenView", [System.Reflection.BindingFlags]::InvokeMethod, $null, $db, @("DELETE FROM `Upgrade`"))
    $view.GetType().InvokeMember("Execute", [System.Reflection.BindingFlags]::InvokeMethod, $null, $view, $null)
    
    $view = $db.GetType().InvokeMember("OpenView", [System.Reflection.BindingFlags]::InvokeMethod, $null, $db, @("INSERT INTO `Upgrade` (`UpgradeCode`,`VersionMin`,`VersionMax`,`Language`,`Attributes`,`Remove`,`ActionProperty`) VALUES ('$upgradeCode','','','',768,'','PREVIOUSVERSIONSINSTALLED')"))
    $view.GetType().InvokeMember("Execute", [System.Reflection.BindingFlags]::InvokeMethod, $null, $view, $null)
    
    $updates = @(
        "UPDATE `Property` SET `Value`='$productCode' WHERE `Property`='ProductCode'",
        "UPDATE `Property` SET `Value`='$Version' WHERE `Property`='ProductVersion'",
        "UPDATE `Property` SET `Value`='$escProductName' WHERE `Property`='ProductName'",
        "UPDATE `Property` SET `Value`='Sboard' WHERE `Property`='Manufacturer'",
        "UPDATE `Property` SET `Value`='$upgradeCode' WHERE `Property`='UpgradeCode'"
    )
    foreach ($sql in $updates) {
        $view = $db.GetType().InvokeMember("OpenView", [System.Reflection.BindingFlags]::InvokeMethod, $null, $db, @($sql))
        $view.GetType().InvokeMember("Execute", [System.Reflection.BindingFlags]::InvokeMethod, $null, $view, $null)
    }
    
    $db.GetType().InvokeMember("Commit", [System.Reflection.BindingFlags]::InvokeMethod, $null, $db, $null)
    
    # Update SummaryInfo
    $sum = $installer.GetType().InvokeMember("SummaryInformation", [System.Reflection.BindingFlags]::InvokeMethod, $null, $installer, @($outputMsi, 1))
    $sum.GetType().InvokeMember("Property", [System.Reflection.BindingFlags]::SetProperty, $null, $sum, @(1, $escTitle))
    $sum.GetType().InvokeMember("Property", [System.Reflection.BindingFlags]::SetProperty, $null, $sum, @(2, $escTitle))
    $sum.GetType().InvokeMember("Property", [System.Reflection.BindingFlags]::SetProperty, $null, $sum, @(4, "Sboard"))
    $sum.GetType().InvokeMember("Persist", [System.Reflection.BindingFlags]::InvokeMethod, $null, $sum, $null)
    
    Write-Host "MSI properties updated. ProductCode=$productCode"
} catch {
    Write-Error "MSI update failed: $_"
    exit 1
}

Write-Host "=== Step 5: Done ==="
Write-Host "MSI: $outputMsi ($([math]::Round((Get-Item $outputMsi).Length/1MB,2)) MB)"

Remove-Item $tempDir -Recurse -Force -ErrorAction SilentlyContinue
Write-Host "Done!"