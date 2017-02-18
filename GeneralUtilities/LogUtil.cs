#region Imports
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using GeneralUtilities.Data;
using System.Reflection;
#endregion

namespace GeneralUtilities
{

	// 2015-05-04 PHE First version.
	public static class LogUtil
	{
		#region Definitions
		private static string configXml = string.Empty; 
		private static StreamWriter _sw;
		private static StringBuilder _p_Data;
		private static StringBuilder _currentData = new StringBuilder();
		// private StringBuilder _line;
		private static bool _isStarted = false;
		private static bool _isSwitchedOn;
		private static bool _useDefaults = false;
		private static bool _logSameText;
		// Show in log
		private static bool _b_LogId = true;
		private static bool _logTimeStamp = true;
		private static bool _logSeverity = false;
		private static bool _logReturned = true;
		private static bool _logMethodName = true;
		private static bool _logClassName = true;
		private static bool _logDuration = true;
		private static bool _logSameCount = true;
		private static bool _logCategory = true;

		// Filters
		private static int _logThresholdMillis = 1;
		private static string _logFilterCategory = "All";
		// Stack
		private static Dictionary<int, DateTime> _stack;
		private static int _logId = 0;

		private static string _logFileDir = string.Empty;
		private static string _logFileName = "LogUtil.txt";
		private static string _logPath;
		private static bool _logFileNameWithTimestamp = true;
		private static int _sameDataCount = 0;
		// Widths
		private static int _textWidth = 160;
		private static int _w_LogId;
		private static int _w_Timestamp;
		private static int _w_Returned;
		private static int _w_ClassName;
		private static int _w_MethodName;
		private static int _w_Severity;
		private static int _w_SameCount;
		private static int _w_Duration;
		private static int _w_Category;

		// Previous line values
		private static int _p_LogId = 0;
		private static string _p_Timestamp;
		private static string _p_Returned;
		private static string _p_ClassName;
		private static string _p_MethodName;
		private static string _p_Severity;
		private static int _p_SameCount = 0;
		private static int _p_Duration = 0;
		private static string _p_Category;

		const string _STRIPECHAR = "-";
		const string _BLANK = " ";
		const string _EMPTY = "";
		const string _ERROR = "*ERROR";

		// properties
		public static bool isSwitchedOn
		{
			get { return _isSwitchedOn; }
		}
		public static bool useDefaults
		{
			get { return _useDefaults; }
		}
		public static bool logSeverity
		{
			get { return _logSeverity; }
			set { _logSeverity = value; }
		}
		public static bool logTimeStamp
		{
			get { return _logTimeStamp; }
			set { _logTimeStamp = value; }
		}
		public static bool logReturned
		{
			get { return _logReturned; }
			set { _logReturned = value; }
		}
		public static bool logClassName
		{
			get { return _logClassName; }
			set { _logClassName = value; }
		}
		public static bool logMethodName
		{
			get { return _logMethodName; }
			set { _logMethodName = value; }
		}
		public static int TextWidth
		{
			get { return _textWidth; }
			set { _textWidth = value; }
		}
		public static bool logCategory
		{
			get { return _logCategory; }
			set { _logCategory = value; }
		}
		public static string LogFileDir
		{
			get { return _logFileDir; }
			set { _logFileDir = value; }
		}
		#endregion
		#region Constructor
		#endregion
		public static void Start(string configPath)
		{
			if (!_isStarted)
			{
				_isStarted = true;
				configXml = configPath;
				Initialize();
				LogTitle();
			}
		}

