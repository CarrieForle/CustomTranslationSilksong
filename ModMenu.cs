using System.Diagnostics;
using Silksong.ModMenu.Elements;
using Silksong.ModMenu.Plugin;
using Silksong.ModMenu.Screens;

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

		var menuBtn = new TextButton(Name);
		menuBtn.OnSubmit += () =>
		{
			MenuScreenNavigation.Show(menu);
		};

		return menuBtn;
	}
}