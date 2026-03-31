using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using BepInEx.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using TeamCherry.Localization;

namespace CustomTranslation;

public static class Text
{
	public static LocalisedString Localized(string key)
	{
		return new LocalisedString($"Mods.{CustomTranslationPlugin.Id}", key);
	}

	public static string Localized(string key, params object[] args)
	{
		return string.Format(Language.Get(key, $"Mods.{CustomTranslationPlugin.Id}"), args);
	}

	public static T? FromJson<T>(string path)
	{
		using var sr = new StreamReader(path);
		using var reader = new JsonTextReader(sr);
		var serializer = new JsonSerializer();
		return serializer.Deserialize<T>(reader);
	}

	public static string EscapeXml(string str)
	{
		var escapeCh = new Dictionary<char, string>
		{
			['\''] = "&apos;",
			['"'] = "&quot;",
			['>'] = "&gt;",
			['<'] = "&lt;",
			['&'] = "&amp;",
		};
		
		var sb = new StringBuilder(str);
		{
			for (int i = 0; i < sb.Length; i++)
			{
				if (escapeCh.TryGetValue(sb[i], out string escapeStr))
				{
					sb.Remove(i, 1);
					sb.Insert(i, escapeStr);
					i += escapeStr.Length - 1;
				}
			}
		}

		return sb.ToString();
	}
}

public static class DirectoryHelper
{
	public static DirectoryInfo TryCreateRecursive(DirectoryInfo dirRoot, string dir)
	{
		return TryCreate(new DirectoryInfo(Path.Combine(dirRoot.FullName, dir)));
	}

	public static DirectoryInfo TryCreate(DirectoryInfo dir)
	{
		dir.Refresh();
		try
		{
			dir.Create();
		}
		catch
		{
			
		}

		return dir;
		}
}

public class LanguageCodeConverter : JsonConverter
{
	public override bool CanRead { get; } = true;
	public override bool CanWrite { get; } = false;
	public override bool CanConvert(Type objectType)
	{
		return objectType == typeof(LanguageCode);
	}

	public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
	{
		throw new NotImplementedException();
	}

	public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
	{
		string? val = (string?)reader.Value;
		return Enum.GetValues(typeof(LanguageCode))
			.Cast<LanguageCode>()
			.First(lang => lang.ToString().Equals(val, StringComparison.OrdinalIgnoreCase));
	}
}

public class BepinExTraceWriter(ManualLogSource logger) : ITraceWriter
{
	public TraceLevel LevelFilter
	{
		// trace all messages. nlog can handle filtering
		get { return TraceLevel.Verbose; }
	}

	public void Trace(TraceLevel level, string message, Exception? ex)
	{
		logger.Log(GetLogLevel(level), message);

		if (ex is not null)
		{
			logger.Log(GetLogLevel(level), ex);
		}
	}

	private LogLevel GetLogLevel(TraceLevel level)
	{
		switch (level)
		{
			case TraceLevel.Error:
				return LogLevel.Error;
			case TraceLevel.Warning:
				return LogLevel.Warning;
			case TraceLevel.Info:
				return LogLevel.Info;
			case TraceLevel.Off:
				return LogLevel.None;
			default:
				return LogLevel.Message;
		}
	}
}