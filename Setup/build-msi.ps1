param([string]$Version = "3.4.0.4")
$ErrorActionPreference = "Stop"
$scriptDir = if ($PSCommandPath) { Split-Path -Parent $PSCommandPath } else { Get-Location }
$projDir = Resolve-Path (Join-Path $scriptDir "..")

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

$asciiTemp = Join-Path $env:TEMP "sal_msi_build"
if (Test-Path $asciiTemp) { Remove-Item $asciiTemp -Recurse -Force }
New-Item $asciiTemp -ItemType Directory -Force | Out-Null

Write-Host "=== Step 1: Create cabinet ==="
$seq = 1; $files = @()
Get-ChildItem $publishDir -Recurse -File | Sort-Object FullName | ForEach-Object {
    $ext = [System.IO.Path]::GetExtension($_.FullName)
    $r = $_.FullName.Substring($publishDir.Length + 1)
    $dirPart = Split-Path $r -Parent
    $asciiName = "f$seq$ext"
    $files += [PSCustomObject]@{Path=$_.FullName;Rel=$r;Id="F$seq";Seq=$seq;Size=$_.Length;AsciiName=$asciiName;DirPart=$dirPart}; $seq++
    if ($dirPart) {
        $td = [System.IO.Path]::GetFullPath((Join-Path $asciiTemp $dirPart))
        if (-not (Test-Path $td)) { New-Item $td -ItemType Directory -Force | Out-Null }
        Copy-Item $_.FullName (Join-Path $td $asciiName) -Force
    } else {
        Copy-Item $_.FullName (Join-Path $asciiTemp $asciiName) -Force
    }
}
$totalSize = ($files | Measure-Object -Property Size -Sum).Sum
Write-Host "$($files.Count) files, $([math]::Round($totalSize/1MB,2)) MB"

$cabFile = Join-Path $asciiTemp "Setup.cab"
$ddf = Join-Path $asciiTemp "setup.ddf"
@"
.Set CabinetName1=Setup.cab
.Set DiskDirectory1=$asciiTemp
.Set MaxDiskSize=0
.Set Cabinet=on
.Set Compress=on
"@ | Out-File $ddf -Encoding ASCII
Get-ChildItem $asciiTemp -Recurse -File | Where-Object { $_.Name -ne "setup.ddf" } | Sort-Object FullName | ForEach-Object {
    Add-Content -Path $ddf -Value $_.FullName -Encoding ASCII
}
$result = & makecab.exe /F $ddf 2>&1 | Out-String
if (-not (Test-Path $cabFile)) { Write-Error "Cabinet failed"; exit 1 }
Write-Host "Cabinet: $((Get-Item $cabFile).Length) bytes"

Write-Host "=== Step 2: Generate VBScript ==="
$vbsPath = Join-Path $asciiTemp "build.vbs"

# Find main exe
$mainExeId = $null; $mainExeFile = $null
foreach ($f in $files) {
    $n = Split-Path $f.Path -Leaf
    if ($n -like "Sboard*.exe" -and $f.Size -gt 100000) { $mainExeId = $f.Id; $mainExeFile = $n }
}
$upgradeCode = "{B2E7A57D-3B2A-4F8C-9E1D-5A2F7C8B9E1A}"
$productCode = [guid]::NewGuid().ToString("B").ToUpper()
$title = "$productName v$Version"
$templateMsi = [System.IO.Path]::GetFullPath((Join-Path $scriptDir "Release\Setup.msi"))
$outputMsi = [System.IO.Path]::GetFullPath((Join-Path $asciiTemp "Setup.msi"))
Copy-Item $templateMsi $outputMsi -Force

# Build VBScript dynamically
$vbsLines = @()
$vbsLines += 'Dim installer, db, view, rec'
$vbsLines += 'Set installer = CreateObject("WindowsInstaller.Installer")'
$vbsLines += "Set db = installer.OpenDatabase(""$outputMsi"", 1)"

