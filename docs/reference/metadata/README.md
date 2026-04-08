# metadata.json

```json
{
  "Language": "EN", // Required: The language of this translation
  "FallbackLanguage": "EN", // Optional: The language to load for untranslated text (Default: EN)
  "TextFontScale": 1, // Optional: The scale of text font (e.g., 1.5 means 150% bigger). Default: 1
  "TitleFontScale": 1, // Optional: The scale of title font. Default: 1
  "UseFontAsFallBack": true, // Optional: Only use font when the original couldn't render it. 
}
```

The following fields require placing a font to function. See [asset](/docs/reference/README.md#asset)
```json
{
  "TextFontScale": 1, // Optional: The scale of text font (e.g., 1.5 means 150% bigger). Default: 1
  "TitleFontScale": 1, // Optional: The scale of title font. Default: 1
  "UseFontAsFallBack": true, // Optional: Only use font when the original couldn't render it. 
}
```

[List of valid language codes](https://github.com/SFGrenade/LanguageSupport-Repo#table-of-available-language-codes)