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

namespace CustomTranslation;

public enum TranslationFileKind
{
	Single,
	Splitted,
	SplittedEncrypted,
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
	internal static ManualLogSource logger;
	private GlobalData globalData;
	public GlobalData? GlobalData
	{
		get
		{
			globalData ??= new();
			return globalData;
		}
		set => globalData = value!;
	}
	public static CustomTranslationPlugin Instance;

	private void Awake()
	{
		Harmony.CreateAndPatchAll(typeof(Patch), Id);
		logger = Logger;
		translationDir = new DirectoryInfo(Path.Combine(Path.GetDirectoryName(Info.Location), "translation"));
		Instance = this;

		try
		{
			translationDir.Create();
		}
		catch (IOException)
		{

		}

		var entries = GetTranslationEntries();

		foreach (var entry in entries)
		{
			try
			{
				var metadata = TranslationMetadata.ReadFrom(entry);
				var translation = new Translation(metadata, entry);

				if (languageReader.ContainsKey(metadata.Language))
				{
					logger.LogWarning($"Found duplicate entries for '{metadata.Language}' ('{languageReader[metadata.Language].entry.location.Name}' and '{entry.location.Name}'). Use '{entry.location.Name}'.");
				}

				languageReader[metadata.Language] = translation;
			}
			catch (Exception e)
			{
				logger.LogWarning($"Failed to load entry at '{entry.location.Name}': {e.Message}");
			}
		}

		if (entries.Count == 0)
		{
			Logger.LogInfo("No entry loaded.");
		}
		else
		{
			Logger.LogInfo($"Found {entries.Count} entries. Loaded {languageReader.Count} entries: {string.Join(", ", languageReader.LanguageList)}");
		}
	}

	private void Start()
	{
		GlobalData ??= new GlobalData();
	}

	private IList<TranslationEntry> GetTranslationEntries()
	{
		List<TranslationEntry> res = [];
		var dirs = translationDir.GetDirectories();
		foreach (var dir in dirs)
		{
			if (!File.Exists(Path.Combine(dir.FullName, METADATA_FILENAME)))
			{
				continue;
			}

			if (File.Exists(Path.Combine(dir.FullName, ENTRY_FILENAME)))
			{
				res.Add(new TranslationEntry
				{
					location = dir,
					kind = TranslationFileKind.Single,
				});
				continue;
			}

			var files = dir.GetFiles();
			if (Language.Settings.sheetTitles.Any(
				sheet => files.Any(f => Regex.IsMatch(f.Name, @$"[a-zA-Z_]+?{sheet}\.txt"))
			))
			{
				res.Add(new TranslationEntry
				{
					location = dir,
					kind = TranslationFileKind.Splitted,
				});
			}
			else if (Language.Settings.sheetTitles.Any(
				sheet => files.Any(f => Regex.IsMatch(f.Name, @$"^[a-zA-Z_]+?{sheet}\.bytes"))
			))
			{
				res.Add(new TranslationEntry
				{
					location = dir,
					kind = TranslationFileKind.SplittedEncrypted,
				});
			}
		}

		return res;
	}
}

public class GlobalData
{
	[JsonConverter(typeof(StringEnumConverter))]
	public LanguageCode Language;
}

public record TranslationEntry
{
	public DirectoryInfo location;
	public TranslationFileKind kind;
}