		#region Add Line
		static void LogTitle()
		{
			// Title bar (for every instance)
			if (_sw != null)
			{
				AddStripe();
				var line = new StringBuilder();
				line.Append(GetSupportedCell("Id", _w_LogId, _b_LogId, true));
				line.Append(GetSupportedCell("Timestamp (end method)", _w_Timestamp, _logTimeStamp));
				line.Append(GetSupportedCell("Dur(ms)", _w_Duration, _logDuration, true));
				line.Append(GetSupportedCell("Category", _w_Category, _logCategory));
				line.Append(GetSupportedCell("Same", _w_SameCount, _logSameText, true));
				line.Append(GetSupportedCell("Severity", _w_Severity, _logSeverity));
				line.Append(GetSupportedCell("Class name", _w_ClassName, _logClassName));
				line.Append(GetSupportedCell("Method name", _w_MethodName, _logMethodName));
				line.Append(GetSupportedCell("Return", _w_Returned, _logReturned));
				line.Append("Data");
				line = WriteLine(line);
				AddStripe();
			}

		}
		// Start a logline, returning the stack logId.
		public static int StartLine()
		{
			if (_isSwitchedOn)
			{
				_logId += 1;
				_stack.Add(_logId, DateTime.UtcNow.ToLocalTime());
			}
			return _logId;
		}
		// Add a line from a string 
		public static void AddLine(string data, string category = _EMPTY, int logId = 0, string returned = _EMPTY, string className = _EMPTY, string methodName = _EMPTY, string severity = _EMPTY)
		{
			AddLine(new string[] { data }, category, logId, returned, className, methodName, severity);
		}
		// Add a line from an exception
		public static void AddLine(string data, string category, Exception ex, int logId = 0, string returned = _EMPTY, string className = _EMPTY, string methodName = _EMPTY, string severity = _EMPTY)
		{
			AddException(ex, data);
		}
		// Add a line from a string array - the basic one
		public static void AddLine(string[] data, string category = _EMPTY, int logId = 0, string returned = _EMPTY, string className = _EMPTY, string methodName = _EMPTY, string severity = _EMPTY)
		{
			if (_isSwitchedOn)
				try
				{
					// Using not possible when "leave open" not possible in .NET 3.5
					//using (_sw)
					//{

					// .NET 3.5 does not support Stringbuilder.Clear() method.
					_currentData = new StringBuilder();
					for (int i = 0; i < data.Length; i++)
					{
						_currentData.Append(data[i]);
					}

					// "Same data" mode: only count same occurrences (do not write).
					if (_logSameText && _currentData.Equals(_p_Data))
					{
						_sameDataCount += 1;
						return;
					}

					// New data

					// Filter
					bool Valid = false;
					// a. Millis threshold
					int ms = 0;
					// No stack dictionary is kept: just write without using duration.
					if (_p_LogId == 0)
					{
						Valid = true;
					}
					else
					{
						// Same data was encountered: Get duration from previous Id, otherwise from saved duration.
						ms = (_sameDataCount > 0) ? Convert.ToInt32(GetDurationFromLogIdTillNow(_p_LogId)) : _p_Duration;
						if (!Valid && ms >= _logThresholdMillis) Valid = true;
					}

					// b. Category 
					if (!Valid && (_logFilterCategory == category || _p_Category == _ERROR)) Valid = true;

					// Write previous line.
					if (Valid)
					{
						WriteLinePrevious();
						// Reset the time for current Id
						if (_logId > 0)
						{
							_stack[_logId] = DateTime.UtcNow.ToLocalTime();
						}
					}
					// Populate current values
					_p_LogId = _logId;
					_p_Timestamp = GetTimeStamp();
					_p_SameCount = _sameDataCount;
					_sameDataCount = 0;
					//_p_SameCount = 0;
					// N.B. with duration from previous line
					_p_Duration = Convert.ToInt32(GetDurationFromLogIdTillNow(_logId));
					_p_Severity = severity;
					_p_ClassName = className;
					_p_MethodName = methodName;
					_p_Category = category;
					_p_Returned = returned;
					_p_Data = new StringBuilder(_currentData.ToString());
					// _line = WriteLine(_line);

				}
				catch (ArgumentNullException ex)
				{
					if (_sw != null)
					{
						AddException(ex, "*ERROR LogUtil ArgumentNullException");
					}
				}
				catch (Exception ex)
				{
					if (_sw != null)
					{
						AddException(ex, "*ERROR LogUtil Exception");
					}
				}
		}
		#endregion
		#region public methods
		//public  LogUtil GetInstance()
		//{
		//    return this;
		//}
		public static bool GetSwitchedOn()
		{
			return _isSwitchedOn;
		}
		public static void Dispose()
		{
			if (_sw != null)
			{
				_sw.Flush();
				_sw.Close();
				_sw = null;
			}
			// this.Dispose();
		}


