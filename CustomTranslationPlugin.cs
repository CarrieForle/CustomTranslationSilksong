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
[BepInDependency("org.silksong-modding.datamanager")]
[BepInAutoPlugin(id: "io.github.carrieforle.customtranslation")]
public partial class CustomTranslationPlugin : BaseUnityPlugin, IGlobalDataMod<GlobalData>
{
	public const string ENTRY_FILENAME = "entry.txt";
	public const string METADATA_FILENAME = "metadata.json";
	public static LanguageReader languageReader = new();
	private DirectoryInfo translationDir;
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
				languageReader[metadata.Language] = translation;
			}
			catch (Exception e)
			{
				logger.LogWarning($"Failed to load entry at \"{entry.location.Name}\": {e.Message}");
			}
		}

		if (entries.Count == 0)
		{
			Logger.LogInfo("No entry loaded.");
		}
		else
		{
			Logger.LogInfo($"Found {entries.Count} entries: {string.Join(", ", languageReader.LanguageList)}");
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

public class CustomTranslationException : Exception
{
	public CustomTranslationException(string message)
		: base(message)
	{

	}
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

			var sr = new StreamReader(Path.Combine(entry.location.FullName, filename.Name));
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
		logger.LogDebug("Patched available languages");
		var languages = languageReader.LanguageList
			.Select(lang => lang.ToString());
		Language._availableLanguages.AddRange(languages);
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(MenuLanguageSetting), nameof(MenuLanguageSetting.UpdateLangsArray))]
	static void UpdateLangsArray()
	{
		logger.LogDebug("Patched language array");
		originalOptionList ??= (string[])MenuLanguageSetting.optionList.Clone();

		MenuLanguageSetting.optionList = [
			.. originalOptionList,
			.. languageReader.LanguageList
				.Select(lang => lang.ToString())
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

		logger.LogInfo($"Loaded Language: {__result}");
	}
}
#pragma warning restore HARMONIZE003