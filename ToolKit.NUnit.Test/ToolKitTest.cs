using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using GeneralUtilities;
using NUnit.Framework;
using UtilConsole;

namespace ToolKitNUnitTest
{
	[TestFixture()]
	public class ToolKitTest
	{
		string configXml = "/users/peterwerk/Projects/TestResources/ToolKitConsole.config";
		string logPath = "/users/peterwerk/Projects/Log";
		QAManager qa;
		OutputWrapper ow;
		ConsoleWrapper cw;
		SettingsManager sm;

		[Test()]
		public void QAManagerConstructDefault()
		{
			// Question types
			// Type 1 (custom question)
			qa = new QAManager("Hello world?");
			Assert.AreEqual(qa.Question, "Hello world? (C=Cancel)");
			// Type 2 (standard questions)
			qa = new QAManager(Enums.QAType.YN);
			Assert.AreEqual(qa.Question, "Please specify a value (Default=Y, C=Cancel)");
			qa = new QAManager(Enums.QAType.Dir);
			Assert.AreEqual(qa.Question, "Please specify an existing directory (C=Cancel)");
			qa = new QAManager(Enums.QAType.File);
			Assert.AreEqual(qa.Question, "Please specify a valid filename (C=Cancel)");
			// Type 3 (custom List of values)
			qa = new QAManager(new List<string> { "a", "b", "c" });
			Assert.AreEqual(qa.Question, "Please specify a value [a/b/c] (C=Cancel)");
		}

		[Test()]
		public void QAManagerConstructOptional()
		{
			// Type 2 - 1 parameter
			qa = new QAManager(Enums.QAType.Str, "NoQuestion at all");
			Assert.AreEqual(qa.Question, "Please specify a value (Default=NoQuestion at all, C=Cancel)");
			// Type 2 - 2 parameters
			qa = new QAManager(Enums.QAType.YN, "Duh", "Q with default 'Duh'");
			Assert.AreEqual(qa.Question, "Q with default 'Duh' (Default=Duh, C=Cancel)");
			// Type 3 - 1 parameter
			qa = new QAManager(new List<string> { "a", "b", "c" }, "Duh");
			Assert.AreEqual(qa.Question, "Please specify a value [a/b/c] (Default=Duh, C=Cancel)");
			// Type 3 - 2 parameters
			qa = new QAManager(new List<string> { "a", "b", "c" }, "Duh", "Q with default 'Duh'");
			Assert.AreEqual(qa.Question, "Q with default 'Duh' [a/b/c] (Default=Duh, C=Cancel)");
		}

		[Test()]
		public void ConsoleWrapper()
		{
			// Construct
			cw = new ConsoleWrapper();
			Assert.AreEqual(cw.Values.Count, 1);
			Assert.AreEqual(cw.Values[0], Enums.YN.C);
		}

		[Test()]
		public void SettingsManager_ConfigXml_Content()
		{
			// Does config file exist?
			Assert.IsTrue(File.Exists(configXml));
			// Are the settings for all apps present in the config file?
			string[] apps = new string[] { "txttohtml", "csvtoxml", "logutil", "xmldbmanager" };
			foreach (string app in apps)
			{
				Assert.IsNotNull(new SettingsManager(configXml, app));
			}
		}

		[Test()]
		public void OutputWrapper()
		{
			ow = new OutputWrapper(configXml);
			Assert.AreEqual(ow.IsOutputSwitchedOn, true);
			Assert.IsNotNull(ow.XmlManager);

			// Try ow-dependent tests
			CsvToXml();
			TxtToHtml();
		}

		[Test()]
		public void XmlTableManager()
		{
			// LogFile
			// Create file
			string logFile = Path.Combine(logPath, "xmlLog.xml");
			var tm = new XmlTableManager(logPath, "xmlLog");
			Assert.IsTrue(File.Exists(logFile));
			// Create row
			int id = tm.Create("New row");
			// Last Id must be equal to id created.
			Assert.AreEqual(id, tm.GetLastId());
			// Read the new row
			var row = tm.Read(id);
			Assert.True(row.Contains("New row"));
			// Update the new row
			tm.Update(id, "value", "Updated row");
			row = tm.Read(id);
			Assert.True(row.Contains("Updated row"));
			// Delete the new row
			tm.Delete(id);
			// Read the deleted row, it must be gone.
			row = tm.Read(id);
			Assert.IsNullOrEmpty(row);
		}