public class TranslationMetadata
{
	[JsonConverter(typeof(LanguageCodeConverter))]
	public LanguageCode Language { get; set; }

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
		try
		{
			if (entry.kind == TranslationFileKind.Single)
			{
				var fileContent = Text.FromJson<Dictionary<string, Dictionary<string, string>>>(Path.Combine(entry.location.FullName, ENTRY_FILENAME));

				if (fileContent is null)
				{
					throw new CustomTranslationException("JSON is null");
				}

				foreach (string sheet in Language.Settings.sheetTitles)
				{
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
				foreach (string sheet in Language.Settings.sheetTitles)
				{
					if (!Language._currentEntrySheets.TryGetValue(sheet, out var sheetDict))
					{
						sheetDict = [];
						Language._currentEntrySheets[sheet] = sheetDict;
					}

					Regex pattern;
					if (entry.kind == TranslationFileKind.Splitted)
					{
						pattern = new Regex(@$"[a-zA-Z_]+?{sheet}.txt");
					}
					else
					{
						pattern = new Regex(@$"[a-zA-Z_]+?{sheet}.bytes");
					}

					FileInfo? filename = files.FirstOrDefault(f => pattern.IsMatch(f.Name));

					using var sr = new StreamReader(Path.Combine(entry.location.FullName, filename.Name));
					string fileContent;
					if (entry.kind == TranslationFileKind.Splitted)
					{
						fileContent = sr.ReadToEnd();
					}
					else
					{
						fileContent = Encryption.Decrypt(sr.ReadToEnd());
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
			logger.LogError($"Error while loading \"{entry.location.Name}\" entry: {ex.Message}");
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
}

class EnumComparer<T> : Comparer<T>
where T : Enum
{
	public override int Compare(T x, T y)
	{
		return x.CompareTo(y);
	}
}

#pragma warning disable HARMONIZE003
class Patch
{
	static string[]? originalOptionList;

	public static void UpdateAvailableLangauages()
	{
		logger.LogDebug("Patched available languages");

		foreach (var lang in languageReader.LanguageList)
		{
			Language._availableLanguages.AddIfNotPresent(lang.ToString());
		}
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(Language), nameof(Language.DoSwitch))]
	static bool DoSwitch(LanguageCode newLang)
	{
		TeamCherry.Localization.LocalizationProjectSettings.OnSwitchedLanguage(newLang);
		Language._currentLanguage = newLang;
		Language._currentEntrySheets = new Dictionary<string, Dictionary<string, string>>();

		if (languageReader.ContainsKey(newLang))
		{
			Language._currentLanguage = languageReader[newLang].metadata.FallbackLanguage;
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

		return false;
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(Language), nameof(Language.SwitchLanguage), [typeof(LanguageCode)])]
	static bool SwitchLanguage(LanguageCode code, ref bool __result)
	{
		if (languageReader.ContainsKey(code))
		{
			__result = true;
			Language.DoSwitch(code);
			return false;
		}

		return true;
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
	static void LoadAvailableLanguages()
	{
		UpdateAvailableLangauages();
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(Language), nameof(Language.RestoreLanguageSelection))]
	static void RestoreLanguageSelection()
	{
		UpdateAvailableLangauages();
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(MenuLanguageSetting), nameof(MenuLanguageSetting.UpdateLangsArray))]
	static void UpdateLangsArray()
	{
		logger.LogDebug("Patched language array");
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
		if (__instance.selectedOptionIndex < MenuLanguageSetting.langs.Length)
		{
			GameManager.instance.gameSettings.gameLanguage = MenuLanguageSetting.langs[__instance.selectedOptionIndex];
			Instance.GlobalData?.Language = (LanguageCode)MenuLanguageSetting.langs[__instance.selectedOptionIndex];
		}
		else
		{
			GameManager.instance.gameSettings.gameLanguage = GlobalEnums.SupportedLanguages.EN;
			Instance.GlobalData?.Language = languageReader.LanguageList[__instance.selectedOptionIndex - MenuLanguageSetting.langs.Length];
		}

		Language.SwitchLanguage(Instance.GlobalData!.Language);
		// __instance.gm is not initialized until the language menu is opened once, but this might be called from modmenu before it happened.
		GameManager.instance.RefreshLocalization(); 
		__instance.UpdateText();

		return false;
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(Language), nameof(Language.RestoreLanguageSelection))]
	static void RestoreLanguageFromGlobalData(ref string __result)
	{
		if (Instance.GlobalData?.Language is { } lang)
		{
			__result = lang.ToString();
		}

		logger.LogInfo($"Loaded language: {__result}");
	}

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
				__instance.currentActiveIndex = index;
			}
		}
	}
}
#pragma warning restore HARMONIZE003