# DELETE old data
$vbsLines += 'db.OpenView("DELETE FROM `File`").Execute'
$vbsLines += 'db.OpenView("DELETE FROM `Component`").Execute'
$vbsLines += 'db.OpenView("DELETE FROM `FeatureComponents`").Execute'
$vbsLines += 'db.OpenView("DELETE FROM `Directory`").Execute'
$vbsLines += 'db.OpenView("DELETE FROM `Shortcut`").Execute'
$vbsLines += 'db.OpenView("DELETE FROM `Registry`").Execute'
$vbsLines += 'db.OpenView("DELETE FROM `Icon`").Execute'
$vbsLines += 'db.OpenView("DELETE FROM `Media`").Execute'
$vbsLines += 'db.OpenView("DELETE FROM `CreateFolder`").Execute'
$vbsLines += 'db.OpenView("DELETE FROM `Feature`").Execute'
$vbsLines += 'db.OpenView("DELETE FROM `Property`").Execute'
$vbsLines += 'db.OpenView("DELETE FROM `LaunchCondition`").Execute'
$vbsLines += 'db.OpenView("DELETE FROM `InstallExecuteSequence`").Execute'
$vbsLines += 'db.OpenView("DELETE FROM `InstallUISequence` WHERE `Action`=''DIRCA_CheckNETCore'' OR `Action`=''ERRCA_UIANDADVERTISED'' OR `Action`=''VSDCA_VsdLaunchConditions''").Execute'
$vbsLines += 'db.OpenView("DELETE FROM `CustomAction` WHERE `Source`=''MSVBDPCADLL'' OR `Target`=''[VSDVERSIONMSG]'' OR `Target`=''[VSDUIANDADVERTISED]''").Execute'
$vbsLines += 'db.OpenView("DELETE FROM `Binary` WHERE `Name`=''MSVBDPCADLL''").Execute'
$vbsLines += 'db.OpenView("DELETE FROM `Upgrade`").Execute'

# Property (2 string cols)
$props = @(
    @("ProductCode", $productCode),
    @("ProductVersion", $Version),
    @("ProductName", $productName),
    @("Manufacturer", "Sboard"),
    @("ProductLanguage", "1042"),
    @("UpgradeCode", $upgradeCode),
    @("ALLUSERS", "1"),
    @("ARPPRODUCTICON", "appicon.ico"),
    @("SecureCustomProperties", "PREVIOUSVERSIONSINSTALLED;NEWERPRODUCTFOUND"),
    @("ErrorDialog", "ErrorDialog")
)
$vbsLines += 'Set rec = installer.CreateRecord(2)'
foreach ($p in $props) {
    $v = ($p[1] -replace '"', '""') -replace "`r|`n", ""
    $vbsLines += "rec.StringData(1)=""$($p[0])"":rec.StringData(2)=""$v"":db.OpenView(""INSERT INTO ``Property`` (``Property``,``Value``) VALUES (?,?)"").Execute(rec)"
}

# Directory (3 string cols: Directory, Directory_Parent, DefaultDir)
$dirs = @(
    @("TARGETDIR","","SourceDir"),
    @("ProgramFiles64Folder","TARGETDIR","."),
    @("ProductFolder","ProgramFiles64Folder",$productName),
    @("DesktopFolder","TARGETDIR","."),
    @("ProgramMenuFolder","TARGETDIR","."),
    @("StartMenuFolder","ProgramMenuFolder",$productName)
)
$vbsLines += 'Set rec = installer.CreateRecord(3)'
foreach ($d in $dirs) {
    $vd0 = ($d[0] -replace '"', '""') -replace "`r|`n", ""
    $vd1 = ($d[1] -replace '"', '""') -replace "`r|`n", ""
    $vd2 = ($d[2] -replace '"', '""') -replace "`r|`n", ""
    $vbsLines += "rec.StringData(1)=""$vd0"":rec.StringData(2)=""$vd1"":rec.StringData(3)=""$vd2"":db.OpenView(""INSERT INTO ``Directory`` (``Directory``,``Directory_Parent``,``DefaultDir``) VALUES (?,?,?)"").Execute(rec)"
}

