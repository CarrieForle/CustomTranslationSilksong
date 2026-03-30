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
		};

		menu.Add(openDirectoryBtn);

		var reloadBtn = new TextButton(Text.Localized("OPTION_RELOAD_TRANSLATION"));
		reloadBtn.OnSubmit += () =>
		{
			var uiManager = UIManager.instance;
			if (uiManager == null)
			{
				return;
			}

			var languageOption = uiManager.transform.Find("UICanvas/GameOptionsMenuScreen/Content/LanguageSetting/LanguageOption");
			if (languageOption == null)
			{
				return;
			}

			if (languageOption.TryGetComponent<MenuLanguageSetting>(out var menuLanguageSetting))
			{
				menuLanguageSetting.UpdateLanguageSetting();
			}
		};

		menu.Add(reloadBtn);

		var dumpBtn = new TextButton(Text.Localized("OPTION_EXPORT_TRANSLATION"));
		dumpBtn.OnSubmit += () =>
		{
			var lang = Language._currentLanguage.ToString();
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