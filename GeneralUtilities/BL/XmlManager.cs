using System;
using System.IO;
using System.Linq;
using UtilConsole;
using System.Xml.Linq;
using GeneralUtilities.Data;
using System.Collections.Generic;

namespace GeneralUtilities
{
	public class XmlManager : IDisposable, IOutput
	{
		const string CLOSE = "CLOSE";
		const string EMPTY = "";
		const string NAME = "name";
		const string TYPE = "type";
		const string VALUE = "value";
		const string TABLE = "table";
		const string XMLLOG = "xmlLog";
		const string XMLLOGROW = "row";

		private OutputWrapper ow;
		private bool Loaded = false;
		private string className;
		private string configFile = string.Empty;
		// Config file properties
		private string baseFolderPath = string.Empty;
		private string dBFileName = string.Empty;
		private XDocument doc;
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
		private string dBXmlPath = string.Empty;
		public string DBXmlPath { get { return dBXmlPath; } set { dBXmlPath = value; } }

		#endregion Properties

		#region Constructors
		// If called from OutputWrapper, the ow must be null.
		public XmlManager(string configXml)
		{
			Initialize(configXml);
		}
		public XmlManager(OutputWrapper outputWrapper, string configXml)
		{
			ow = outputWrapper;
			Initialize(configXml);
		}
		#endregion Constructors

