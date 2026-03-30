# Example: fan-made Indonesian translation

[Download link](https://www.nexusmods.com/hollowknightsilksong/mods/934)

Install the translation as per instructions (use an translator if you don't know Indonesian). Make sure to back up `resources.assets` before proceeding.

Start Silksong. Select Indonesian in the language menu. 

Go back and go to Mods > Custom Translation > Export.

Copy `entry` folder into `translation` folder at the root of mod installation folder. Rename it to `ID`.

In `translation/ID`, open `metadata.json`. Replace `"Language": "EN"` with `"Language": "ID"`.

Save and close. Start Silksong.

Loop the entire language menu once. If Indonesian appear twice, it means it works! One is from the replaced `resources.assets` and the other from this mod.

Once you confirm it works, you can replace the Indonesian `resources.assets` back with the original.