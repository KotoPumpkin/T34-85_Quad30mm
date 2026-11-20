$ErrorActionPreference='Stop'
$buildRoot=$PSScriptRoot
$modRoot=Split-Path $buildRoot -Parent
$binRoot=Split-Path $modRoot -Parent
$managed=Join-Path $binRoot 'GHPC_Data\Managed'
$loader=Join-Path $binRoot 'MelonLoader\net35'
$csc='C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$refs=@((Join-Path $loader 'MelonLoader.dll'),(Join-Path $loader '0Harmony.dll'))
$names=@('Assembly-CSharp.dll','Assembly-CSharp-firstpass.dll','UnityEngine.dll','UnityEngine.CoreModule.dll','UnityEngine.InputLegacyModule.dll','UnityEngine.IMGUIModule.dll','UnityEngine.PhysicsModule.dll','UnityEngine.ParticleSystemModule.dll','UnityEngine.AudioModule.dll','UnityEngine.ImageConversionModule.dll','UnityEngine.TextRenderingModule.dll','UnityEngine.UIModule.dll','UnityEngine.UI.dll','Unity.TextMeshPro.dll','Unity.Postprocessing.Runtime.dll','Unity.Addressables.dll','Unity.ResourceManager.dll','netstandard.dll')
foreach($n in $names){$refs+=Join-Path $managed $n}
$lines=@('/nologo','/target:library','/optimize+','/debug:pdbonly','/out:"T3485Quad30.dll"')
foreach($ref in $refs){$lines+=('/reference:"'+$ref+'"')}
foreach($f in @('ModCore.cs','AmmoCatalog.cs','Reflection.cs','PrefabProvider.cs','NativeAssets.cs','MeshPayload.cs','ModelChanges.cs','ArmorChanges.cs','WeaponConversion.cs','QuadWeaponConversion.cs','QuadGeometry.cs','ShieldPaint.cs','PerformanceChanges.cs','Stabilization.cs','ThermalSight.cs','MissileLauncher.cs','SightRegistry.cs','NativeValidation.cs','NativeControls.cs','EraMath.cs','EraGeometry.cs','FuldaEraAdapter.cs','ReactiveArmor.cs','RuntimeState.cs','QuadMod.cs')){$lines+=('"'+$f+'"')}
$lines+='/resource:"2a42.meshbin",T3485Quad30.2a42.meshbin'
$lines | Set-Content -Encoding UTF8 (Join-Path $buildRoot 'build.rsp')
Push-Location $buildRoot
try{& $csc '@build.rsp';if($LASTEXITCODE -ne 0){throw 'Mod compilation failed'}}finally{Pop-Location}

