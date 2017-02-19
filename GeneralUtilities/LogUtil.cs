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
	public class LogUtil : IOutput
	{
		private string configXml = string.Empty; 
		private StreamWriter _sw;
		private StringBuilder _p_Data;
		private StringBuilder _currentData = new StringBuilder();
		// private StringBuilder _line;
		private bool _isStarted = false;
		private bool _isSwitchedOn;
		private bool _useDefaults = false;
		private bool _logSameText;
		// Show in log
		private bool _b_LogId = true;
		private bool _logTimeStamp = true;
		private bool _logSeverity = false;
		private bool _logReturned = true;
		private bool _logMethodName = true;
		private bool _logClassName = true;
		private bool _logDuration = true;
		private bool _logSameCount = true;
		private bool _logCategory = true;

		// Filters
		private int _logThresholdMillis = 1;
		private string _logFilterCategory = "All";
		// Stack
		private Dictionary<int, DateTime> _stack;
		private int _logId = 0;

		private string _logFileDir = string.Empty;
		private string _logFileName = "LogUtil.txt";
		private string _logPath;
		private bool _logFileNameWithTimestamp = true;
		private int _sameDataCount = 0;
		// Widths
		private int _textWidth = 160;
		private int _w_LogId;
		private int _w_Timestamp;
		private int _w_Returned;
		private int _w_ClassName;
		private int _w_MethodName;
		private int _w_Severity;
		private int _w_SameCount;
		private int _w_Duration;
		private int _w_Category;

		// Previous line values
		private int _p_LogId = 0;
		private string _p_Timestamp;
		private string _p_Returned;
		private string _p_ClassName;
		private string _p_MethodName;
		private string _p_Severity;
		private int _p_SameCount = 0;
		private int _p_Duration = 0;
		private string _p_Category;

		const string _STRIPECHAR = "-";
		const string _BLANK = " ";
		const string _EMPTY = "";
		const string _ERROR = "*ERROR";

		#region properties
		private static LogUtil _instance;
		public static LogUtil Instance
		{ 
			get 
			{
				if (_instance == null)
				{ 
					_instance = new LogUtil();
				}
				return _instance; 
			} 
		}

		public bool isSwitchedOn
		{
			get { return _isSwitchedOn; }
		}
		public bool useDefaults
		{
			get { return _useDefaults; }
		}
		public bool logSeverity
		{
			get { return _logSeverity; }
			set { _logSeverity = value; }
		}
		public bool logTimeStamp
		{
			get { return _logTimeStamp; }
			set { _logTimeStamp = value; }
		}
		public bool logReturned
		{
			get { return _logReturned; }
			set { _logReturned = value; }
		}
		public bool logClassName
		{
			get { return _logClassName; }
			set { _logClassName = value; }
		}
		public bool logMethodName
		{
			get { return _logMethodName; }
			set { _logMethodName = value; }
		}
		public int TextWidth
		{
			get { return _textWidth; }
			set { _textWidth = value; }
		}
		public bool logCategory
		{
			get { return _logCategory; }
			set { _logCategory = value; }
		}
		public string LogFileDir
		{
			get { return _logFileDir; }
			set { _logFileDir = value; }
		}
		#endregion
		#region Constructor
		#endregion
		public void Start(string configPath)
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
		void LogTitle()
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
				WriteLine(line.ToString());
				line = new StringBuilder();
				AddStripe();
			}

		}
		// Start a logline, returning the stack logId.
		public int StartLine()
		{
			if (_isSwitchedOn)
			{
				_logId += 1;
				_stack.Add(_logId, DateTime.UtcNow.ToLocalTime());
			}
			return _logId;
		}
		// Add a line from a string 
		public void AddLine(string data, string category = _EMPTY, int logId = 0, string returned = _EMPTY, string className = _EMPTY, string methodName = _EMPTY, string severity = _EMPTY)
		{
			AddLine(new string[] { data }, category, logId, returned, className, methodName, severity);
		}
		// Add a line from an exception
		public void AddLine(string data, string category, Exception ex, int logId = 0, string returned = _EMPTY, string className = _EMPTY, string methodName = _EMPTY, string severity = _EMPTY)
		{
			AddException(ex, data);
		}
		// Add a line from a string array - the basic one
		public void AddLine(string[] data, string category = _EMPTY, int logId = 0, string returned = _EMPTY, string className = _EMPTY, string methodName = _EMPTY, string severity = _EMPTY)
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
		public bool GetSwitchedOn()
		{
			return _isSwitchedOn;
		}
		public void Dispose()
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
		public void AddStripe()
		{
			if (_sw == null) return;

			for (int i = 0; i < _textWidth; i++)
				if (i == TextWidth - 1)
				{
					WriteLine(_STRIPECHAR);
				}
				else
				{
					_sw.Write(_STRIPECHAR);
				}
		}

		// Add a free text 
		public void AddText(string text)
		{
			if (_sw == null) return;
			WriteLine(text);
		}
		// Wrap up
		public void Close()
		{
			if (_sw != null)
			{
				_sw.Flush();
				_sw.Close();
			}
		}

		public void WriteLine(string line)
		{
			if (string.IsNullOrEmpty(line)) return;
			_sw.WriteLine(line);
		}
		#endregion
		#region Private methods
		// Initialize
		private void Initialize()
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
				WriteLine("S t a r t    L o g U t i l");
				AddStripe();
				WriteLine("Log file . . . . . . . : " + _logFileName);
				WriteLine("Ms threshold . . . . . : " + _logThresholdMillis.ToString());
				WriteLine("Category . . . . . . . : " + _logFilterCategory.ToString());
			}
		}
		// Error handling
		public void AddException(Exception ex, string firstText = _EMPTY, bool stacktrace = true, bool external = true)
		{
			if (_sw != null)
			{
				if (firstText != _EMPTY)
				{
					WriteLine(firstText);
					AddStripe();
				}
				WriteLine("Message . . . .: " + ex.Message);
				if (ex.InnerException != null) WriteLine("Inner Exception: " + ex.InnerException.Message);
				if (stacktrace && ex.StackTrace != null)
				{
					WriteLine("Stack trace: ");
					WriteLine(ex.StackTrace);
				}
				AddStripe();
				if (!external)
				{
					WriteLine("Logging has ended ABNORMALLY");
					AddStripe();
					_isSwitchedOn = false;
				}
			}
		}
		// Create a Log file
		private void CreateLogFile(string path, string fileName, int width)
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
		private string GetSupportedCell(string input, int cellWidth, bool isSupported, bool rightadjust = false)
		{
			string o = (isSupported) ? GetCell(input, cellWidth, rightadjust) : _EMPTY;
			return o;
		}
		// Get cell appended with blanks and with width constraint.
		private string GetCell(string s, int cellWidth, bool rightAdjust = false)
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
		private void WriteLinePrevious()
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
				WriteLine(sb.ToString());

			}
		}

		// Get Timestamp
		private string GetTimeStamp()
		{
			return DateTime.UtcNow.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss.fff ");
		}

		// Get Duration
		private double GetDurationFromLogIdTillNow(int id)
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
