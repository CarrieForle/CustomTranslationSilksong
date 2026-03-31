using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Silksong.DataManager;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using TeamCherry.Localization;
using TeamCherry.SharedUtils;
using UnityEngine.UI;
using static CustomTranslation.CustomTranslationPlugin;
using static CustomTranslation.DirectoryHelper;

namespace CustomTranslation;

public enum TranslationFileKind
{
	Single,
	Splitted
}

/// <summary>
/// There are two enums for "supported" language:
/// - LanguageCode
/// - SupportedLanguages (subset of LanguageCode, contain officially supported languages)
/// 
/// Localization codes mostly work with LanguageCode which allows us to work with unsupported language. SupportedLangauges work with saved option and UI.
/// 
/// </summary>
[BepInDependency("org.silksong-modding.modmenu")]
[BepInDependency("org.silksong-modding.datamanager")]
[BepInDependency("org.silksong-modding.i18n")]
[BepInAutoPlugin(id: "io.github.carrieforle.customtranslation", name: "Custom Translation")]
public partial class CustomTranslationPlugin : BaseUnityPlugin, IGlobalDataMod<GlobalData>
{
	public const string ENTRY_FILENAME = "entry.json";
	public const string METADATA_FILENAME = "metadata.json";
	public static LanguageReader languageReader = new();
	public static DirectoryInfo translationDir;
	public static DirectoryInfo dir;
	internal static ManualLogSource logger;
	public static CustomTranslationPlugin Instance;
	private Harmony harmony;
	private GlobalData? globalData;
	public GlobalData? GlobalData
	{
		get
		{
			globalData ??= new();
			return globalData;
		}
		set => globalData = value!;
	}

	private void Awake()
	{
		harmony = new Harmony(Id);
		harmony.PatchAll(typeof(Patch));
		logger = Logger;
		dir = new DirectoryInfo(Path.GetDirectoryName(Info.Location));
		translationDir = TryCreateRecursive(dir, "translation");
		Instance = this;

		RefreshLanguage();
	}

	private void Start()
	{
		// Language class cannot be patched in Awake() because it
		// would've been too early to call its static constructor
		// which causes gibberish text in the intro scene.
		harmony.PatchAll(typeof(LanguagePatch));
		Logger.LogInfo("Patched Language class");

		// remove error "Loaded saved language code 'XX' is not an
		// available language". There is still one same error
		// that happens before patching.
		LanguagePatch.LoadAvailableLanguages();

		// Apply LanguagePatch here will miss Language.DoSwitch()
		// on game launch and cause the game not using the language 
		// during intro, so we force the game to switch language here.
		if (GlobalData?.Language is {} lang && languageReader.ContainsKey(lang))
		{
			Language.DoSwitch(lang);
		}
	}

	private static IList<TranslationEntry> GetTranslationEntries()
	{
		List<TranslationEntry> res = [];
		var dirs = TryCreate(translationDir).GetDirectories();
		string[] validExtensions = [".txt", ".bytes"];
		foreach (var dir in dirs)
		{
			if (!File.Exists(Path.Combine(dir.FullName, METADATA_FILENAME)))
			{
				continue;
			}

			if (File.Exists(Path.Combine(dir.FullName, ENTRY_FILENAME)))
			{
				res.Add(new TranslationEntry(dir, TranslationFileKind.Single));
				continue;
			}

			var files = dir.GetFiles();
			if (files.Any(f => validExtensions.IndexOf(f.Extension) != -1))
			{
				res.Add(new TranslationEntry(dir, TranslationFileKind.Splitted));
			}
		}

		return res;
	}

	public static void RefreshLanguage()
	{
		var entries = GetTranslationEntries();
		languageReader = new ();

		foreach (var entry in entries)
		{
			try
			{
				var metadata = TranslationMetadata.ReadFrom(entry);
				var translation = new Translation(metadata, entry);

				if (languageReader.TryGetValue(metadata.Language, out var duplicated))
				{
					logger.LogWarning($"Found duplicate entries for '{metadata.Language}' ('{duplicated.entry.Name}' and '{entry.Name}'). Use '{entry.Name}'.");
				}

				languageReader[metadata.Language] = translation;
			}
			catch (Exception e)
			{
				logger.LogWarning($"Failed to load entry at '{entry.Name}': {e.Message}");
			}
		}

		if (entries.Count == 0)
		{
			logger.LogInfo("No entry found.");
		}
		else
		{
			logger.LogInfo($"Found {entries.Count} entries. Loaded {languageReader.Count} entries: {string.Join(", ", languageReader.LanguageList)}");
		}
	}
}

public class GlobalData
{
	[JsonConverter(typeof(StringEnumConverter))]
	public LanguageCode Language;
}

