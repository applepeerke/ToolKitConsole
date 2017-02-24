using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using GeneralUtilities.Data;

namespace GeneralUtilities
{
	public class XmlDBManager : IDisposable
	{
		const string CLOSE = "CLOSE";
		const string EMPTY = "";

		private string NAME = "name";
		private string TABLE = "table";
		private string TABLES = "tables";

		private XDocument doc;
		private XElement tables;
		private OutputWrapper ow;
		private bool LoadedDFD = false;
		private string className;
		private string configFile = string.Empty;
		// Config file properties
		private string baseFolderPath = string.Empty;
		private string DBFileName = string.Empty;
		#region Properties
		/// The xml database.
		private XmlDatabase xmlDatabase;
		public XmlDatabase XmlDatabase { get { return this.xmlDatabase; } set { this.xmlDatabase = value; } }

		/// The output.
		private Output output = Output.None;
		public Output Output { get { return this.output; } set { this.output = value; } }

		/// The utilConsole.
		private ConsoleWrapper consoleWrapper = new ConsoleWrapper();
		public ConsoleWrapper ConsoleWrapper { get { return this.consoleWrapper; } set { this.consoleWrapper = value; } }

		/// The xml path.
		private string dFDPath = string.Empty;
		public string DFDPath { get { return dFDPath; } set { dFDPath = value; } }

		#endregion Properties

		#region Constructors
		// If called from OutputWrapper, the ow must be null.
		public XmlDBManager(string configXml)
		{
			ow = new OutputWrapper(configXml);
			Initialize(configXml);
		}
		public XmlDBManager(OutputWrapper outputWrapper, string configXml)
		{
			ow = outputWrapper;
			Initialize(configXml);
		}
		#endregion Constructors

		#region Public methods

		public void DeleteDFD()
		{
			string ext = Path.GetExtension(DFDPath).ToLower();
			if (ext == ".xml")
			{
				File.Delete(DFDPath);
				Log(string.Format("{0}: Deleted DFD in '{1}'", className, DFDPath));
			}
			else
			{
				throw new Exception(string.Format("{0}: File '{1}' to delete is not a xml file.", className, DFDPath));
			}
		}

		public void LoadDFD()
		{
			doc = XDocument.Load(DFDPath);
			tables = doc.Element(TABLES);
			LoadedDFD = true;
			Log(string.Format("{0}: Loaded DFD from '{1}'", className, dFDPath));
		}

		public void SaveDFD()
		{
			if (LoadedDFD)
			{
				doc.Save(DFDPath);
				Log(string.Format("{0}: Saved DFD in '{1}'", className, DFDPath));
			}
		}

		public void CreateDFDTable(XElement tableName, XElement table = null)
		{
			if (!LoadedDFD) LoadDFD();

			if (table == null)
			{
				tables.Add(tableName);
				Log(string.Format("{0}: Empty table '{1}' added to '{2}'", className, tableName, DFDPath));
			}
			else
			{
				tables.Add(table);
				Log(string.Format("{0}: Populated table '{1}' added to '{2}'", className, tableName, DFDPath));
			}
		}

		public void DeleteDFDTable(string table)
		{
			if (!LoadedDFD) LoadDFD();
			if (tables != null)
			{
				try
				{
					tables.DescendantsAndSelf(TABLE).FirstOrDefault(r => r.Attribute(NAME).Value == table).Remove();
					Log(string.Format("{0}: Table '{1}' deleted from '{2}'", className, table, DFDPath));
				}
				catch (System.NullReferenceException) { }
			}
		}
		public XElement GetTableRef(string table)
		{
			try
			{
				XElement parentElement = doc.Descendants(TABLE).FirstOrDefault(r => r.Attribute(NAME).Value == table);
				return parentElement;
			}
			catch (System.NullReferenceException)
			{
				return null;
			}
		}
		public void Dispose()
		{
			SaveDFD();
		}

		#endregion
		#region Private methods
		private void Initialize(string configXml)
		{
			className = GetType().Name.Split('.').Last();
			configFile = configXml;
			try
			{
				GetSettings(configFile);
				Verify();
				CreateDFD();
			}
			catch (Exception e)
			{
				ErrorHandling(e);
			}
			Log(string.Format("Starting {0}...", className));
		}

		// Get settings
		private void GetSettings(string configXml)
		{
			// Try to get directory from .config
			try
			{
				if (File.Exists(configXml))
				{
					SettingsManager settings = new SettingsManager(configXml, className);
					baseFolderPath = settings.SelectElementValue("baseFolderPath");
					DBFileName = settings.SelectElementValue("dBFileName");
					// Derived
					dFDPath = Path.Combine(baseFolderPath, DBFileName);
				}
			}
			catch (Exception e)
			{
				if (ow == null) throw e;
				ErrorHandling(e);
			}
		}

		// Verify input
		private void Verify()
		{
			Log("=======================================================================");
			Log(string.Format("{0}", className.ToUpper()));
			Log("=======================================================================");
			Log(string.Format("Base folderpath  . . . . . . . . . . : {0}", baseFolderPath));
			Log(string.Format("XML DB definitions (DFD) name  . . . : {0}", DBFileName));
			Log("=======================================================================");
			Log("Press any key to continue (Ctrl-C = Cancel)");
			if (ow != null) ow.ReadKey();
		}

		// Creates the dfd.
		private void CreateDFD()
		{
			try
			{
				if (!File.Exists(DFDPath))
				{
					doc =
						new XDocument(
							new XElement(TABLES)
						);
					doc.Save(DFDPath);
					Log(string.Format("{0}: DFD '{1}' has been created in '{2}'", className, DBFileName, DFDPath));
				}
			}
			catch (Exception e)
			{
				if (ow == null) throw e;
				ErrorHandling(e);
			}
		}

		// Saves the xml.
		private void SerializeXml()
		{
			try
			{
				XmlSerializer.Serialize<XmlDatabase>(XmlDatabase, DFDPath);
				LoadedDFD = false;
				Log(string.Format("{0}: Saved '{1}'", className, DFDPath));
			}
			catch (Exception ex)
			{
				throw new Exception(string.Format("{0}: Save '{1}' has failed!\n{2}", className, DFDPath, ex.Message), ex);
			}
		}
		private void DeserializeXml()
		{
			XmlDatabase = XmlSerializer.Deserialize<XmlDatabase>(DFDPath);
			LoadedDFD = true;
			Log(string.Format("{0} Read DFD from '{1}'", className, DFDPath));
		}

		// Writes the xml.
		private void OutputXml()
		{
			Log(string.Format("{0}", XmlDatabase.Name));
			XmlDatabase.XmlTables.ForEach((o) =>
			{
				Log(string.Format("Table {0}: ", o.Name));
				o.XmlColumns.ForEach((a) =>
				{
					Log(string.Format("-Attribute {0}: {1}", a.Name, a.Value));
				});
			});
		}

		// Error Handling
		private void ErrorHandling(Exception e)
		{
			if (ow == null) throw e;
			string message = (e.InnerException != null) ? e.InnerException.Message : e.Message;
			Log(string.Format("[ER] - {0}", message));
		}

		// Logging
		private void Log(string line)
		{
			if (ow != null) ow.WriteLine(line);
		}

		#endregion Private methods
	}
}