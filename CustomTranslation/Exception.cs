using System;

namespace CustomTranslation;
public class CustomTranslationException : Exception
{
	public CustomTranslationException(string message)
		: base(message)
	{

	}
}