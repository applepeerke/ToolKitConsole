#region Imports
using System;
using System.IO;
using System.Data;
using System.Text;
using System.Collections.Generic;
using System.Configuration;
#endregion

namespace GeneralUtilities
{

	public sealed class JournalUtil : IOutput
	{
		private DataTable _journal;
		private StreamWriter _sw;
		private int _journalId;
		private List<string> _headerRow = new List<string>();
		private List<string> _detailRow = new List<string>();
		private string _header;
		private string _ext;
		private bool _isSwitchedOn = false;
		private bool _journalAllConditionsBeforeSave = false;
		private int _textWidth = 160;
		private int _count_OK = 0;
		private int _count_ER = 0;
		private string _columnSeparator = ";";
		private readonly object _lock = new object();

		#region Properties
		private static JournalUtil _instance;
		public static JournalUtil Instance
		{
			get
			{
				if (_instance == null)
				{
					_instance = new JournalUtil();
				}
				return _instance;
			}
		}

		public int TextWidth
		{
			get { return _textWidth; }
		}

		public  string ColumnSeparator
		{
			get { return _columnSeparator; }
		}

		public  bool isSwitchedOn
		{
			get { return _isSwitchedOn; }
		}
		public  bool JournalAllConditionsBeforeSave
		{
			get { return _journalAllConditionsBeforeSave; }
		}
		#endregion

		#region Constructors
		private JournalUtil()
		{
			Initialize();
		}
		#endregion