public record TranslationEntry(DirectoryInfo location, TranslationFileKind kind)
{
	public DirectoryInfo location = location;
	public TranslationFileKind kind = kind;
	public string Name => location.Name;
}

public class TranslationMetadata
{
	[JsonProperty(Required = Required.Always)]
	[JsonConverter(typeof(LanguageCodeConverter))]
	public LanguageCode Language { get; set; }

	[JsonProperty(Required = Required.DisallowNull)]
	[JsonConverter(typeof(LanguageCodeConverter))]
	public LanguageCode FallbackLanguage { get; set; } = LanguageCode.EN;

	[JsonConstructor]
	public TranslationMetadata(LanguageCode language)
	{
		Language = language;
	}

	public static TranslationMetadata ReadFrom(TranslationEntry entry)
	{
		var res = Text.FromJson<TranslationMetadata>(Path.Combine(entry.location.FullName, METADATA_FILENAME));

		if (res is null || res.Language == LanguageCode.N)
		{
			throw new CustomTranslationException("Invalid language code");
		}

		return res;
	}
}

public class Translation(TranslationMetadata metadata, TranslationEntry entry)
{
	public readonly TranslationMetadata metadata = metadata;
	public readonly TranslationEntry entry = entry;

	public bool UpdateSheet()
	{
		string sheet = "";
		try
		{
			if (entry.kind == TranslationFileKind.Single)
			{
				var fileContent = Text.FromJson<Dictionary<string, Dictionary<string, string>>>(Path.Combine(entry.location.FullName, ENTRY_FILENAME));

				if (fileContent is null)
				{
					throw new CustomTranslationException("JSON is null");
				}

				foreach (string sheet_ in Language.Settings.sheetTitles)
				{
					sheet = sheet_;
					if (!Language._currentEntrySheets.TryGetValue(sheet, out var sheetDict))
					{
						sheetDict = [];
						Language._currentEntrySheets[sheet] = sheetDict;
					}
					if (fileContent.TryGetValue(sheet, out var sheetContent))
					{
						foreach ((string key, string text) in sheetContent)
						{
							sheetDict[key] = text;
						}
					}
				}
			}
			else
			{
				var files = entry.location.GetFiles();
				foreach (string sheet_ in Language.Settings.sheetTitles)
				{
					sheet = sheet_;
					if (!Language._currentEntrySheets.TryGetValue(sheet, out var sheetDict))
					{
						sheetDict = [];
						Language._currentEntrySheets[sheet] = sheetDict;
					}

					var pattern = new Regex(@$"[a-zA-Z_]+?{sheet}\.(?:txt|bytes)");

					FileInfo? filename = files.FirstOrDefault(f => pattern.IsMatch(f.Name));

					if (filename is null)
					{
						continue;
					}

					using var sr = new StreamReader(Path.Combine(entry.location.FullName, filename.Name));
					string fileContent = sr.ReadToEnd();
					if (!fileContent.Contains('<'))
					{
						fileContent = Encryption.Decrypt(fileContent);
					}

					using var xmlReader = XmlReader.Create(new StringReader(fileContent));
					while (xmlReader.ReadToFollowing("entry"))
					{
						xmlReader.MoveToFirstAttribute();
						string value = xmlReader.Value;
						xmlReader.MoveToElement();
						string text2 = xmlReader.ReadElementContentAsString().Trim();
						text2 = text2.UnescapeXml();
						sheetDict[value] = text2;
					}
				}
			}

			return true;
		}
		catch (Exception ex)
		{
			logger.LogError($"Error while loading \"{entry.Name}\" entry on sheet \"{sheet}\": {ex.Message}");
			return false;
		}
	}
}

public class LanguageReader
{
	private readonly SortedList<LanguageCode, Translation> languages = new(EnumComparer<LanguageCode>.Default);
	public IList<LanguageCode> LanguageList => languages.Keys;
	public int Count => languages.Count;

	public Translation this[LanguageCode lang]
	{
		get => languages[lang];
		set => languages[lang] = value;
	}

	public bool ContainsKey(LanguageCode key)
	{
		return languages.ContainsKey(key);
	}

	public bool TryGetValue(LanguageCode key, out Translation translation)
	{
		return languages.TryGetValue(key, out translation);
	}
}

#pragma warning disable HARMONIZE003
class EnumComparer<T> : Comparer<T>
where T : Enum
{
	public override int Compare(T x, T y)
	{
		return x.CompareTo(y);
	}
}

