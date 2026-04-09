# CustomTranslation

Load custom translation/localization for unsupported (or supported) languages in Silksong.

It also supports [I18N](https://thunderstore.io/c/hollow-knight-silksong/p/silksong_modding/I18N/), allowing you to translate mods in unsupported languages. See [here](docs/README.md#I18N) for more details.

# Limitation

This mod only modify texts in the game. This means:

- It cannot swap localized assets (e.g., Localized TC Logo in Simplified Chinese).
- It cannot load external fonts. You're out of luck if the vanilla font doens't work with your language.

## Install

It's recommended to use a Thunderstore mod managager (e.g., [r2modman](https://r2modman.com/)) for ease of installation.

You can also do a manual installation. First [install BepinEx](https://docs.bepinex.dev/articles/user_guide/installation/index.html#installing-bepinex-1). Download the mod. Go to Silksong installation folder (where you should've installed BepinEx) and extract the mod zip file under `BepinEx/plugins`.

You also need to install the dependencies (which are also mods). They can be found on Thunderstore. Just download those mods and extract them under `BepinEx/plugins`.

This is what your folder structure should look like:

```
.
└── BepinEx/
    └── plugins/
		├── Other mods...
        ├── CarrieForle-CustomTranslation/
        │   ├── CustomTranslation.dll
        │   ├── CustomTranslation.pdb
		│   └── other contents...
		└── Other mods...
```

## Usage

See [here](https://github.com/CarrieForle/CustomTranslationSilksong/tree/main/docs).

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
