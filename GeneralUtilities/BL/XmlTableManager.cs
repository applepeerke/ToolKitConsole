using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using GeneralUtilities.Data;

namespace GeneralUtilities
{
	public class XmlTableManager : IDisposable
	{
		private const string EMPTY = "";
		private string TABLE = "table";
		private string ROW = "row";
		private string NAME = "name";
		private string VALUE = "value";
		private string TIMESTAMP = "dateTimeCreation";
		private string ID = "id";
		private string tableName;
		private XDocument doc;
		private XElement table;
		#region Properties
		private string xmlPath = string.Empty;
		public string XmlPath { get { return xmlPath; } set { xmlPath = value; } }
		#endregion Properties

		#region Constructors
		public XmlTableManager(string path, string tablename)
		{
			tableName = tablename;
			XmlPath = string.Concat(Path.Combine(path, tableName), ".xml");
			if (File.Exists(XmlPath))
			{
				Load();
			}
			else
			{
				doc =
					new XDocument(
						new XElement(TABLE, new XAttribute(NAME, tableName),
						new XElement(ROW, new XAttribute(ID, "1"), new XAttribute(VALUE, "First row"))
								 )
					);
				doc.Save(XmlPath);
			}
		}
		#endregion

		#region Public methods

		// Inserts the row, returns the new id
		public int Create(string value)
		{
			if (doc == null) Load();
			int id = 1;
			// Add the data row after the last row.
			XElement lastRow = doc.Descendants(ROW).LastOrDefault();
			if (lastRow == null)
			{
				XElement newRow = new XElement(ROW, new XAttribute(ID, id.ToString()), new XAttribute(TIMESTAMP, GetTimeStamp()), new XAttribute(VALUE, value));
				XElement parent = doc.Root;
				parent.AddAfterSelf(newRow);
			}
			else
			{
				// Last row id + 1
				id = Convert.ToInt32(lastRow.Attribute("id").Value) + 1;
				XElement newRow = new XElement(ROW, new XAttribute(ID, id.ToString()), new XAttribute(TIMESTAMP, GetTimeStamp()), new XAttribute(VALUE, value));
				lastRow.AddAfterSelf(newRow);
			}
			return id;
		}
		/// <summary>
		/// Update the specified id, name and value.
		/// </summary>
		/// <returns>The update success.</returns>
		/// <param name="id">Identifier.</param>
		/// <param name="name">Name.</param>
		/// <param name="value">Value.</param>
		public bool Update(int id, string name, string value)
		{
			if (name == ID)
			{
				ErrorControl.Instance().AddError(string.Format("Attribute 'Id' can not be modified. Table={0}, Id={1}.", tableName, id.ToString()));
				return false;
			}
			if (doc == null) Load();
			XElement row = ReadX(id);
			if (row == null)
			{
				ErrorControl.Instance().AddError(string.Format("Row not found. Table={0}, Id={1}.", tableName, id.ToString()));
				return false;
			}
			if (row.Attribute(name) == null)
			{
				ErrorControl.Instance().AddError(string.Format("Attribute not found. Table={0}, Id={1}, Attribute name={2}.", tableName, id.ToString(), name));
				return false;
			}
			row.Attribute(name).Value = value;
			return true;
		}
		// 
		public string Read(int id)
		{
			if (doc == null) Load();
			XElement row = table.Descendants(ROW).FirstOrDefault(r => r.Attribute(ID).Value == id.ToString());
			if (row == null) return EMPTY;
			else return row.ToString();
		}
		private XElement ReadX(int id)
		{
			if (doc == null) Load();
			return table.Descendants(ROW).FirstOrDefault(r => r.Attribute(ID).Value == id.ToString());
		}
		public void Delete(int id)
		{
			if (doc == null) Load();
			XElement row = ReadX(id);
			if (row != null) row.Remove();
		}
		public int GetLastId()
		{
			if (doc == null) Load();

			var sortedRows = from r in table.Elements()
							 orderby Convert.ToInt32(r.Attribute("id").Value) ascending
							 select r;
			var row = sortedRows.Last();
			if (row == null) return 0;
			else return Convert.ToInt32(row.Attribute(ID).Value);
		}
		public void Save()
		{
			if (doc != null) doc.Save(XmlPath);
		}
		public void Dispose()
		{
			Save();
		}
		#endregion
		#region Private methods
		private void Load()
		{
			doc = XDocument.Load(XmlPath);
			table = doc.Element(TABLE);
		}
		private string GetTimeStamp()
		{
			return DateTime.UtcNow.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
		}
		#endregion
	}
}