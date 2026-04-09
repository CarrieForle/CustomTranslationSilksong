If you haven't installed the mod, do it before reading this.

# Load new Language

First run Silksong once to generate necessary files. You can immediately close it after entering the menu.

Go to the mod installation folder. This is either a profile folder if you use a mod manager, or Silksong installation folder for manual installation (`<Silksong or profile folder>/BepinEx/plugins/CarrieForle-CustomTranslation`).

Here you should see a folder called `translation`. This is where you put all the translation files. You would download other people's work of translation zip files and unzip them to here.

For example:

```
CarrieForle-CustomTranslation/
├── ...
└── translation/
    ├── ID/
    │   ├── entry.txt
    │   └── metadata.json
    ├── NL/
    │   ├── EN_Achievement.txt
    │   ├── ...
    │   └── metadata.json
    └── TR/
        ├── EN_Achievement.bytes
        ├── ...
        └── metadata.json
```

Here, the mod will load 3 languages: Indonesian (ID), Dutch (NL), and Turkish (TR). Each language is separated in folders which contain their translation files. When you start the game, you can select these languages from the game menu.

> [!NOTE]
> This mod contains another folder called `languages`. This is for the translation **just for this mod**, not related to vanilla game text.

This mod is also capable of loading existing fan-made translation to an extent, so that you don't have to replace game files and can easily manage it with this mod. [See here for more details](guide/load-fan-made-translation-without-exporting/README.md).

# I18N

This mod is compatible with [I18N](https://thunderstore.io/c/hollow-knight-silksong/p/silksong_modding/I18N/) and allow you to translate/load translation in unsupported languages for mods by including the translation file named with lowercase language code.

For example, if you want to translate a mod into Thai, you would provide `th.json` under its `languages` folder.

[Here's the complete list of language codes.](https://github.com/SFGrenade/LanguageSupport-Repo#table-of-available-language-codes)

# Translation quickstart

This section is intended for translators. If you just want to load translation, you can skip this part. 

Let's translate some texts in Indonesian to see how this is done (You don't actually need know Indonesian).

First, go into `translation` folder and create a new subfolder named `ID`. 

> [!NOTE]
> This subfolder can be named anything, but it's good to use the language as the name to separate other translation files.

Create an `entry.json` text file. Copy and paste the following text:

```json
{
  "MainMenu": {
    "LANG_CURRENT": "Indonesia",
    "MAIN_ACHIEVEMENTS": "Pencapaian",
    "MAIN_START": "Mulai Permainan",
    "MAIN_OPTIONS": "Pengaturan",
    "EXTRAS_CREDITS": "Kredit",
    "MAIN_QUIT": "Keluar Game"
  }
}
```

`entry.json` includes the translated texts. When you switch to Indonesian from the game menu, the mod will look at `entry.json`, and load these texts into the game.

It's okay If you don't know JSON. All you need to care about is the text in each line after colon `:`, surrounded by quotation marks `"`. These are the actual texts that you should modify/translate.

For comparison, here's the original text in English:

```json
{
  "MainMenu": {
    "LANG_CURRENT": "English",
    "MAIN_ACHIEVEMENTS": "Achievements",
    "MAIN_START": "Start Game",
    "MAIN_OPTIONS": "Options",
    "EXTRAS_CREDITS": "Credits",
    "MAIN_QUIT": "Quit Game"
  }
}
```

Notice how everything else didn't change? Well, you're not going to modify those anyway.

<hr>

Next, create a `metadata.json` text file. Copy and paste the following text:

```json
{
  "Language": "ID"
}
```

`metadata.json` tells the mod which language of this translation is, and `ID` tells the mod that it is Indonesian. To translate into other languages, put the appropriate language code in place of `ID`. 

> [!NOTE]
> [The complete list of language codes](https://github.com/SFGrenade/LanguageSupport-Repo#table-of-available-language-codes)

<hr>

Now you have all the necessary materials. Save and close both files. Start Silksong and go to Options > Game and change the language.

Congratulations! You just added a new language in Silksong!

Next step, [take a look at the reference to see what you can do](reference/README.md).
