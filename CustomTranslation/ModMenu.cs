using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using GlobalEnums;
using Newtonsoft.Json;
using Silksong.ModMenu.Elements;
using Silksong.ModMenu.Plugin;
using Silksong.ModMenu.Screens;
using TeamCherry.Localization;
using UnityEngine.UI;
using static CustomTranslation.DirectoryHelper;

namespace CustomTranslation;

public partial class CustomTranslationPlugin : IModMenuCustomElement
{
	public string ModMenuName()
	{
		return Name;
	}

	public SelectableElement BuildCustomElement()
	{
		SimpleMenuScreen menu = new(Name);
		var openDirectoryBtn = new TextButton(Text.Localized("OPTION_OPEN_TRANSLATION_DIRECTORY"));
		openDirectoryBtn.OnSubmit += () =>
		{
			Process.Start(translationDir.ToString());
			logger.LogInfo($"Opened translation directory \"{translationDir.FullName}\"");
		};

		menu.Add(openDirectoryBtn);

		var reloadBtn = new TextButton(Text.Localized("OPTION_RELOAD_TRANSLATION"));
		reloadBtn.OnSubmit += () =>
		{
			logger.LogInfo("Reloading translation");
			var uiManager = UIManager.instance;
			if (uiManager == null)
			{
				logger.LogError("UI Manager is not found");
				return;
			}

			var languageOption = uiManager.transform.Find("UICanvas/GameOptionsMenuScreen/Content/LanguageSetting/LanguageOption");
			if (languageOption == null)
			{
				logger.LogError("LanguageOption is not found");
				return;
			}

			if (languageOption.TryGetComponent<MenuLanguageSetting>(out var menuLanguageSetting))
			{
				RefreshLanguage();
				MenuLanguageSetting.UpdateLangsArray();
				if (!languageReader.ContainsKey(Language._currentLanguage))
				{
					Logger.LogWarning($"Unable to load \"{Language._currentLanguage}\". Fallback to EN.");
					menuLanguageSetting.SetOptionTo(Array.IndexOf(MenuLanguageSetting.langs, SupportedLanguages.EN));
				}
				menuLanguageSetting.UpdateLanguageSetting();
				Logger.LogInfo("Reloaded translation");
			}
		};

		menu.Add(reloadBtn);

		var dumpBtn = new TextButton(Text.Localized("OPTION_EXPORT_TRANSLATION"));
		dumpBtn.OnSubmit += () =>
		{
			var lang = Language._currentLanguage.ToString();
			logger.LogInfo($"Exporting: {lang}");
			var saveDir = new DirectoryInfo(Path.Combine(Path.GetDirectoryName(Info.Location), "export", lang));
			CreateRecursive(saveDir);

			using var entrySw = new StreamWriter(Path.Combine(saveDir.FullName, "entry.json"));
			using var entryTw = new JsonTextWriter(entrySw);
			var serializer = JsonSerializer.Create(new JsonSerializerSettings
			{
				Formatting = Formatting.Indented
			});

			serializer.Serialize(entryTw, Language._currentEntrySheets);

			foreach (var (sheetName, sheet) in Language._currentEntrySheets)
			{
				using var sheetSw = new StreamWriter(Path.Combine(saveDir.FullName, $"{lang}_{sheetName}.txt"));

				sheetSw.WriteLine("<entries>");
				foreach (var (key, text) in sheet)
				{
					sheetSw.WriteLine($"<entry name=\"{key}\">{Text.EscapeXml(text)}</entry>");
				}
				sheetSw.WriteLine("</entries>");
			}

			using var metadataSw = new StreamWriter(Path.Combine(saveDir.FullName, "metadata.json"));
			using var metadataTw = new JsonTextWriter(metadataSw);
			serializer.Serialize(metadataTw, new Dictionary<string, string>
			{
				["Language"] = lang,
			});

			logger.LogInfo($"Exported \"{lang}\"");
			Process.Start(saveDir.Parent.FullName);
		};

		menu.Add(dumpBtn);

		var menuBtn = new TextButton(Name);
		menuBtn.OnSubmit += () =>
		{
			MenuScreenNavigation.Show(menu);
		};

		return menuBtn;
	}
}