		[Test()]
		public void XmlDBManager()
		{
			sm = new SettingsManager(configXml, "xmldbmanager");
			Assert.IsNotNull(sm);
			Assert.IsNotNullOrEmpty(sm.SelectElementValue("baseFolderPath"));
			Assert.IsNotNullOrEmpty(sm.SelectElementValue("dBFileName"));
			// Create file
			string DBPath = Path.Combine(sm.SelectElementValue("baseFolderPath"), sm.SelectElementValue("dBFileName"));
			string DBPath_Test = Path.Combine(sm.SelectElementValue("baseFolderPath"), "DB_Test.xml");
			var dm = new XmlDBManager(configXml);
			// Delete Test DFD
			dm.DFDPath = DBPath_Test;
			dm.DeleteDFD();
			Assert.IsFalse(File.Exists(DBPath_Test));
			// Create Test DFD
			dm.DFDPath = DBPath;
			dm.LoadDFD();
			dm.DFDPath = DBPath_Test;
			dm.SaveDFD();
			Assert.IsTrue(File.Exists(DBPath_Test));
			// Create/delete a table in Test-DFD
			dm.DFDPath = DBPath_Test;
			XElement newTable =
				new XElement("table", new XAttribute("name", "coordinates"),
							 new XElement("Name", new XAttribute("type", "string")),
							 new XElement("Latitude", new XAttribute("type", "int")),
							 new XElement("Longitude", new XAttribute("type", "int")));
			dm.CreateDFDTable(newTable);
			dm.SaveDFD();
			// Table must exist
			Assert.IsNotNull(dm.GetTableRef("coordinates"));
			dm.DeleteDFDTable("coordinates");
			dm.SaveDFD();
			// Table must not exist
			Assert.IsNull(dm.GetTableRef("coordinates"));
			// Create again, now from existing DFD
			XElement newTable2 =
	new XElement("table", new XAttribute("name", "coordinates"),
				 new XElement("Name", new XAttribute("type", "string")),
				 new XElement("Latitude", new XAttribute("type", "int")),
				 new XElement("Longitude", new XAttribute("type", "int")));
			dm.CreateDFDTable(newTable2);
			dm.SaveDFD();
		}

		[Test()]
		public void CsvToXml()
		{
			if (ow != null)
			{
				var c2x = new CsvToXml(ow, configXml);
				Assert.IsNotNull(c2x);
			}
		}

		[Test()]
		public void TxtToHtml()
		{
			if (ow != null)
			{
				var t2h = new TxtToHtml(ow, configXml);
				Assert.IsNotNull(t2h);
			}
		}

		[Test()]
		public void ErrorControl()
		{
			var ec = GeneralUtilities.ErrorControl.Instance();
			Assert.IsNotNull(ec);
			// Add and check 1 warning and 1 error 

			// No userId
			ec.AddWarning("Warning for user 1");
			Assert.AreEqual(1, ec.GetWarningCount(), "No of warnings must be 1");
			ec.AddError("Error key=99999");
			Assert.AreEqual(1, ec.GetErrorCount(), "No of errors must be 1");
			Assert.AreEqual("Error key=99999 ", ec.GetErrorMessageById(99999), "Error text(key=99999) is false");
			var eMsgs = ec.GetErrorMessages();
			Assert.AreEqual(1, eMsgs.Count, "Errormessages should be 1");
			var wMsgs = ec.GetWarningMessages();
			Assert.AreEqual(1, wMsgs.Count, "Warningmessages should be 1");
			ec.InitializeErrorhandling();
			eMsgs = ec.GetErrorMessages();
			wMsgs = ec.GetWarningMessages();
			Assert.AreEqual(0, eMsgs.Count, "Errormessages should be 0");
			Assert.AreEqual(0, wMsgs.Count, "Warningmessages should be 0");

			// With userId
			ec.AddWarning(1, "Warning for user 1", true);
			Assert.AreEqual(1, ec.GetWarningCount(1), "No of warnings for user 1 must be 1");
			ec.AddError(1, "Error key=99999 for user 1");
			Assert.AreEqual(1, ec.GetErrorCount(1), "No of errors for user 1 must be 1");
			Assert.AreEqual("Error key=99999 for user 1 ", ec.GetErrorMessageById(1, 99999), "Error text(key=99999, User=1) is false");
			eMsgs = ec.GetErrorMessages(1);
			Assert.AreEqual(1, eMsgs.Count, "Errormessages should be 1");
			wMsgs = ec.GetWarningMessages(1);
			Assert.AreEqual(1, wMsgs.Count, "Warningmessages should be 1");
			ec.InitializeErrorhandling(1);
			eMsgs = ec.GetErrorMessages(1);
			wMsgs = ec.GetWarningMessages(1);
			Assert.AreEqual(0, eMsgs.Count, "Errormessages should be 0");
			Assert.AreEqual(0, wMsgs.Count, "Warningmessages should be 0");
		}
	}
}
