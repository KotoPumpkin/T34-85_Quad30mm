$ErrorActionPreference='Stop'
$sourceRoot=$PSScriptRoot
$modRoot=Split-Path $sourceRoot -Parent
$stage=Join-Path $sourceRoot 'dist\T3485Quad30-0.1.0'
New-Item -ItemType Directory -Force (Join-Path $stage 'Mods\T3485Quad30'),(Join-Path $stage 'Source\t3485_quad30'),(Join-Path $stage 'Validation') | Out-Null
Copy-Item -LiteralPath (Join-Path $sourceRoot 'T3485Quad30.dll') -Destination (Join-Path $stage 'Mods\T3485Quad30.dll') -Force
Copy-Item -LiteralPath (Join-Path $sourceRoot 'README_zh.md') -Destination (Join-Path $stage 'Mods\T3485Quad30\README_zh.md') -Force
$sourceFiles=@('ModCore.cs','AmmoCatalog.cs','Reflection.cs','PrefabProvider.cs','NativeAssets.cs','MeshPayload.cs','ModelChanges.cs','ArmorChanges.cs','WeaponConversion.cs','QuadWeaponConversion.cs','QuadGeometry.cs','ShieldPaint.cs','PerformanceChanges.cs','Stabilization.cs','ThermalSight.cs','MissileLauncher.cs','SightRegistry.cs','NativeControls.cs','NativeValidation.cs','EraMath.cs','EraGeometry.cs','FuldaEraAdapter.cs','ReactiveArmor.cs','RuntimeState.cs','QuadMod.cs','ManagedTests.cs','ManagedTests.runtimeconfig.json','build.ps1','test.ps1','2a42.meshbin','README.md')
foreach($name in $sourceFiles){Copy-Item -LiteralPath (Join-Path $sourceRoot $name) -Destination (Join-Path $stage ('Source\t3485_quad30\'+$name)) -Force}
if(Test-Path -LiteralPath (Join-Path $modRoot 't3485q30-preflight.txt')){Copy-Item -LiteralPath (Join-Path $modRoot 't3485q30-preflight.txt') -Destination (Join-Path $stage 'Validation\native_preflight.txt') -Force}
$intro=@('T-34-85M Quad 30mm 0.1.0','','安装：将本压缩包里的 Mods 文件夹合并到 Gunner HEAT PC/Bin。','需要 MelonLoader 0.7.3 net35，并保留已安装的 Fulda Expansion DLL 与 Fulda1989 完整资源目录；本包不附带该第三方 Mod。','启动游戏，在任务自定义里选择 T-34-85M Quad 30mm (T3485Q30MOD)。','原版下一武器选择四联装 30mm；1/2 切换 3UBR11/3UOF21；E 测距；T 切换昼间/热像。','完整说明见 Mods/T3485Quad30/README_zh.md。','卸载：退出游戏，将 Mods/T3485Quad30.dll 移出 Mods。','Source 是可选源码，不要把 Source 的内容当成额外 Mod DLL 安装。')
$intro | Set-Content -Encoding UTF8 (Join-Path $stage '安装说明.txt')
$validationState=Get-Content -LiteralPath (Join-Path $sourceRoot 'validation_0.1.0.json') -Raw | ConvertFrom-Json
$verifiedRenders=Join-Path $validationState.directory 'renders'
$renders=Join-Path $stage 'Validation/renders';New-Item -ItemType Directory -Force $renders | Out-Null
foreach($imageName in @('front_low.png','front_three_quarter.png','roof_launcher.png','era_left.png','era_right.png','era_front.png','era_roof.png','shield_source_patch.png','shield_cropped_patch.png')){
 $image=Join-Path $verifiedRenders $imageName
 if(-not (Test-Path -LiteralPath $image)){throw "Verified render missing: $image"}
 Copy-Item -LiteralPath $image -Destination (Join-Path $renders $imageName) -Force
}

$hash=(Get-FileHash -Algorithm SHA256 -LiteralPath (Join-Path $stage 'Mods\T3485Quad30.dll')).Hash
($hash+'  Mods/T3485Quad30.dll') | Set-Content -Encoding ASCII (Join-Path $stage 'SHA256SUMS.txt')
$zip=Join-Path $sourceRoot 'dist\T3485Quad30-0.1.0.zip'
Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $zip -Force
Get-Item -LiteralPath $zip | Select-Object FullName,Length,LastWriteTime
