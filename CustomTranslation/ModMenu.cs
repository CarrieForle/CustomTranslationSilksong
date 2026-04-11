using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;
using Silksong.ModMenu.Elements;
using Silksong.ModMenu.Plugin;
using Silksong.ModMenu.Screens;
using TeamCherry.Localization;
using UnityEngine.UI;
using static CustomTranslation.DirectoryHelper;

namespace CustomTranslation;

public partial class CustomTranslationPlugin : IModMenuCustomMenu
{
	public LocalizedText ModMenuName()
	{
		return Text.Localized("NAME");
	}

	public AbstractMenuScreen BuildCustomMenu()
	{
		var menu = new SimpleMenuScreen(Text.Localized("NAME"));
		var openDirectoryBtn = new TextButton(Text.Localized("OPTION_OPEN_TRANSLATION_DIRECTORY"));
		openDirectoryBtn.OnSubmit += () =>
		{
			bool isOpened = false;

			try
			{
				if (languageReader.TryGetValue(Language._currentLanguage, out var translation))
				{
					var targetDir = translation.entry.location;
					Process.Start(targetDir.FullName);
					isOpened = true;
					logger.LogInfo($"Opened translation directory \"{targetDir.FullName}\"");
				}
				else
				{
					isOpened = false;
				}
			}
			catch
			{
				isOpened = false;
			}

			if (!isOpened)
			{
				var targetDir = Create(translationDir);
				Process.Start(targetDir.FullName);
				logger.LogInfo($"Opened translation directory \"{targetDir.FullName}\"");
			}
		};

		menu.Add(openDirectoryBtn);

		var reloadBtn = new TextButton(
			Text.Localized("OPTION_RELOAD_TRANSLATION"),
			Text.Localized("OPTION_DESCRIPTION_RELOAD_TRANSLATION")
		);
		reloadBtn.OnSubmit += () =>
		{
			logger.LogInfo("Reloading translation");
			var languageOption = UIManager.instance.transform.Find("UICanvas/GameOptionsMenuScreen/Content/LanguageSetting/LanguageOption");

			if (languageOption.TryGetComponent<MenuLanguageSetting>(out var menuLanguageSetting))
			{
				RefreshLanguage();
				Language.LoadAvailableLanguages();
				MenuLanguageSetting.UpdateLangsArray();

				var optionIndex = MenuLanguageSetting.optionList.IndexOf(Language._currentLanguage.ToString());
				if (optionIndex != -1)
				{
					menuLanguageSetting.SetOptionTo(optionIndex);
				}
				else
				{
					Logger.LogWarning($"Unable to load \"{Language._currentLanguage}\". Fallback to EN.");
					menuLanguageSetting.SetOptionTo(MenuLanguageSetting.optionList.IndexOf(LanguageCode.EN.ToString()));
				}

				menuLanguageSetting.UpdateLanguageSetting();

				foreach (var cfbl in FindObjectsByType<ChangeFontByLanguage>(UnityEngine.FindObjectsSortMode.None))
				{
					cfbl.SetFont();
				}

				Logger.LogInfo("Reloaded translation");
			}
		};

		menu.Add(reloadBtn);

		var dumpBtn = new TextButton(Text.Localized("OPTION_EXPORT_TRANSLATION"));
		dumpBtn.OnSubmit += () =>
		{
			var lang = Language._currentLanguage.ToString();
			logger.LogInfo($"Exporting: {lang}");
			var saveDir = Create(dir, "export", lang);
			var tmpDir = new DirectoryInfo(Path.GetTempPath());

			var entryDir = Create(saveDir, "entry");
			using var entrySw = new StreamWriter(Path.Combine(entryDir.FullName, ENTRY_FILENAME));
			using var entryTw = new JsonTextWriter(entrySw);
			var serializer = JsonSerializer.Create(new JsonSerializerSettings
			{
				Formatting = Formatting.Indented
			});

			// i18n has included their sheets, so we exclude it.
			Dictionary<string, Dictionary<string, string>> vanillaCurrentEntrySheets = [];

			foreach (string sheet in Language.Settings.sheetTitles)
			{
				// Sheets not in resources.asset will be empty,
				// so we exclude them
				// this includes "Hornet Old" and "Deprecated" in ZH_TW.
				if (Language._currentEntrySheets.TryGetValue(sheet, out var dict)
					&& !dict.IsNullOrEmpty())
				{
					vanillaCurrentEntrySheets[sheet] = dict;
				}
			}

			serializer.Serialize(entryTw, vanillaCurrentEntrySheets);

			var sheetDir = Create(saveDir, "sheet");
			foreach (var (sheetName, sheet) in vanillaCurrentEntrySheets)
			{
				using var sheetSw = new StreamWriter(Path.Combine(sheetDir.FullName, $"{lang}_{sheetName}.txt"));

				sheetSw.WriteLine("<entries>");
				foreach (var (key, text) in sheet)
				{
					sheetSw.WriteLine($"<entry name=\"{key}\">{Text.EscapeXml(text)}</entry>");
				}
				sheetSw.WriteLine("</entries>");
			}

			var metadataPath = new FileInfo(Path.Combine(tmpDir.FullName, METADATA_FILENAME));
			using (var metadataSw = new StreamWriter(metadataPath.FullName))
			using (var metadataTw = new JsonTextWriter(metadataSw))
			{
				serializer.Serialize(metadataTw, new Dictionary<string, string>
				{
					["Language"] = lang,
				});
			}

			metadataPath.CopyTo(Path.Combine(sheetDir.FullName, metadataPath.Name), true);
			metadataPath.CopyTo(Path.Combine(entryDir.FullName, metadataPath.Name), true);

			logger.LogInfo($"Exported \"{lang}\"");
			Process.Start(saveDir.FullName);
		};

		menu.Add(dumpBtn);

		return menu;
	}
}