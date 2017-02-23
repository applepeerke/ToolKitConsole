using System;
namespace GeneralUtilities
{
	public static class Enums
	{
		public enum YN
		{
			Y,
			N,
			C
		}
		public enum QAType
		{
			Str,
			Dir,
			File,
			YN,
			ListValue
		}

		// Extensions
		public static T ToEnum<T>(this string value)
		{
			try
			{
				return (T)Enum.Parse(typeof(T), value, true);
			}
			catch (Exception e)
			{
				throw e;
			}
		}
		public static T ToEnum<T>(this string value, T defaultValue)
		{
			if (string.IsNullOrEmpty(value))
			{
				return defaultValue;
			}
			try
			{
				return (T)Enum.Parse(typeof(T), value, true);
			}
			catch (Exception) 
			{ 
				return defaultValue; 
			}
		}
	}
}
