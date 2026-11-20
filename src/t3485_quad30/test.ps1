$ErrorActionPreference='Stop'
$sourceRoot=$PSScriptRoot
$managed=Join-Path (Split-Path (Split-Path $sourceRoot -Parent) -Parent) 'GHPC_Data\Managed'
$lines=@('/nologo','/target:exe','/out:"ManagedTests.exe"','ManagedTests.cs')
foreach($name in @('Assembly-CSharp.dll','Assembly-CSharp-firstpass.dll','UnityEngine.CoreModule.dll','netstandard.dll')){$lines+=('/reference:"'+(Join-Path $managed $name)+'"')}
Set-Content -LiteralPath (Join-Path $sourceRoot 'managedtests.rsp') -Encoding UTF8 -Value $lines
Push-Location $sourceRoot
try {
 & 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe' '@managedtests.rsp'
 if($LASTEXITCODE -ne 0){throw 'Managed test compilation failed'}
 & dotnet '.\ManagedTests.exe'
 if($LASTEXITCODE -ne 0){throw 'Managed tests failed'}
}finally{Pop-Location}
