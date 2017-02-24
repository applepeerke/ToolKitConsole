using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace GeneralUtilities
{
	public class CsvToXml
	{
		private const string EMPTY = "";
		private string className;
		private OutputWrapper ow;
		private string xmlFile = string.Empty;
		private string configFile = string.Empty;
		private string inputFilePath = string.Empty;
		// Settings
		private string baseFolderPath = string.Empty;
		private string outputPath = string.Empty;
		private char splitChar = ';';
		private string inputFileName = string.Empty;
		private string outputFolder = string.Empty;

		#region Constructor
		public CsvToXml(OutputWrapper outputWrapper, string configXml)
		{
			className = this.GetType().Name.Split('.').Last();
			ow = outputWrapper;
			configFile = configXml;
		}
		#endregion
		#region public methods
		public void Start()
		{
			Log(string.Format("Starting {0}...", className));

			try
			{
				GetSettings(configFile);
				ConfigureOutput();
				Verify();
				ValidateInput();
				ConvertToXml();
				Log(string.Format("XML file '{0}' has been created in folder '{1}'", xmlFile, outputFolder));
			}
			catch (Exception e)
			{
				if (ow == null) throw e;
				ErrorHandling(e);
			}
		}
		#endregion
		#region private methods
		private void GetSettings(string configXml)
		{
			// Get the properties from the .config
			if (string.IsNullOrEmpty(configXml))
			{
				configXml = string.Concat(AppDomain.CurrentDomain.BaseDirectory, System.Reflection.Assembly.GetEntryAssembly().GetName().ToString().Split(',')[0].Trim(), ".config");
			}
			if (File.Exists(configXml))
			{
				SettingsManager settings = new SettingsManager(configXml, className);
				baseFolderPath = settings.SelectElementValue("baseFolderPath");
				outputPath = settings.SelectElementValue("outputPath");
				inputFileName = settings.SelectElementValue("inputFileName");
				splitChar = Convert.ToChar(settings.SelectElementValue("splitChar"));
			}
		}

		private void ConfigureOutput()
		{
			// outputfolder is a subfolder of the basefolder.
			outputFolder = Path.Combine(baseFolderPath, outputPath);
			if (!Directory.Exists(outputFolder))
			{
				Directory.CreateDirectory(outputFolder);
			}
		}

		private void Verify()
		{
			Log("=======================================================================");
			Log(string.Format("{0}", className.ToUpper()));
			Log("=======================================================================");
			Log(string.Format("Csv file base folderpath . . . . . . : {0}", baseFolderPath));
			Log(string.Format("Textfile input file name   . . . . . : {0}", inputFileName));
			Log(string.Format("XML output root folder name  . . . . : {0}", outputPath));
			Log(string.Format("XML output folder name   . . . . . . : {0}", outputFolder));
			Log(string.Format("Split character  . . . . . . . . . . : '{0}'", splitChar));
			Log("=======================================================================");
			Log("Press any key to continue (Ctrl-C = Cancel)");
			ow.ReadKey();
		}
		private void ValidateInput()
		{
			inputFilePath = Path.Combine(baseFolderPath, inputFileName);
			if (!File.Exists(inputFilePath))
			{
				Log(string.Format("CsvToXml reports: Input path '{0}' as specified in '{1}' does not exist.", inputFilePath, configFile));
				return;
			}
		}

		private void ConvertToXml()
		{
			string tableName = Path.GetFileNameWithoutExtension(inputFileName);
			xmlFile = string.Concat(tableName, ".xml");
			string outputFilePath = Path.Combine(outputFolder, xmlFile);

			try
			{
				var lines = File.ReadAllLines(inputFilePath);

				var xml = new XElement(tableName,
				   lines.Select(line => new XElement("Row",
					  line.Split(splitChar)
						  .Select((column, index) => new XElement("Col" + index, column)))));

				xml.Save(outputFilePath);
			}
			catch (Exception e)
			{
				if (ow == null) { throw e; } else { ow.AddException(e); }
			}
		}

		// Log this instance.
		private void Log(string line)
		{
			if (ow != null) ow.WriteLine(line);
		}
		private void ErrorHandling(Exception e)
		{
			if (ow == null) throw e;
			string message = (e.InnerException != null) ? e.InnerException.Message : e.Message;
			Log(string.Format("[ER] - {0}", message));
		}
		#endregion
	}
}