		// Add a -------- 
		public static void AddStripe()
		{
			if (_sw == null) return;

			for (int i = 0; i < _textWidth; i++)
				if (i == TextWidth - 1)
				{
					_sw.WriteLine(_STRIPECHAR);
				}
				else
				{
					_sw.Write(_STRIPECHAR);
				}
		}

		// Add a free text 
		public static void AddText(string text)
		{
			if (_sw == null) return;
			_sw.WriteLine(text);
		}
		// Wrap up
		public static void Close()
		{
			if (_sw != null)
			{
				_sw.Flush();
				_sw.Close();
			}
		}
		#endregion
		#region Private methods
		// Initialize
		private static void Initialize()
		{
			// Is Log file already created? Then return.
			if (_sw != null) return;

			if (ConfigurationManager.AppSettings["isLogUtilSwitchedOn"] == null || ConfigurationManager.AppSettings["isLogUtilSwitchedOn"].ToString() == string.Empty)
			{
				_useDefaults = true;
				_isSwitchedOn = true;

				// Try to get directory from .config
				try
				{
					if (File.Exists(configXml))
					{
						string className = MethodBase.GetCurrentMethod().DeclaringType.ToString().Split('.').Last();
						SettingsManager settings = new SettingsManager(configXml, className);
						string dir = settings.SelectElementValue("DirName");
						if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
						{
							LogFileDir = dir;
						}
						logTimeStamp = Convert.ToBoolean(settings.SelectElementValue("IsTimeStampLogged", AsT.boolT));
						logSeverity = Convert.ToBoolean(settings.SelectElementValue("IsSeverityLogged", AsT.boolT));
						logReturned = Convert.ToBoolean(settings.SelectElementValue("IsReturnLogged", AsT.boolT));
						logClassName = Convert.ToBoolean(settings.SelectElementValue("IsClassNameLogged", AsT.boolT));
						logMethodName = Convert.ToBoolean(settings.SelectElementValue("IsMethodNameLogged", AsT.boolT));
						logCategory = Convert.ToBoolean(settings.SelectElementValue("IsCategoryLogged", AsT.boolT));

						_textWidth = Convert.ToInt32(settings.SelectElementValue("TextWidth", AsT.intT));
						_logThresholdMillis = Convert.ToInt32(settings.SelectElementValue("ThresholdMillis", AsT.intT));
						_logFileName = Convert.ToString(settings.SelectElementValue("FileName"));
						_logFileNameWithTimestamp = Convert.ToBoolean(settings.SelectElementValue("HasFileNameWithTimestamp", AsT.boolT));
						_logSameText = Convert.ToBoolean(settings.SelectElementValue("IsSameTextCounted", AsT.boolT));
						_logFilterCategory = Convert.ToString(settings.SelectElementValue("FilterCategory"));
					}
				}
				catch (Exception e)
				{
					string dummy = e.InnerException.Message;
				}

				if (string.IsNullOrEmpty(LogFileDir))
				{
					_logFileDir = string.Concat(AppDomain.CurrentDomain.BaseDirectory);
				}
				_logFileName = _logFileName + DateTime.UtcNow.ToLocalTime().ToString(" yyyyMMdd HHmmss ") + Environment.UserName;
			}
			else
			{
				_isSwitchedOn = Convert.ToBoolean(ConfigurationManager.AppSettings["isLogUtilSwitchedOn"]);
			}
			if (_isSwitchedOn)
			{
				_stack = new Dictionary<int, DateTime>();
				_p_Data = new StringBuilder();
				_currentData = new StringBuilder();
				if (!useDefaults)
				{
					logTimeStamp = Convert.ToBoolean(ConfigurationManager.AppSettings["IsLogUtilTimeStampLogged"]);
					logSeverity = Convert.ToBoolean(ConfigurationManager.AppSettings["IsLogUtilSeverityLogged"]);
					logReturned = Convert.ToBoolean(ConfigurationManager.AppSettings["IsLogUtilReturnLogged"]);
					logClassName = Convert.ToBoolean(ConfigurationManager.AppSettings["IsLogUtilClassNameLogged"]);
					logMethodName = Convert.ToBoolean(ConfigurationManager.AppSettings["IsLogUtilMethodNameLogged"]);
					logCategory = Convert.ToBoolean(ConfigurationManager.AppSettings["IsLogUtilCategoryLogged"]);

					_textWidth = Convert.ToInt32(ConfigurationManager.AppSettings["LogUtilTextWidth"]);
					_logThresholdMillis = Convert.ToInt32(ConfigurationManager.AppSettings["LogUtilThresholdMillis"]);
					_logFileDir = ConfigurationManager.AppSettings["LogUtilDirName"];
					if (_logFileDir == "%temp%") { _logFileDir = System.IO.Path.GetTempPath(); }
					_logFileName = ConfigurationManager.AppSettings["LogUtilFileName"];
					_logFileNameWithTimestamp = Convert.ToBoolean(ConfigurationManager.AppSettings["LogUtilFileNameWithTimestamp"]);
					_logSameText = Convert.ToBoolean(ConfigurationManager.AppSettings["IsLogUtilSameTextCounted"]);
					_logFilterCategory = ConfigurationManager.AppSettings["LogUtilFilterCategory"];
				}

				_logFileName = (!string.IsNullOrEmpty(Path.GetFileNameWithoutExtension(_logFileName))) ? Path.GetFileNameWithoutExtension(_logFileName) : "";

				if (_logFileNameWithTimestamp && !string.IsNullOrEmpty(_logFileName))
				{
					_logFileName = _logFileName + DateTime.UtcNow.ToLocalTime().ToString("_yyyyMMdd_HHmmss_") + Environment.UserName;
				}
				_logFileName = (!string.IsNullOrEmpty(_logFileName)) ? _logFileName + ".txt" : "LogUtil.txt";
				_logPath = Path.Combine(_logFileDir, _logFileName);

				_w_LogId = 5;
				_w_Timestamp = logTimeStamp ? 25 : 0;
				_w_Severity = logSeverity ? 12 : 0;
				_w_Returned = logReturned ? 20 : 0;
				_w_ClassName = logClassName ? 20 : 0;
				_w_MethodName = logMethodName ? 40 : 0;
				_w_SameCount = _logSameText ? 5 : 0;
				_w_Category = logCategory ? 10 : 0;
				_w_Duration = 8;

				CreateLogFile(_logPath, _logFileName, _textWidth);
				// FATAL error.
				if (_sw == null) _isSwitchedOn = false;

				// Using not possible when "leave open" not possible in .NET 3.5
				//using (_sw)
				AddStripe();
				_sw.WriteLine("S t a r t    L o g U t i l");
				AddStripe();
				_sw.WriteLine("Log file . . . . . . . : " + _logFileName);
				_sw.WriteLine("Ms threshold . . . . . : " + _logThresholdMillis.ToString());
				_sw.WriteLine("Category . . . . . . . : " + _logFilterCategory.ToString());
			}
		}
		// Error handling
		public static void AddException(Exception ex, string firstText = _EMPTY, bool stacktrace = true, bool external = true)
		{
			if (_sw != null)
			{
				if (firstText != _EMPTY)
				{
					_sw.WriteLine(firstText);
					AddStripe();
				}
				_sw.WriteLine("Message . . . .: " + ex.Message);
				if (ex.InnerException != null) _sw.WriteLine("Inner Exception: " + ex.InnerException.Message);
				if (stacktrace && ex.StackTrace != null)
				{
					_sw.WriteLine("Stack trace: ");
					_sw.WriteLine(ex.StackTrace);
				}
				AddStripe();
				if (!external)
				{
					_sw.WriteLine("Logging has ended ABNORMALLY");
					AddStripe();
					_isSwitchedOn = false;
				}
			}
		}
		// Create a Log file
		private static void CreateLogFile(string path, string fileName, int width)
		{
			if (_sw == null)
			{
				try
				{
					if (File.Exists(path)) File.Delete(fileName);
					var fs = new FileStream(path, FileMode.CreateNew);
					// Streamwriter leave open(true): From .NET 4.5 only: _sw = new StreamWriter(fs, Encoding.UTF8, _textWidth, true);
					_sw = new StreamWriter(fs, Encoding.UTF8, width);
					_sw.AutoFlush = true;
				}
				catch (Exception ex) { throw ex; }
			}
		}
		// Get supported Cell
		private static string GetSupportedCell(string input, int cellWidth, bool isSupported, bool rightadjust = false)
		{
			string o = (isSupported) ? GetCell(input, cellWidth, rightadjust) : _EMPTY;
			return o;
		}
		// Get cell appended with blanks and with width constraint.
		private static string GetCell(string s, int cellWidth, bool rightAdjust = false)
		{
			if (rightAdjust)
			{
				return s.Trim().PadLeft(cellWidth) + _BLANK;
			}
			else
			{
				return s.Trim().PadRight(cellWidth);
			}
		}
		// Write line and reset the text
		private static StringBuilder WriteLine(StringBuilder sb)
		{
			_sw.WriteLine(sb.ToString());
			return new StringBuilder();
		}
		// Write line and reset the text
		private static void WriteLinePrevious()
		{
			// Populate previous line. Use current "same count" and recalculated duration.
			// Populate previous values
			if (_p_Timestamp != null)
			{
				var sb = new StringBuilder();
				string sameCount = (_p_SameCount.ToString() == "0") ? _BLANK : _p_SameCount.ToString();
				sb.Append(GetSupportedCell(_p_LogId.ToString(), _w_LogId, _b_LogId, true));
				sb.Append(GetSupportedCell(_p_Timestamp, _w_Timestamp, _logTimeStamp));
				sb.Append(GetSupportedCell(_p_Duration.ToString(), _w_Duration, _logDuration, true));
				sb.Append(GetSupportedCell(_p_Category, _w_Category, _logCategory));
				sb.Append(GetSupportedCell(sameCount, _w_SameCount, _logSameCount, true));
				sb.Append(GetSupportedCell(_p_Severity, _w_Severity, _logSeverity));
				sb.Append(GetSupportedCell(_p_ClassName, _w_ClassName, _logClassName));
				sb.Append(GetSupportedCell(_p_MethodName, _w_MethodName, _logMethodName));
				sb.Append(GetSupportedCell(_p_Returned, _w_Returned, _logReturned));
				sb.Append(_p_Data.ToString());
				_sw.WriteLine(sb.ToString());

			}
		}

		// Get Timestamp
		private static string GetTimeStamp()
		{
			return DateTime.UtcNow.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss.fff ");
		}

		// Get Duration
		private static double GetDurationFromLogIdTillNow(int id)
		{
			double ms = 0;
			DateTime started;
			if (_stack.TryGetValue(id, out started))
			{
				ms = (started > DateTime.MinValue) ? DateTime.UtcNow.ToLocalTime().Subtract(started).TotalMilliseconds : 0;
			}
			return ms;
		}

		#endregion
	}
}