# Component (6 cols: Component, ComponentId, Directory_, Attributes[SHORT], Condition, KeyPath)
$compGuid = [guid]::NewGuid().ToString("B").ToUpper()
$shortcutCompGuid1 = [guid]::NewGuid().ToString("B").ToUpper()
$shortcutCompGuid2 = [guid]::NewGuid().ToString("B").ToUpper()
$vbsLines += 'Set rec = installer.CreateRecord(6)'
$vbsLines += "rec.StringData(1)=""MainComponent"":rec.StringData(2)=""$compGuid"":rec.StringData(3)=""ProductFolder"":rec.IntegerData(4)=0:rec.StringData(5)="""":rec.StringData(6)=""$mainExeId"":db.OpenView(""INSERT INTO ``Component`` (``Component``,``ComponentId``,``Directory_``,``Attributes``,``Condition``,``KeyPath``) VALUES (?,?,?,?,?,?)"").Execute(rec)"
$vbsLines += "rec.StringData(1)=""DesktopShortcutComp"":rec.StringData(2)=""$shortcutCompGuid1"":rec.StringData(3)=""DesktopFolder"":rec.IntegerData(4)=0:rec.StringData(5)="""":rec.StringData(6)="""":db.OpenView(""INSERT INTO ``Component`` (``Component``,``ComponentId``,``Directory_``,``Attributes``,``Condition``,``KeyPath``) VALUES (?,?,?,?,?,?)"").Execute(rec)"
$vbsLines += "rec.StringData(1)=""StartMenuShortcutComp"":rec.StringData(2)=""$shortcutCompGuid2"":rec.StringData(3)=""StartMenuFolder"":rec.IntegerData(4)=0:rec.StringData(5)="""":rec.StringData(6)="""":db.OpenView(""INSERT INTO ``Component`` (``Component``,``ComponentId``,``Directory_``,``Attributes``,``Condition``,``KeyPath``) VALUES (?,?,?,?,?,?)"").Execute(rec)"

# Feature (8 cols: Feature, Feature_Parent, Title, Description, Display[SHORT], Level[SHORT], Directory_, Attributes[SHORT])
$vbsLines += 'Set rec = installer.CreateRecord(8)'
$titleEsc = ($title -replace '"', '""') -replace "`r|`n", ""
$vbsLines += "rec.StringData(1)=""DefaultFeature"":rec.StringData(2)="""":rec.StringData(3)=""$titleEsc"":rec.StringData(4)=""$titleEsc"":rec.IntegerData(5)=1:rec.IntegerData(6)=1:rec.StringData(7)=""ProductFolder"":rec.IntegerData(8)=0:db.OpenView(""INSERT INTO ``Feature`` (``Feature``,``Feature_Parent``,``Title``,``Description``,``Display``,``Level``,``Directory_``,``Attributes``) VALUES (?,?,?,?,?,?,?,?)"").Execute(rec)"

# FeatureComponents (2 string cols: Feature_, Component_)
$vbsLines += 'Set rec = installer.CreateRecord(2)'
$vbsLines += 'rec.StringData(1)="DefaultFeature":rec.StringData(2)="MainComponent":db.OpenView("INSERT INTO `FeatureComponents` (`Feature_`,`Component_`) VALUES (?,?)").Execute(rec)'
$vbsLines += 'rec.StringData(1)="DefaultFeature":rec.StringData(2)="DesktopShortcutComp":db.OpenView("INSERT INTO `FeatureComponents` (`Feature_`,`Component_`) VALUES (?,?)").Execute(rec)'
$vbsLines += 'rec.StringData(1)="DefaultFeature":rec.StringData(2)="StartMenuShortcutComp":db.OpenView("INSERT INTO `FeatureComponents` (`Feature_`,`Component_`) VALUES (?,?)").Execute(rec)'

# Files (8 cols: File, Component_, FileName, FileSize[INT], Version, Language, Attributes[INT], Sequence[INT])
$vbsLines += 'Set rec = installer.CreateRecord(8)'
foreach ($f in $files) {
    $origName = Split-Path $f.Path -Leaf
    $msiFileName = "$($f.AsciiName)|$origName"
    $fsize = $f.Size; $fseq = $f.Seq
    $vbsLine = 'rec.StringData(1)="' + $f.Id + '":rec.StringData(2)="MainComponent":rec.StringData(3)="' + $msiFileName + '":rec.IntegerData(4)=' + $fsize + ':rec.StringData(5)="":rec.StringData(6)="":rec.IntegerData(7)=0:rec.IntegerData(8)=' + $fseq + ':db.OpenView("INSERT INTO `File` (`File`,`Component_`,`FileName`,`FileSize`,`Version`,`Language`,`Attributes`,`Sequence`) VALUES (?,?,?,?,?,?,?,?)").Execute(rec)'
    $vbsLines += $vbsLine
}

