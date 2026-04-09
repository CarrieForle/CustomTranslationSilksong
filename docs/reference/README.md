# Inside a translation

In order for the mod to load a translation/language, you need to provide two things:
- The translation files containing the translation texts. They come in multiple formats, and can be more than one files.
- Metadata that describes the language you're translating as well as configuration. They're written in [`metadata.json`](metadata/README.md).

> [!NOTE]
> In all cases, you don't have to prepare these files from the ground up. You can start translating right away by [exporting translation files](#Export) and modifying those.

Additionally, you can prepare font files in case the language is not supported by the original fonts. See [asset](#asset)

# Translation File Format

This section describe all the formats to write translation in. You only need to write translation in one of them.

## entry.json

This is the simplest format and the preferred way to write translation. You create a text file named `entry.json` containing all the translation texts.

Here's an example `entry.json` in English:

```json
{
  "MainMenu": {
    "MAIN_START": "Start Game",
    "MAIN_OPTIONS": "Options",
    "MAIN_QUIT": "Quit Game"
  }
}
```

## Sheet

This format resembles how Silksong works with translation internally. Translation texts are stored across multiple files. These files are called **sheet**, and each sheet contains one or more texts.

Sheets are written in XML with the file named `<LANGUAGE>_<SHEET_NAME>`, with extensions `.txt` or `.bytes`. 

Sheets can be encrypted. They are stored as files in the encrypted form and only decrypted when the player selects that language in the game. The mod is able to load both encrypted and plain-text (decrypted) forms.

Here is an example of a decrypted sheet with file name `EN_MainMenu.txt`. The corresponding `entry.json` can be found in the previous section:

```xml
<entries>
<entry name="MAIN_START">Start Game</entry>
<entry name="MAIN_OPTIONS">Options</entry>
<entry name="MAIN_QUIT">Quit Game</entry>
</entries>
```

Here, `MainMenu` is the name of the sheet.

You don't need to understand XML to start translating. All you  should care about is the text between `<entry name="...">` and `</entry>`. Those are the text you should translate.

> [!IMPORTANT]
> The mod ignores the `<LANGUAGE>` part of the file name (which is English in this case). Instead it looks at `metadata.json` to determine the language of this sheet.

> [!NOTE]
> If you go back and check `entry.json`, you will find the sheet name is directly written in the file. This is why `entry.json` can store all the translation texts without splitting to multiple files.

## Escape characters

[Escaping character](https://en.wikipedia.org/wiki/Escape_character) is needed when putting the character as-is makes the file an invalid format, such as double quotation mark `"` (`\u0022`) in `entry.json`.

To escape characters, you write Unicode code points as `\uXXXX`. In every case however, you can just put Unicode characters directly, no need to escape. 

For example, the following `entry.json` translate `MAIN_START` to `"Start Game"` (with quotation marks!):

```json
{
  "MainMenu": {
    "MAIN_START": "\u0022Start Game\u0022"
  }
}
```

In Sheet format, some characters can be escaped alternatively:

|Character|Escape character|
|:-:|:-:|
|`'`|`&apos;`|
|`"`|`&quot;`|
|`>`|`&gt;`|
|`<`|`&lt;`|
|`&`|`&amp;`|

# Control Text

If you view exported translation files, you might find a few texts contain something like `<br>` or `<hpage>`, etc. These are special texts that aren't part of the dialogue text, but instead control the behavior of dialogue, and so called Control Text (I made that name up).

|Control Text|Behavior|
|:-:|:--|
|`<br>`|Add a new line|
|`<hpage>`|Start a new dialogue from Hornet|
|`<page>`|Start a new dialogue from NPCs |
|`<page=*>`|Start a new dialogue from NPCs with special event (e.g., voice, behavior). `*` is one of `S`,`T`,`M`,`L`. |

You are free to add or remove control texts except `<page=*>` as it does things more than starting a new dialogue, where changing/adding/removing them might cause unintended behaviors.

# Asset

You can include custom assets that will be replaced upon language selection. 

Currently, you can only replace fonts by providing a `text.otf` (or `ttf`) and `title.otf` (or `ttf`) to replace dialogue font and title font. If you only provide one font, both will use the same font.

To place assets, create an `assets` folder in your translation text folder, and put the respective files in:

```
CarrieForle-CustomTranslation/
└── translation/
    └── YourLanguage/
        ├── ...
        ├── metadata.json
        └── assets/
            └── put your assets here
```

Once you place a font. You can put the following optional fields in [`metadata.json`](metadata/README.md) to configure:

```json
{
	"TextFontScale": 1, // Optional: The scale of text font (e.g., 1.5 means 150% bigger). Default: 1
	"TitleFontScale": 1, // Optional: The scale of title font. Default: 1
	"UseFontAsFallBack": true, // Optional: Only use font when the original couldn't render it. Default: true
}
```

# Export

You can export translation files of a language from the game (including fan-made translation, [see here for how](../guide/load-fan-made-translation-without-exporting/README.md)). This allows you to modify those texts and easily translate to one language from another.

To export a translation, first switch the language from the game menu (Options > Game), go back and go to Mods > Custom Translation. Then click "Export". The mod will export the files in both entry.json and sheet formats and open file explorer to their locations.