		#region Public methods
		private void Initialize(string configXml)
		{
			className = GetType().Name.Split('.').Last();
			configFile = configXml;
			try
			{
				GetSettings(configFile);
				Verify();
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
					dBFileName = settings.SelectElementValue("dBFileName");
					// Derived
					dBXmlPath = Path.Combine(baseFolderPath, dBFileName);
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
			Log(string.Format("XML DB name . . .  . . . . . . . . . : {0}", dBFileName));
			Log("=======================================================================");
			Log("Press any key to continue (Ctrl-C = Cancel)");
			if (ow != null) ow.ReadKey();
		}

		// Execute
		public void Execute(CRUD operation, ObjectType type, string name = EMPTY, string val = EMPTY, string parentName = EMPTY, Mode mode = Mode.None)
		{
			switch (operation)
			{
				case CRUD.Create:
					{
						if (type == ObjectType.Row) InsertRow(parentName, name, val);
						//SaveXml();
						break;
					}
				case CRUD.Read:
					{
						ReadXml();
						break;
					}
				case CRUD.Update:
					{
						throw new NotImplementedException();
						// SaveXml();
						// break;
					}
				case CRUD.Delete:
					{
						DeleteXml();
						break;
					}
				default:
					{ throw new NotImplementedException(); }
			}
		}

		// Writes the xml.
		public void OutputXml()
		{
			Log(string.Format("{0}", XmlDatabase.Name));
			XmlDatabase.XmlObjects.ForEach((o) =>
			{
				Log(string.Format("Object {0}: ", o.Name));
				o.XmlAttributes.ForEach((a) =>
				{
					Log(string.Format("-Attribute {0}: {1}", a.Name, a.Value));
				});
			});
		}

		// Saves the xml.
		public void Save()
		{
			if (Loaded)
			{
				doc.Save(dBXmlPath);
				Log(string.Format("{0}: Saved '{1}'", className, dBXmlPath));
			}
		}
		#endregion
		#region Private methods

		// Saves the xml.
		private void SaveXml()
		{
			try
			{
				XmlSerializer.Serialize<XmlDatabase>(XmlDatabase, dBXmlPath);
				Loaded = false;
				Log(string.Format("{0}: Saved '{1}'", className, dBXmlPath));
			}
			catch (Exception ex)
			{
				throw new Exception(string.Format("{0}: Save '{1}' has failed!\n{2}", className, dBXmlPath, ex.Message), ex);
			}
		}

		// Loads the xml.
		private void ReadXml()
		{
			XmlDatabase = XmlSerializer.Deserialize<XmlDatabase>(dBXmlPath);
			Loaded = true;
			Log(string.Format("{0} Read '{1}'", className, dBXmlPath));
		}

		// Deletes the xml.
		private void DeleteXml()
		{
			string ext = Path.GetExtension(dBXmlPath).ToLower();
			if (ext == ".xml")
			{
				File.Delete(dBXmlPath);
				Log(string.Format("{0}: Deleted '{1}'", className, dBXmlPath));
			}
			else
			{
				throw new Exception(string.Format("{0}: File '{1}' to delete is not a xml file.", className, dBXmlPath));
			}
		}

		// Deletes the table.
		private void DeleteTable(string table)
		{
			if (!Loaded) Load();
			XElement parentElement = (from Node in doc.Descendants("objects")
									  where (string)Node.Attribute("name") == table
									  select Node).FirstOrDefault();
			parentElement.RemoveAll();
			Log(string.Format("{0}: Table '{1}' deleted from '{2}'", className, table, dBXmlPath));
		}

		// Creates the Xml db.
		private void CreateXmlDB()
		{
			XmlDatabase.XmlObjects.Add(new XmlObject("myObject"));
			XmlObject obj = XmlDatabase.XmlObjects.LastOrDefault();
			obj.XmlAttributes.Add(new XmlObjectAttribute(NAME, "Naam"));
			obj.XmlAttributes.Add(new XmlObjectAttribute(TYPE, "int"));
			obj.XmlAttributes.Add(new XmlObjectAttribute(VALUE, 1));
			obj.XmlAttributes.Add(new XmlObjectAttribute("CreationDate", DateTime.Now));
			SaveXml();

			XElement TableElem = new XElement(TABLE, new XAttribute(NAME, "table"));
			XElement newTable =
				new XElement(TABLE, new XAttribute(NAME, "table"),
							 new XElement("Name", new XAttribute(TYPE, "string")),
							 new XElement("Latitude", new XAttribute(TYPE, "int")),
							 new XElement("Longitude", new XAttribute(TYPE, "int")));

			InsertTable(TableElem, newTable);
			Log(string.Format("{0}: Table '{1}' added to '{2}'", className, newTable, dBXmlPath));
		}

		// Inserts the table.
		private void InsertTable(XElement tableName, XElement table = null)
		{
			if (!Loaded) Load();

			XElement parentElement = doc.Descendants("objects").FirstOrDefault();
			if (table == null)
			{
				parentElement.Add(tableName);
			}
			else
			{
				parentElement.Add(table);
			}
			Log(string.Format("{0}: Table '{1}' added to '{2}'", className, table, dBXmlPath));
		}

		// Inserts the row.
		private void InsertRow(string tableName, string rowName, string rowValue)
		{
			if (!Loaded) Load();

			var tableElements = doc.Root.Element("objects").Elements(TABLE);
			var currentTable = tableElements.Where(r => (string)r.Attribute(NAME) == tableName).FirstOrDefault();

			// Table does not exist, add it with the row.
			if (currentTable == null)
			{
				XElement newTable =
	new XElement(TABLE, new XAttribute(NAME, tableName),
					new XElement(rowName, rowValue));
				InsertTable(null, newTable);
				currentTable = tableElements.Where(r => (string)r.Attribute(NAME) == tableName).FirstOrDefault();
			}
			else
			{
				// Add the table row after the last row.
				var newRow = new XElement(rowName, rowValue);
				var lastRow = currentTable.Descendants(rowName).LastOrDefault();
				lastRow.AddAfterSelf(newRow);
			}
		}

		private void Load()
		{
			try
			{
				doc = XDocument.Load(dBXmlPath);
				Loaded = true;
				Log(string.Format("{0}: Loaded '{1}'", className, dBXmlPath));
			}
			catch (System.IO.FileNotFoundException)
			{
				CreateXmlDB();
				doc = XDocument.Load(dBXmlPath);
			}
		}

		// Log this line.
		private void Log(string line)
		{
			InsertRow("xmlLog", "row", line);
		}

		// Error Handling
		private void ErrorHandling(Exception e)
		{
			if (ow == null) throw e;
			string message = (e.InnerException != null) ? e.InnerException.Message : e.Message;
			Log(string.Format("[ER] - {0}", message));
		}

		public void Dispose()
		{
			Save();
		}

		public void WriteLine(string line)
		{
			if (string.IsNullOrEmpty(line)) return;
			InsertRow(XMLLOG, XMLLOGROW, line);
		}
		#endregion Private methods
	}
}