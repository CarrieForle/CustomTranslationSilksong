# CustomTranslation

Load new languages or modify translation in Silksong.

This mod does not introduce new language/translation. Instead it allows you to translate Silksong and add new languages into the game.

## Features

- Choose new languages from in-game menu.
- Compatible with [I18N](https://thunderstore.io/c/hollow-knight-silksong/p/silksong_modding/I18N/), allowing you to translate mods in unsupported languages.
- Export translation texts.
- Load existing fan-made localizations without replacing game files.
- Load external fonts per language.
- Reload translation.

## Limitation

- Right-to-left text will be rendered left-to-right because Silksong's text frameworks (TextMeshPro and uGui) do not support it. This means languages such as Arabic remains unsupported.
- Fonts in game and pause menu can't be replaced.

## Install

For manual installation, first [install BepinEx](https://docs.bepinex.dev/articles/user_guide/installation/index.html#installing-bepinex-1). Download the mod. Go to Silksong installation folder (where you should've installed BepinEx) and extract the mod zip file under `BepinEx/plugins`. You also need to install the dependencies which can be found on [Thunderstore](https://thunderstore.io/c/hollow-knight-silksong/p/CarrieForle/CustomTranslation/).

This is what your folder structure should look like:

```
.
└── BepinEx/
    └── plugins/
        ├── CarrieForle-CustomTranslation/
        │   ├── CustomTranslation.dll
        │   ├── CustomTranslation.pdb
        │   └── ...
        └── ...
```

## Usage

See [here](https://github.com/CarrieForle/CustomTranslationSilksong/tree/main/docs).

## Contribution

Documentation and mod translation are welcomed, but please don't submit fan-made translation.

## Build

.NET 10 is required.

Create `SilksongPath.props`. Copy and paste the following text and edit as needed.

```xml
<Project>
  <PropertyGroup>
    <SilksongFolder>SilksongInstallPath</SilksongFolder>
    <!-- If you use a mod manager rather than manually installing BepInEx, this should be a profile directory for that mod manager. -->
    <SilksongPluginsFolder>$(SilksongFolder)/BepInEx/plugins</SilksongPluginsFolder>
  </PropertyGroup>
</Project>
```

```sh
dotnet build -c Release
```