# Media (6 cols: DiskId[INT], LastSequence[INT], DiskPrompt, Cabinet, VolumeLabel, Source)
$vbsLines += 'Set rec = installer.CreateRecord(6)'
$vbsLines += "rec.IntegerData(1)=1:rec.IntegerData(2)=$($files.Count):rec.StringData(3)="""":rec.StringData(4)=""#Setup.cab"":rec.StringData(5)="""":rec.StringData(6)="""":db.OpenView(""INSERT INTO ``Media`` (``DiskId``,``LastSequence``,``DiskPrompt``,``Cabinet``,``VolumeLabel``,``Source``) VALUES (?,?,?,?,?,?)"").Execute(rec)"

# CreateFolder (2 string cols: Directory_, Component_)
$vbsLines += 'Set rec = installer.CreateRecord(2)'
$vbsLines += "rec.StringData(1)=""ProductFolder"":rec.StringData(2)=""MainComponent"":db.OpenView(""INSERT INTO ``CreateFolder`` (``Directory_``,``Component_``) VALUES (?,?)"").Execute(rec)"

# Shortcuts (12 cols: Shortcut, Directory_, Name, Component_, Target, Arguments, Description, Hotkey[INT], Icon_, IconIndex[INT], ShowCmd[INT], WkDir)
$vbsLines += 'Set rec = installer.CreateRecord(12)'
$titleEsc = ($title -replace '"', '""') -replace "`r|`n", ""
$vbsLines += "rec.StringData(1)=""DesktopShortcut"":rec.StringData(2)=""DesktopFolder"":rec.StringData(3)=""$titleEsc.lnk"":rec.StringData(4)=""DesktopShortcutComp"":rec.StringData(5)=""$mainExeId"":rec.StringData(6)="""":rec.StringData(7)=""$titleEsc"":rec.IntegerData(8)=0:rec.StringData(9)="""":rec.IntegerData(10)=0:rec.IntegerData(11)=1:rec.StringData(12)=""ProductFolder"":db.OpenView(""INSERT INTO ``Shortcut`` (``Shortcut``,``Directory_``,``Name``,``Component_``,``Target``,``Arguments``,``Description``,``Hotkey``,``Icon_``,``IconIndex``,``ShowCmd``,``WkDir``) VALUES (?,?,?,?,?,?,?,?,?,?,?,?)"").Execute(rec)"
$vbsLines += "rec.StringData(1)=""StartMenuShortcut"":rec.StringData(2)=""StartMenuFolder"":rec.StringData(3)=""$titleEsc.lnk"":rec.StringData(4)=""StartMenuShortcutComp"":rec.StringData(5)=""$mainExeId"":rec.StringData(6)="""":rec.StringData(7)=""$titleEsc"":rec.IntegerData(8)=0:rec.StringData(9)="""":rec.IntegerData(10)=0:rec.IntegerData(11)=1:rec.StringData(12)=""ProductFolder"":db.OpenView(""INSERT INTO ``Shortcut`` (``Shortcut``,``Directory_``,``Name``,``Component_``,``Target``,``Arguments``,``Description``,``Hotkey``,``Icon_``,``IconIndex``,``ShowCmd``,``WkDir``) VALUES (?,?,?,?,?,?,?,?,?,?,?,?)"").Execute(rec)"

# Registry (6 cols: Registry, Root[INT], Key, Name, Value, Component_)
$vbsLines += 'Set rec = installer.CreateRecord(6)'
$regKey = "SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\$productCode"
$vbsLines += "rec.StringData(1)=""DisplayVersion"":rec.IntegerData(2)=-1:rec.StringData(3)=""$regKey"":rec.StringData(4)=""DisplayVersion"":rec.StringData(5)=""$Version"":rec.StringData(6)=""MainComponent"":db.OpenView(""INSERT INTO ``Registry`` (``Registry``,``Root``,``Key``,``Name``,``Value``,``Component_``) VALUES (?,?,?,?,?,?)"").Execute(rec)"

# InstallExecuteSequence (3 cols: Action, Condition, Sequence[INT])
$acts = @(
    @("FindRelatedProducts",25),@("LaunchConditions",100),@("ValidateProductID",700),
    @("CostInitialize",800),@("FileCost",900),@("CostFinalize",1000),
    @("SetAllUsers",1030),@("InstallValidate",1400),@("InstallInitialize",1500),
    @("ProcessComponents",1600),@("UnpublishFeatures",1800),@("RemoveShortcuts",1900),
    @("RemoveFiles",2000),@("InstallFiles",4000),@("CreateShortcuts",4500),
    @("WriteRegistryValues",4600),@("RegisterUser",4601),@("RegisterProduct",4602),
    @("PublishFeatures",4603),@("PublishProduct",4604),@("InstallFinalize",6600)
)
$vbsLines += 'Set rec = installer.CreateRecord(3)'
foreach ($a in $acts) {
    $vbsLines += "rec.StringData(1)=""$($a[0])"":rec.StringData(2)="""":rec.IntegerData(3)=$($a[1]):db.OpenView(""INSERT INTO ``InstallExecuteSequence`` (``Action``,``Condition``,``Sequence``) VALUES (?,?,?)"").Execute(rec)"
}

