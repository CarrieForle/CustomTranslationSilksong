using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using TeamCherry.Localization;

namespace CustomTranslation;

public class Text
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