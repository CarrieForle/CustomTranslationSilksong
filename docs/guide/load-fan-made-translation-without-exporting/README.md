# Load fan-made translation without exporting

This is needed when the translators did not update their `resources.assets` to the latest version of the game and cause the game in an unplayable state upon replacement.

The process boils down to extracting the text files from `resources.assets`, decrypt them, and add `metadata.json`.

## Extraction

To extract `*.assets`, download and run [AssetRipper](https://assetripper.github.io/AssetRipper/).

Go to File > Settngs. In Import section change Default Version to `6000.0.50f1`. Scroll down to the bottom of the page and save.

Go to File > Open File. Select the targeted `resources.assets`.

Once loaded, go to Export > Export All Files. Select a folder to export and click "Export Primary Content".

Open the exported folder in file explorer and go to Assets > Text Assets.

Here are all the encrypted text files of every language. Fan-made translations will replace one of the language with their text and you can't tell until they're decrypted. In practice though, it's usually English (`EN_<sheet>.bytes`).

## Decryption

[There's a general purpose tool](https://www.nexusmods.com/hollowknightsilksong/mods/10) that can decrypt these text, and I also made a tool to [decrypt these text](https://github.com/CarrieForle/DecryptTextAssetSilksong). Choose one and follow the instructions.

After the files are decrypted, add `metadata.json`, create a new folder under `translation` and copy them over. The mod should be able to load them.