param([Parameter(Mandatory = $true)][string]$Phase, [Parameter(Mandatory = $true)][string]$Version)
$ErrorActionPreference = 'Stop'; Set-StrictMode -Version Latest
$repository = [System.IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot)); $name='Mech Muster'; $package='CoolNether123.MechMuster'; $description='Assign newly available mechs to mechanitors with deficits.'
$aboutXml=@"
<?xml version="1.0" encoding="utf-8"?>
<ModMetaData><name>$name</name><author>CoolNether123</author><packageId>$package</packageId><modVersion>1.0.0</modVersion><supportedVersions><li>$Version</li></supportedVersions><modDependencies><li><packageId>Ludeon.RimWorld.Biotech</packageId><displayName>Biotech</displayName></li><li><packageId>brrainz.harmony</packageId><displayName>Harmony</displayName></li><li><packageId>CoolNether123.Spine</packageId><displayName>SpineLib</displayName></li></modDependencies><loadAfter><li>Ludeon.RimWorld.Biotech</li><li>brrainz.harmony</li><li>CoolNether123.Spine</li></loadAfter><description>$description RimWorld $Version support build.</description></ModMetaData>
"@;
$loadFoldersXml = @"
<?xml version="1.0" encoding="utf-8"?>
<loadFolders>
  <v$Version>
    <li>/</li>
  </v$Version>
</loadFolders>
"@
if($Phase -eq 'after-merge'){[System.IO.File]::WriteAllText((Join-Path $repository 'About\About.xml'),$aboutXml);[System.IO.File]::WriteAllText((Join-Path $repository 'LoadFolders.xml'),$loadFoldersXml);& git -C $repository add -- About/About.xml LoadFolders.xml;if($LASTEXITCODE -ne 0){throw 'Could not stage support metadata.'}}
elseif($Phase -eq 'before-stage'){$assembly=[string](Get-Content -Raw -LiteralPath (Join-Path $repository 'Tools\CascadeManifest.json')|ConvertFrom-Json).build.expectedAssembly;$source=Join-Path $repository "$Version\Assemblies\$assembly";$root=Join-Path $repository 'Assemblies';[System.IO.Directory]::CreateDirectory($root)|Out-Null;[System.IO.File]::Copy($source,(Join-Path $root $assembly),$true);& git -C $repository add -- Assemblies About/About.xml LoadFolders.xml;if($LASTEXITCODE -ne 0){throw 'Could not stage support payload.'}}
