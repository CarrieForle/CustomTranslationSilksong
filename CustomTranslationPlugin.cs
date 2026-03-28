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
	public const string ENTRY_FILENAME = "entry.txt";
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

		var entries = ListTranslationDirectories();

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

	private IList<TranslationEntry> ListTranslationDirectories()
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

	[JsonConverter(typeof(StringEnumConverter))]
	public LanguageCode Language { get; set; }

	[JsonConstructor]
	public TranslationMetadata(LanguageCode language)
	{
		Language = language;
	}

	public static TranslationMetadata ReadFrom(TranslationEntry entry)
	{
		using var sr = new StreamReader(Path.Combine(entry.location.FullName, METADATA_FILENAME));
		var res = JsonConvert.DeserializeObject<TranslationMetadata>(sr.ReadToEnd());

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

	public string LoadTranslationText(string sheet)
	{
		if (entry.kind == TranslationFileKind.Single)
		{
			using var sr = new StreamReader(Path.Combine(entry.location.FullName, ENTRY_FILENAME));
			return sr.ReadToEnd();
		}
		else
		{
			var files = entry.location.GetFiles();
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

			if (filename is null)
			{
				return "";
			}

			using var sr = new StreamReader(Path.Combine(entry.location.FullName, filename.Name));
			if (entry.kind == TranslationFileKind.Splitted)
			{
				return sr.ReadToEnd();
			}
			else
			{
				return Encryption.Decrypt(sr.ReadToEnd());
			}
		}
	}
}

public class LanguageReader
{
	private readonly SortedList<LanguageCode, Translation> languages = new(EnumComparer<LanguageCode>.Default);
	private readonly Dictionary<LanguageCode, bool> isLanguageRead = [];
	public IList<LanguageCode> LanguageList => languages.Keys;
	public int Count => languages.Count;

	public Translation this[LanguageCode lang]
	{
		get => languages[lang];
		set
		{
			languages[lang] = value;
			isLanguageRead[lang] = false;
		}
	}

	public bool ContainsKey(LanguageCode key)
	{
		return languages.ContainsKey(key);
	}

	public string Load(LanguageCode lang, string sheet)
	{
		// Language.GetLanguageFileContents loops reading via sheet.
		// So we don't over-read it.
		if (languages[lang].entry.kind == TranslationFileKind.Single && isLanguageRead[lang])
		{
			return "";
		}

		return languages[lang].LoadTranslationText(sheet);
	}

	public void ResetRead()
	{
		foreach (var lang in isLanguageRead.Keys.ToArray())
		{
			isLanguageRead[lang] = false;
		}
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
	[HarmonyPatch(typeof(Language), nameof(Language.GetLanguageFileContents))]
	static bool GetLanguageFileContents(string sheetTitle, ref string __result)
	{
		if (languageReader.ContainsKey(Language._currentLanguage))
		{
			__result = languageReader.Load(Language._currentLanguage, sheetTitle);
			return false;
		}

		return true;
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(Language), nameof(Language.DoSwitch))]
	static void ResetRead()
	{
		languageReader.ResetRead();
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
		__instance.gm.RefreshLocalization();
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