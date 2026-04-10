# Load fan-made translation

In most cases, you can export fan-made translation from the game and then load them with the mod. Here's how:

1. Load the translation as per instructions. This usually means replacing `resources.assets` plus other files for font, etc. You need to backup the game files before replacing.
2. Start Silksong with the mod installed.
3. Go to Options > Game and select that language.
4. Go back and go to Mods > Custom Translation > Export current language.
5. The game will open a folder containing the exported files. Copy `entry` folder to `translation` in the mod installation folder.
6. Under `translation`, rename `entry` folder to its language name.
7. Open `metadata.json` and replace `Language` field with the correct [language code](https://github.com/SFGrenade/LanguageSupport-Repo#table-of-available-language-codes).
8. Save and close `metadata.json`.
9. Return to Silksong and hit 'Reload translation'.
10. Go to language menu and see if it's there. You can tell by checking if there are two same languages. Or close Silksong, replace the game files with the original backup, and start Silksong to see if it's there.

If you confirmed it's there, you can replace the game files with the original backup.

If your language isn't rendered correcly due to font issue. See [Ukrainian example](#font) for how to swap fonts.

In some case, you need to extract the files yourself because you can't start Silksong after replacing the files. This can be done with tools like [AssetRipper](https://github.com/AssetRipper/AssetRipper) and [AssetStudio](https://github.com/Perfare/AssetStudio/). You can follow the [Indonesian example](#example-indonesian) below for a walkthrough.

## Example: Ukrainian

Let's try to load [fan-made Ukrainian translation by SkS_Punk](https://steamcommunity.com/sharedfiles/filedetails/?id=3582065114).

### Translation

Install the translation as per instructions. Make sure to back up game files before proceeding.

Start Silksong. Select Ukrainian in the language menu. 

Go back and go to Mods > Custom Translation > Export current language.

A folder containing the export files will appear. Copy `entry` folder into `translation` folder in the mod installation folder. Rename it to `UK`.

Under `UK` in `translation`, open `metadata.json`. Replace `"Language": "PT"` with `"Language": "UK"`.

Save and close. Go back to Silksong and hit 'Reload translation'.

Loop the entire language selection once. If Ukrainian appear twice, it means it works! One is from the replaced files and the other from this mod.

Once you confirm it works, you can close the game and replace the game files with the original backup.

### Font

Silksong can't render some Ukrainian letter, so we need to provide a font. Any Ukrainian font would do. In this example we will use [Lora](https://fonts.google.com/specimen/Lora?lang=uk_Cyrl&preview.lang=uk_Cyrl).

Before you download the font. Create a folder named `assets` under `UK`.

Download the font and unzip it. It should contain `Lora-VariableFont_wght.ttf`. Copy that file to `assets` folder in the mod installation folder and rename it to `text.ttf`.

Start Silksong and you should see the font changed (assuming you selected Ukrainian before you closed the game).

> [!NOTE]
> Silksong uses two fonts throughout the game and you can replace both with different fonts. See [asset](../../reference#asset).

## Example: Indonesian

Because [Indonesian translation](https://www.nexusmods.com/hollowknightsilksong/mods/273) hasn't been updated for a long time, it will break the game in the current version. This means we need to extract the files ourselves. Let's download the files and start!

### Extraction

To extract `*.assets`, download and run [AssetRipper](https://assetripper.github.io/AssetRipper/).

AssetRipper will open a webpage. On that page, go to File > Settngs. In Import section change Default Version to `6000.0.50f1`. Scroll down to the bottom of the page and save.

Go to File > Open File. Select the downloaded `resources.assets`.

Once loaded, go to Export > Export All Files. Click "Select Folder" and select a folder to export. Check "Create Subfolder" and click "Export Primary Content".

Open the exported folder in file explorer and go to Assets > Text Assets.

Here are all the encrypted text files of every language. Fan-made translations will replace one of the language with their text and you can't tell until they're decrypted. In this case of Indonesian, it's English (`EN_<sheet>.bytes`).

Create a new folder called `ID` under `translation` in the mod installation folder. Copy all the `EN_<sheet>.bytes` files to `ID`.

Create `metadata.json` and write following text:

```json
{
  "Language": "ID"
}
```

Close and save. Start Silksong and see there is Indonesian.

If it works. It's great! ...However you could not edit the translation text because they're encrypted. You need to decrypt it to edit those text.

### Decryption

The easiest way to decrypt it is export the current language from the game. In Silksong, choose Indonesian from the language menu, then go back and go to Mods > Custom Translation > Export current language. 

A folder will be opened containing the exported files which are decrypted. Copy `entry` folder into `translation` folder in the mod installation folder. Remove `ID` folder and rename `entry` folder to `ID`. Go back to the game and hit 'Reload translation' to see if it works.

> [!NOTE]
If for some reason you can't decrypt the files within the game,
[this tool by zhoppers](https://www.nexusmods.com/hollowknightsilksong/mods/10) can do it. I also made [a tool](https://github.com/CarrieForle/DecryptTextAssetSilksong) for this purpose.