class LanguagePatch
{
	[HarmonyPrefix]
	[HarmonyPatch(typeof(Language), nameof(Language.DoSwitch))]
	static bool DoSwitch(LanguageCode newLang)
	{
		TeamCherry.Localization.LocalizationProjectSettings.OnSwitchedLanguage(newLang);
		Language._currentLanguage = newLang;
		Language._currentEntrySheets = new Dictionary<string, Dictionary<string, string>>();

		if (languageReader.TryGetValue(newLang, out var translation))
		{
			Language._currentLanguage = translation.metadata.FallbackLanguage;
		}

		foreach (string text in Language.Settings.sheetTitles)
		{
			Language._currentEntrySheets[text] = new Dictionary<string, string>();
			string languageFileContents = Language.GetLanguageFileContents(text);
			if (!string.IsNullOrEmpty(languageFileContents))
			{
				using (XmlReader xmlReader = XmlReader.Create(new StringReader(languageFileContents)))
				{
					while (xmlReader.ReadToFollowing("entry"))
					{
						xmlReader.MoveToFirstAttribute();
						string value = xmlReader.Value;
						xmlReader.MoveToElement();
						string text2 = xmlReader.ReadElementContentAsString().Trim();
						text2 = text2.UnescapeXml();
						Language._currentEntrySheets[text][value] = text2;
					}
				}
			}
		}

		if (languageReader.ContainsKey(newLang))
		{
			languageReader[newLang].UpdateSheet();
		}

		Language._currentLanguage = newLang;
		LocalizedAsset[] array = (LocalizedAsset[])UnityEngine.Object.FindObjectsOfType(typeof(LocalizedAsset));
		for (int i = 0; i < array.Length; i++)
		{
			array[i].LocalizeAsset();
		}
		Language.SendMonoMessage("ChangedLanguage", new object[] { Language._currentLanguage });
		Instance.GlobalData?.Language = newLang;

		return false;
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(Language), nameof(Language.GetLanguages))]
	static void GetLanguages(ref string[] __result)
	{
		__result = [.. __result, .. languageReader.LanguageList
			.Select(lang => lang.ToString())];
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(Language), nameof(Language.LoadAvailableLanguages))]
	public static void LoadAvailableLanguages()
	{
		foreach (var lang in languageReader.LanguageList)
		{
			Language._availableLanguages.AddIfNotPresent(lang.ToString());
		}
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(Language), nameof(Language.RestoreLanguageSelection))]
	static void RestoreLanguageFromGlobalData(ref string __result)
	{
		if (Instance.GlobalData?.Language is { } lang)
		{
			__result = lang.ToString();
		}

		logger.LogInfo($"Restored language: {__result}");
	}
}

class Patch
{
	static string[]? originalOptionList;

	[HarmonyPostfix]
	[HarmonyPatch(typeof(MenuLanguageSetting), nameof(MenuLanguageSetting.UpdateLangsArray))]
	static void UpdateLangsArray()
	{
		originalOptionList ??= [.. MenuLanguageSetting.optionList];

		MenuLanguageSetting.optionList = [
			.. originalOptionList,
			.. languageReader.LanguageList
				.Select(lang => lang.ToString())
				.Where(lang => !originalOptionList.Any(og_lang => lang == og_lang))
		];
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(MenuLanguageSetting), nameof(MenuLanguageSetting.UpdateLanguageSetting))]
	static bool UpdateLanguageSetting(MenuLanguageSetting __instance)
	{
		LanguageCode lang;
		if (__instance.selectedOptionIndex < MenuLanguageSetting.langs.Length)
		{
			GameManager.instance.gameSettings.gameLanguage = MenuLanguageSetting.langs[__instance.selectedOptionIndex];
			lang = (LanguageCode)MenuLanguageSetting.langs[__instance.selectedOptionIndex];
		}
		else
		{
			GameManager.instance.gameSettings.gameLanguage = GlobalEnums.SupportedLanguages.EN;
			lang = languageReader.LanguageList[__instance.selectedOptionIndex - MenuLanguageSetting.langs.Length];
		}

		Language.SwitchLanguage(lang);
		// __instance.gm is not initialized until the language menu is opened once, but this might be called from modmenu before it happened.
		GameManager.instance.RefreshLocalization();
		__instance.UpdateText();

		return false;
	}

	// An error of "Couldn't find currently active language" is logged from this
	// function. This is normal and harmless.
	[HarmonyPostfix]
	[HarmonyPatch(typeof(MenuLanguageSetting), nameof(MenuLanguageSetting.RefreshCurrentIndex))]
	static void PatchCurrentIndex(MenuLanguageSetting __instance)
	{
		if (!MenuLanguageSetting.languageCodeToSupportedLanguages.ContainsKey(Language._currentLanguage) &&
			languageReader.ContainsKey(Language._currentLanguage))
		{
			int index = MenuLanguageSetting.optionList.IndexOf(Language._currentLanguage.ToString());
			if (index != -1)
			{
				__instance.selectedOptionIndex = index;
			}
		}
	}
}
#pragma warning restore HARMONIZE003