using System;
using System.Diagnostics;
using BepInEx.Logging;
using Newtonsoft.Json.Serialization;
using TeamCherry.Localization;

namespace CustomTranslation;

public class Text
{
	public static LocalisedString Localized(string key)
	{
		return new LocalisedString($"Mods.{CustomTranslationPlugin.Id}", key);
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