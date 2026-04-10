# Used in Nexus CD. Pass BUILD_DIR envvar for dll

$name = "CarrieForle-CustomTranslation"
$modUrls = 
(
	(
		"silksong_modding-I18N", 
		"https://thunderstore.io/package/download/silksong_modding/I18N/1.0.3/", 
		"https://raw.githubusercontent.com/silksong-modding/Silksong.I18N/refs/heads/main/LICENSE"
	),
	(
		"silksong_modding-DataManager", 
		"https://thunderstore.io/package/download/silksong_modding/DataManager/1.2.2/",
		"https://raw.githubusercontent.com/silksong-modding/Silksong.DataManager/refs/heads/main/LICENSE"
	),
	(
		"silksong_modding-ModMenu", 
		"https://thunderstore.io/package/download/silksong_modding/ModMenu/0.5.2/",
		"https://raw.githubusercontent.com/silksong-modding/Silksong.ModMenu/refs/heads/main/LICENSE"
	),
	(
		"silksong_modding-UnityHelper", 
		"https://thunderstore.io/package/download/silksong_modding/UnityHelper/1.2.0/",
		"https://raw.githubusercontent.com/silksong-modding/Silksong.DataManager/refs/heads/main/LICENSE"
	),
	(
		"SFGrenade-WavLib", 
		"https://thunderstore.io/package/download/SFGrenade/WavLib/1.1.1/",
		"https://raw.githubusercontent.com/SFGrenade/WavLib/refs/heads/master/LICENSE"
	),
	(
		"CarrieForle-CustomFont",
		"https://thunderstore.io/package/download/CarrieForle/CustomFont/0.1.1/",
		"https://raw.githubusercontent.com/CarrieForle/CustomFontSilksong/refs/heads/main/LICENSE"
	)
)

# These are MIT
$monoDetoursUrls = (
	"https://thunderstore.io/package/download/MonoDetour/MonoDetour/0.7.13/",
	"https://thunderstore.io/package/download/MonoDetour/MonoDetour_BepInEx_5/0.7.13/"
)

$binDir = "$PSScriptRoot/bin"
$tmpDir = "$binDir/tmp"
$nexusDir = "$binDir/nexus"
$bepinExDir = "$nexusDir/BepInEx"
$pluginDir = "$bepinExDir/plugins"
$myDir = "$pluginDir/$name"

if (!($?))
{
	exit 1
}

if (Test-Path $binDir) 
{ 
	Remove-Item $binDir -Recurse -Force 
}

if (!(Test-Path $pluginDir)) 
{ 
	New-Item -ItemType Directory $pluginDir
}

if (!(Test-Path $myDir))
{
	New-Item -ItemType Directory $myDir
}

foreach ($modUrl in $modUrls)
{
	$m_name = $modUrl[0]
	$m_url = $modUrl[1]
	$m_licenseUrl = $modUrl[2]

	Write-Host "Downloading $m_url"
	$depPath = "$pluginDir/$m_name"
	$zipPath = "$binDir/archive.zip"
	Invoke-WebRequest -Uri $m_url -OutFile $zipPath
	Expand-Archive -Path $zipPath -DestinationPath $depPath
	Remove-Item $zipPath
	Invoke-WebRequest -Uri $m_licenseUrl -OutFile "$depPath/LICENSE"
}

foreach ($monoDetoursUrl in $monoDetoursUrls)
{
	Write-Host "Downloading $monoDetoursUrl"
	$zipPath = "$binDir/archive.zip"
	Invoke-WebRequest -Uri $monoDetoursUrl -OutFile $zipPath

	if (Test-Path $tmpDir)
	{
		Remove-Item "$tmpDir/*" -Recurse -Force
	}

	Expand-Archive -Path $zipPath -DestinationPath $tmpDir
	foreach ($patchersPath in "core","patchers")
	{
		if (Test-Path "$tmpDir/$patchersPath")
		{
			Copy-Item "$tmpDir/$patchersPath" $bepinExDir -Recurse -Force
		}
	}

	Remove-Item $zipPath
}

Copy-Item "$env:BUILD_DIR/*" -Recurse $myDir
Remove-Item $tmpDir -Recurse -Force
Compress-Archive -Path "$nexusDir/*" -DestinationPath "$nexusDir/$name.zip"