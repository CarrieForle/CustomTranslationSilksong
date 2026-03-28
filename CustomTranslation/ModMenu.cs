using System.Diagnostics;
using Silksong.ModMenu.Elements;
using Silksong.ModMenu.Plugin;
using Silksong.ModMenu.Screens;
using TeamCherry.Localization;
using UnityEngine.UI;

namespace CustomTranslation;

public partial class CustomTranslationPlugin: IModMenuCustomElement
{
	public string ModMenuName()
	{
		return "Custom Translation";
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

		var menuBtn = new TextButton(Name);
		menuBtn.OnSubmit += () =>
		{
			MenuScreenNavigation.Show(menu);
		};

		return menuBtn;
	}
}