		#region Overloads
		// Add record
		// e.g. AddRecord("Fee", 40219, "my data before update", Image.Before)
		public  void AddRecord(JournalModel journalModel)
		{
			switch (journalModel.Operation.ToString())
			{
				case "Insert":
				case "Update":
				case "Delete":
					if (journalModel.Image.ToString() != "Before")
					{
						if (journalModel.Result.ToString() == "OK") _count_OK += 1;
						else _count_ER += 1;
					}
					break;
				default:
					ResetCounters();
					break;
			}

			// User name
			if (string.IsNullOrEmpty(journalModel.UserName))
			{
				journalModel.UserName = Environment.UserName;
			}

			_journalId += 1;
			_journal.Rows.Add(journalModel.UserName, _journalId, DateTime.UtcNow.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss.fff "), journalModel.Source.ToString(), journalModel.Entity, journalModel.Key, journalModel.Image.ToString(), journalModel.Operation.ToString(), journalModel.Result.ToString(), _count_OK.ToString(), _count_ER.ToString(), journalModel.Data, journalModel.UserParameter, journalModel.SpareKey1, journalModel.SpareKey2);
			//_journal.Rows.Add(_journalId, DateTime.UtcNow.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss.fff "), source.ToString(), entity, key, image.ToString(), oper.ToString(), result.ToString(), _count_OK.ToString(), _count_ER.ToString(), data, userparm, sparekey1, sparekey2);
		}
		public  void AddRecord(JournalModel journalModel, int result)
		{
			journalModel.Result = result < -1 ? JournalResult.ER : JournalResult.OK;
			AddRecord(journalModel);
		}
		public  void AddRecord(JournalModel journalModel, bool result)
		{
			journalModel.Result = result ? JournalResult.OK : JournalResult.ER;
			AddRecord(journalModel);
		}
		#endregion Overloads

		#region Public methods
		// Clear journal
		public  void Clear()
		{
			_journal.Clear();
			_journalId = 0;
			ResetCounters();
		}

		// Reset counters
		public  void ResetCounters()
		{
			_count_OK = 0;
			_count_ER = 0;
		}

		// Export journal
		public void Export(bool header = true)
		{
			if (_journal.Rows.Count > 0)
			{
				_ext = ConfigurationManager.AppSettings["JournalUtilExtension"];

				switch (_ext)
				{
					case ".xls":
					case ".txt":
						ExportToPCFile("\t");
						break;
					case ".csv":
						ExportToPCFile(";");
						break;
					case ".log":
						ExportToLogUtil();
						break;
					case ".tbl":
						ExportToTable();
						break;
					default:
						ExportToPCFile(" ");
						break;
				}
				// Clear journal
				Clear();
			}
		}

		public void Dispose()
		{
			Export();
		}

		public void WriteLine(string line)
		{
			if (string.IsNullOrEmpty(line)) return;
			_sw.WriteLine(line);
		}
		#endregion

		#region Private methods
		private void Initialize()
		{
			_isSwitchedOn = Convert.ToBoolean(ConfigurationManager.AppSettings["IsJournalUtilSwitchedOn"]);
			_journalAllConditionsBeforeSave = Convert.ToBoolean(ConfigurationManager.AppSettings["JournalAllConditionsBeforeSave"]);

			if (_isSwitchedOn)
			{
				// Get properties
				_textWidth = Convert.ToInt32(ConfigurationManager.AppSettings["JournalUtilTextWidth"]);
				_columnSeparator = ConfigurationManager.AppSettings["JournalUtilColumnSeparator"];
				// Define table
				_journal = new DataTable();
				_journal.Columns.Add("UserName", typeof(string));
				_journal.Columns.Add("JounalId", typeof(string));
				_journal.Columns.Add("Timestamp", typeof(string));
				_journal.Columns.Add("Source", typeof(string));
				_journal.Columns.Add("Entity", typeof(string));
				_journal.Columns.Add("Key");
				_journal.Columns.Add("Image", typeof(string));
				_journal.Columns.Add("Operation", typeof(string));
				_journal.Columns.Add("Result", typeof(string));
				_journal.Columns.Add("OK_count", typeof(string));
				_journal.Columns.Add("ER_count", typeof(string));
				_journal.Columns.Add("Data", typeof(string));
				_journal.Columns.Add("UserParameter", typeof(string));
				_journal.Columns.Add("SpareKey1", typeof(string));
				_journal.Columns.Add("SpareKey2", typeof(string));
				// Create header
				_headerRow.Clear();
				_headerRow.Add("UserName");
				_headerRow.Add("JounalId");
				_headerRow.Add("Timestamp");
				_headerRow.Add("Source");
				_headerRow.Add("Entity");
				_headerRow.Add("Key");
				_headerRow.Add("Image");
				_headerRow.Add("Operation");
				_headerRow.Add("Result");
				_headerRow.Add("OK_count");
				_headerRow.Add("ER_count");
				_headerRow.Add("Data");
				_headerRow.Add("UserParameter");
				_headerRow.Add("SpareKey1");
				_headerRow.Add("SpareKey2");
				// Initialize
				_journalId = 0;
			}
		}

		private  void ExportToLogUtil()
		{
			// Write header
			LogUtil.Instance.AddStripe(); //---------------
			LogUtil.Instance.AddText(_header);
			LogUtil.Instance.AddStripe(); //---------------

			// Write rows
			foreach (DataRow row in _journal.Rows)
			{
				LogUtil.Instance.AddText(GetFormattedRow(row, "\t"));
			}
			// End 
			LogUtil.Instance.AddStripe(); //---------------
		}

		private  string GetFormattedRow(DataRow row, string delimiter)
		{
			string formattedRow = string.Empty;
			_detailRow.Clear();
			foreach (var item in row.ItemArray)
			{
				string s = item.ToString().Trim();
				_detailRow.Add(s.Contains(delimiter) ? ("\"" + s + "\"") : s);
			}

			try
			{
				formattedRow = string.Join(delimiter, _detailRow.ToArray());
			}
			catch (Exception ex)
			{
				LogUtil.Instance.AddLine("JournalUtil: GetFormattedRow", "*ERROR", ex);
			}
			return formattedRow;
		}

		private void ExportToPCFile(string delimiter)
		{
			try
			{
				lock (_lock)
				{
					if (_sw == null)
					{
						CreateJournalFile(_ext);
						// Write header
						_header = string.Join(delimiter, _headerRow.ToArray());
						WriteLine(_header);
					}
				}

				// Write rows
				foreach (DataRow row in _journal.Rows)
				{
					WriteLine(GetFormattedRow(row, delimiter));
				}
			}
			catch (Exception ex)
			{
				_isSwitchedOn = false;
				LogUtil.Instance.AddLine("JournalUtil switched off. Exception occurred.", "*ERROR", ex);
				// throw ex;
			}
		}

		private  void ExportToTable()
		{
			throw new NotImplementedException();
		}

		private  void CreateJournalFile(string extension)
		{
			try
			{
				string journalDir = ConfigurationManager.AppSettings["JournalUtilDirName"];
				if (journalDir == "%temp%") { journalDir = System.IO.Path.GetTempPath(); }
				string journalPath = Path.Combine(journalDir, Path.GetFileNameWithoutExtension(ConfigurationManager.AppSettings["JournalUtilFileName"]) + DateTime.UtcNow.ToLocalTime().ToString(" yyyyMMdd HHmmss ") + Environment.UserName + extension);
				// Prevent IOError if file already exists.

				var fs = new FileStream(journalPath, FileMode.CreateNew);
				// Streamwriter leave open(true): From .NET 4.5 only: _sw = new StreamWriter(fs, Encoding.UTF8, _LogUtilTextWidth, true);
				_sw = new StreamWriter(fs, Encoding.UTF8, _textWidth);
				_sw.AutoFlush = true;

			}
			catch (Exception ex) { throw ex; }
		}
		#endregion
	}
}