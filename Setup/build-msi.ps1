param([string]$Version = "3.4.0.5")
$ErrorActionPreference = "Stop"
$scriptDir = if ($PSCommandPath) { Split-Path -Parent $PSCommandPath } else { Get-Location }
$projDir = Resolve-Path (Join-Path $scriptDir "..")

# Parse 4-part version: 3.4.0.4 -> major=3, minor=4, patch=0, build=4
$vParts = $Version.Split('.')
if ($vParts.Length -ne 4) { Write-Error "Version must be 4-part (e.g. 3.4.0.4)"; exit 1 }
$major = $vParts[0]; $minor = $vParts[1]; $build = $vParts[3]
$vdprojVersion = "$major.$minor.$build"

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
$vdprojPath = [System.IO.Path]::GetFullPath((Join-Path $scriptDir "Setup.vdproj"))
(Get-Content $vdprojPath) -replace '"ProductVersion" = "8:[^"]*"', "`"ProductVersion`" = `"8:$vdprojVersion`"" | Set-Content $vdprojPath -Encoding ASCII
Write-Host "ProductVersion set to $vdprojVersion"

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

$outputMsi = [System.IO.Path]::GetFullPath((Join-Path $scriptDir "Release\Setup.msi"))
if (-not (Test-Path $outputMsi)) { Write-Error "MSI not found"; exit 1 }
Write-Host "MSI: $outputMsi ($([math]::Round((Get-Item $outputMsi).Length/1MB,2)) MB)"

$productCode = [guid]::NewGuid().ToString("B").ToUpper()
$upgradeCode = "{B2E7A57D-3B2A-4F8C-9E1D-5A2F7C8B9E1A}"
$title = "$productName v$Version"
$escTitle = $title -replace '"', '""'
$escProductName = $productName -replace '"', '""'

Write-Host "=== Step 3: Post-process MSI ==="
$vbsPath = Join-Path $tempDir "build.vbs"

$vbsContent = @"
Dim installer, db
Set installer = CreateObject("WindowsInstaller.Installer")
Set db = installer.OpenDatabase("$outputMsi", 1)

' Delete MSVBDPCADLL references
db.OpenView("DELETE FROM `Binary` WHERE `Name`='MSVBDPCADLL'").Execute
db.OpenView("DELETE FROM `CustomAction` WHERE `Source`='MSVBDPCADLL'").Execute
db.OpenView("DELETE FROM `InstallUISequence` WHERE `Action`='DIRCA_CheckNETCore' OR `Action`='ERRCA_UIANDADVERTISED' OR `Action`='VSDCA_VsdLaunchConditions'").Execute

' Delete old Upgrade and insert new one
db.OpenView("DELETE FROM `Upgrade`").Execute
db.OpenView("INSERT INTO `Upgrade` (`UpgradeCode`,`VersionMin`,`VersionMax`,`Language`,`Attributes`,`Remove`,`ActionProperty`) VALUES ('$upgradeCode','','','',768,'','PREVIOUSVERSIONSINSTALLED')").Execute

' Update Properties
db.OpenView("UPDATE `Property` SET `Value`='$productCode' WHERE `Property`='ProductCode'").Execute
db.OpenView("UPDATE `Property` SET `Value`='$Version' WHERE `Property`='ProductVersion'").Execute
db.OpenView("UPDATE `Property` SET `Value`='$escProductName' WHERE `Property`='ProductName'").Execute
db.OpenView("UPDATE `Property` SET `Value`='Sboard' WHERE `Property`='Manufacturer'").Execute
db.OpenView("UPDATE `Property` SET `Value`='$upgradeCode' WHERE `Property`='UpgradeCode'").Execute

db.Commit
Set db = Nothing

' Update SummaryInfo
Dim installer2, sum
Set installer2 = CreateObject("WindowsInstaller.Installer")
On Error Resume Next
Set sum = installer2.SummaryInformation("$outputMsi", 1)
If Not Err Then
    sum.Property(1) = "$escTitle"
    sum.Property(2) = "$escTitle"
    sum.Property(4) = "Sboard"
    sum.Persist
End If
Set sum = Nothing
On Error Goto 0
Set installer2 = Nothing

WScript.Echo "OK"
"@

[System.IO.File]::WriteAllText($vbsPath, $vbsContent, [System.Text.Encoding]::Unicode)

$vbsResult = & cscript.exe //nologo $vbsPath 2>&1
Write-Host $vbsResult
if ($vbsResult -notcontains "OK") { Write-Error "VBScript failed"; exit 1 }

Write-Host "=== Step 4: Done ==="
Write-Host "MSI: $outputMsi ($([math]::Round((Get-Item $outputMsi).Length/1MB,2)) MB)"

Remove-Item $tempDir -Recurse -Force -ErrorAction SilentlyContinue
Write-Host "Done!"