# Upgrade (7 cols: UpgradeCode, VersionMin, VersionMax, Language, Attributes[INT], Remove, ActionProperty)
$vbsLines += 'Set rec = installer.CreateRecord(7)'
$vbsLines += "rec.StringData(1)=""$upgradeCode"":rec.StringData(2)="""":rec.StringData(3)="""":rec.StringData(4)="""":rec.IntegerData(5)=768:rec.StringData(6)="""":rec.StringData(7)=""PREVIOUSVERSIONSINSTALLED"":db.OpenView(""INSERT INTO ``Upgrade`` (``UpgradeCode``,``VersionMin``,``VersionMax``,``Language``,``Attributes``,``Remove``,``ActionProperty``) VALUES (?,?,?,?,?,?,?)"").Execute(rec)"

# Icon (skip - SetStream not available, icon from cabinet instead)

# Commit database first, then release it
$vbsLines += 'db.Commit'

# Release database before updating summary info
$vbsLines += 'Set db = Nothing'
$vbsLines += 'Set view = Nothing'
$vbsLines += 'Set rec = Nothing'

# Now update SummaryInfo stream (database must be closed)
$vbsLines += 'On Error Resume Next'
$vbsLines += 'Dim sum'
$vbsLines += "Set sum = installer.SummaryInformation(""$outputMsi"", 1)"
$titleEscSum = ($title -replace '"', '""') -replace "`r|`n", ""
$vbsLines += "sum.Property(2)=""$titleEscSum"""
$vbsLines += "sum.Property(4)=""Sboard"""
$vbsLines += 'sum.Persist'
$vbsLines += 'Set sum = Nothing'
$vbsLines += 'On Error Goto 0'

# Embed cabinet as embedded stream
$cabPathVbs = $cabFile -replace '\\', '\\'
$vbsLines += "Set db = installer.OpenDatabase(""$outputMsi"", 1)"
$vbsLines += 'Set view = db.OpenView("SELECT `Name`, `Data` FROM `_Streams`")'
$vbsLines += 'Set rec = installer.CreateRecord(2)'
$vbsLines += 'rec.StringData(1) = "Setup.cab"'
$vbsLines += "rec.SetStream 2, ""$cabPathVbs"""
$vbsLines += 'view.Modify 3, rec  '' 3 = msiViewModifyAssign (insert or replace)'
$vbsLines += 'view.Close'
$vbsLines += 'db.Commit'
$vbsLines += 'Set db = Nothing'
$vbsLines += 'Set view = Nothing'
$vbsLines += 'Set rec = Nothing'
$vbsLines += 'WScript.Echo "Cabinet embedded"'

$vbsLines += 'WScript.Echo "OK"'

# Write VBScript
[System.IO.File]::WriteAllText($vbsPath, ($vbsLines -join "`r`n"), [System.Text.Encoding]::Unicode)

Write-Host "=== Step 3: Run VBScript to update MSI ==="
$vbsResult = & cscript.exe //nologo $vbsPath 2>&1
Write-Host $vbsResult
if ($vbsResult -notcontains "OK") { Write-Error "VBScript failed"; exit 1 }

Write-Host "=== Step 4: Copy output ==="
$finalMsi = [System.IO.Path]::GetFullPath((Join-Path $scriptDir "Release\Setup.msi"))
Copy-Item $outputMsi $finalMsi -Force
Copy-Item $cabFile (Join-Path (Split-Path $finalMsi -Parent) "Setup.cab") -Force
Write-Host "MSI: $finalMsi ($([math]::Round((Get-Item $finalMsi).Length/1MB,2)) MB)"
Write-Host "CAB: $(Join-Path (Split-Path $finalMsi -Parent) 'Setup.cab') ($([math]::Round((Get-Item (Join-Path (Split-Path $finalMsi -Parent) 'Setup.cab')).Length/1MB,2)) MB)"

# Cleanup
Remove-Item $asciiTemp -Recurse -Force
Write-Host "Done!"
