using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace GeneralUtilities
{
	public class TxtToHtml
	{
		private const string EMPTY = "";
		private string className;
		private OutputWrapper ow;
		private string configFile = string.Empty;

		private List<string> txtFile = new List<string>();
		private List<string> htmlFile = new List<string>();
		private string resourceFolderPath = string.Empty;
		// Config file properties
		private string txtFileNumber = string.Empty;
		private string txtFileName = string.Empty;
		private string fileTitle = string.Empty;
		private string prefix = string.Empty;
		private string baseFolderPath = string.Empty;
		private string resourceFolderName = string.Empty;
		private string inputFileName = string.Empty;
		private string indexType = string.Empty;
		private string indexTitle = string.Empty;
		private string outputPath = string.Empty;
		private string outputSubFolderName = string.Empty;
		private string backgroundImage = string.Empty;
		private string styleSheet = string.Empty;
		private string[] text;

		//==== Create Regex object and pass pattern.
		// indexByNumber:      myFile.html | My title
		// indexByName:        My title    | myFile.html
		// indexByNameTitle:   myFile.html | My title (myNumber)
		// indexByNumberTitle: myFile.html | myNumber - My title
		private Dictionary<string, string> indexByNumber = new Dictionary<string, string>();
		private Dictionary<string, string> indexByName = new Dictionary<string, string>();
		private Dictionary<string, string> indexByNumberTitle = new Dictionary<string, string>();
		private Dictionary<string, string> indexByNameTitle = new Dictionary<string, string>();

		private bool error = false;
		private List<string> errors = new List<string>();

		#region Constructor
		public TxtToHtml(OutputWrapper outputWrapper, string configXml)
		{
			className = this.GetType().Name.Split('.').Last();
			ow = outputWrapper;
			configFile = configXml;
		}
		#endregion
		public void Start()
		{
			Log(string.Format("Starting {0}...", className));

			try
			{
				GetSettings(configFile);
				ConfigureOutput();
				Verify();
				ValidateInput();
				Convert();
				CopyFiles();
				CompletionMessage();
			}
			catch (Exception e)
			{
				if (ow == null) throw e;
				ErrorHandling(e);
			}
		}
		// Processing
		private void GetSettings(string configXml)
		{
			// Try to get directory from .config
			try
			{
				if (string.IsNullOrEmpty(configXml))
				{
					configXml = string.Concat(AppDomain.CurrentDomain.BaseDirectory, System.Reflection.Assembly.GetEntryAssembly().GetName().ToString().Split(',')[0].Trim(), ".config");
				}
				if (File.Exists(configXml))
				{
					SettingsManager settings = new SettingsManager(configXml, className);
					prefix = settings.SelectElementValue("prefix");
					baseFolderPath = settings.SelectElementValue("baseFolderPath");
					resourceFolderName = settings.SelectElementValue("resourceFolderName");
					inputFileName = settings.SelectElementValue("inputFileName");
					indexType = settings.SelectElementValue("indexType");
					indexTitle = settings.SelectElementValue("indexTitle");
					outputPath = settings.SelectElementValue("outputPath");
					outputSubFolderName = settings.SelectElementValue("outputSubFolderName");
					backgroundImage = settings.SelectElementValue("backgroundImage");
					styleSheet = settings.SelectElementValue("styleSheet");
					// Derived
					resourceFolderPath = Path.Combine(baseFolderPath, resourceFolderName);
					if (prefix == string.Empty)
					{
						prefix = Path.GetFileNameWithoutExtension(inputFileName);
					}
				}
			}
			catch (Exception e)
			{
				if (ow == null) throw e;
				ErrorHandling(e);
			}
		}

		// Configure output 
		private void ConfigureOutput()
		{
			// Get output folder (create if not exists)
			if (!Directory.Exists(baseFolderPath))
			{
				Directory.CreateDirectory(baseFolderPath);
			}
			// Get output subfolder (create if not exists)
			outputSubFolderName = Path.Combine(outputPath, outputSubFolderName);
			if (!Directory.Exists(outputSubFolderName))
			{
				Directory.CreateDirectory(outputSubFolderName);
			}
		}

		// Verify input
		private void Verify()
		{
			Log("=======================================================================");
			Log(string.Format("{0}", className.ToUpper()));
			Log("=======================================================================");
			Log(string.Format("Prefix . . . . . . . . . . . . . . . : {0}", prefix));
			Log(string.Format("Input base folderpath  . . . . . . . : {0}", baseFolderPath));
			Log(string.Format("Input subfolder name . . . . . . . . : {0}", resourceFolderName));
			Log(string.Format("Input textfile name  . . . . . . . . : {0}", inputFileName));
			Log(string.Format("Index type . . . . . . . . . . . . . : {0}", indexType));
			if (indexType != "None")
			{
				Log(string.Format("index.htm output folder  . . . . . . : {0}", outputPath));
				Log(string.Format("index.htm title  . . . . . . . . . . : {0}", indexTitle));
				Log(string.Format("HTML output subfolder  . . . . . . . : {0}", outputSubFolderName));
			}
			Log(string.Format("Background image to copy . . . . . . : {0}", backgroundImage));
			Log(string.Format("Stylesheet to copy . . . . . . . . . : {0}", styleSheet));
			Log("=======================================================================");
			Log("Press any key to continue (Ctrl-C = Cancel)");
			ow.ReadKey();
		}

		// Validate input
		private void ValidateInput()
		{
			// Validate stylesheet and background image.

			if (!File.Exists(Path.Combine(resourceFolderPath, styleSheet)))
			{
				errors.Add(string.Format("Stylesheet '{0}' to copy does not exist in '{1}'.", styleSheet, resourceFolderPath));
			}
			if (!File.Exists(Path.Combine(resourceFolderPath, backgroundImage)))
			{
				errors.Add(string.Format("Background image '{0}' to copy does not exist in '{1}'.", backgroundImage, resourceFolderPath));
			}
			// Validate input file

			text = File.ReadAllLines(Path.Combine(baseFolderPath, resourceFolderName, inputFileName), Encoding.GetEncoding("iso-8859-1"));
			Log("-----------------------------------------------------------------------");
			Log("Validating input file...");

			for (int i = 0; i < text.Length; i++)
			{
				// Substitute special characters
				string l = ReplaceSpecialChars(text[i].Trim());
				if (l != text[i])
				{
					text[i] = l;
				}
			}
			// Write errors
			if (errors.Count == 0)
			{
				Log("[OK]");
			}
			foreach (string e in errors)
			{
				Log(e);
			}
			Log("=======================================================================");
			Log("Press any key to continue (Ctrl-C = Cancel)");
			ow.ReadKey();
		}

		// Processing
		private void Convert()
		{
			if (errors.Count > 0) return;

			bool titleMode = false;

			foreach (string t in text)
			{
				string l = t;
				// txtFilenummer: level break
				if (!string.IsNullOrEmpty(l) && l.Substring(0, 1) == "*")
				{

					AddPreviousTxtFile();
					SetTxtFileName(l);
				}
				else
				{
					// txtFile title
					if (titleMode && !string.IsNullOrEmpty(l))
					{
						// Remove trailing ","
						string lastChar = l.Substring(l.Length - 1, 1);
						fileTitle = (lastChar == "," || lastChar == "." || lastChar == ":" || lastChar == ";") ? l.Remove(l.Length - 1, 1) : l;
						titleMode = false;
					}
					// line = Valid line
					txtFile.Add(l);
				}
			}
			// Last time
			AddPreviousTxtFile();
			// index
			AddIndex();

		}

		// Voeg txtFile toe als een HTML page
		private void AddPreviousTxtFile()
		{
			if (txtFile != null && txtFile.Count > 0 && !string.IsNullOrEmpty(txtFileName))
			{
				// Process
				AddHeader(fileTitle);
				AddBody();
				AddFooter();
				// Write txtFile to HTML file.
				WriteToFile(Path.Combine(outputSubFolderName, txtFileName));
				// Add txtFile to index.
				AddFileToIndex();
			}
			// Wrap up
			txtFile.Clear();
		}

		private void AddFileToIndex()
		{
			if (indexType == "None") return;

			// [Nummer | Filenaam] moet bij indexing altijd gevuld worden 
			indexByNumber.Add(txtFileNumber, txtFileName);
			if (indexType == "Number" || indexType == "Both")
			{
				indexByNumberTitle.Add(txtFileNumber, txtFileNumber + " - " + fileTitle);
			}
			if (indexType == "Name" || indexType == "Both")
			{
				int suffix = 0;
				string key = fileTitle;
				while (indexByName.ContainsKey(key))
				{
					suffix += 1;
					key += "-" + suffix.ToString();
				}
				indexByName.Add(key, txtFileNumber);
				indexByNameTitle.Add(txtFileNumber, fileTitle + " (" + txtFileNumber + ")");
			}
		}

		private void AddIndex()
		{
			if (indexType == "None") return;

			// Process
			AddHeader(indexTitle, true);
			AddIndexBody();
			AddFooter();
			WriteToFile(Path.Combine(outputPath, "index.htm"));

			// Wrap up
			txtFile.Clear();
		}

		private void AddHeader(string titel, bool index = false)
		{
			htmlFile.Clear();
			AddTag("!DOCTYPE HTML");
			AddTag("head");
			AddTag("title", titel, true);
			if (index)
			{
				AddTag("link rel = \"stylesheet\" href = \"style.css\"");
			}
			else
			{
				AddTag("link rel = \"stylesheet\" href = \"../style.css\"");
			}
			AddTag("/head");
		}

		private void AddTag(string tag, string value = "", bool close = false)
		{
			string line = "<" + tag + ">";
			// If value specifed, add the value and the end tag.
			if (value != string.Empty)
			{
				line += value;
			}
			if (close)
			{
				line += "</" + tag + ">";
			}
			htmlFile.Add(line);
		}

		private void SetTxtFileName(string l)
		{
			txtFileName = string.Empty;
			if (!string.IsNullOrEmpty(l) && l.Substring(0, 1) == "*")
			{
				// Liednummer kan gevolgd worden door "a".
				txtFileNumber = l.Remove(0, 1);
				int num;
				if (int.TryParse(txtFileNumber, out num))
				{
					txtFileName = (prefix + num.ToString("D3"));
				}
				else
				{
					txtFileName = (prefix + txtFileNumber);
				}
				txtFileName = Regex.Replace(txtFileName, "[^0-9a-zA-Z]+", "_") + ".html";
			}
		}

		private void AddBody()
		{
			AddTag(string.Format("body background=\"../{0}.gif\"", prefix));
			htmlFile.Add("<h1>" + fileTitle + "</h1>");
			htmlFile.Add(string.Format("<DIV class=\"{0}\">", prefix));

			foreach (string t in txtFile)
			{
				string l = t;
				htmlFile.Add(l + "<BR />");
			}
			AddTag("/div");
		}

		private void AddIndexBody()
		{
			AddTag(string.Format("body class=\"{0}\"", prefix));
			htmlFile.Add("<h1>" + indexTitle + "</h1>");
			htmlFile.Add("<DIV class=\"{0}\">");
			if (indexType == "Number" || indexType == "Both")
			{
				var list = indexByNumber.Keys.ToList();
				list.Sort();

				foreach (string key in list)
				{
					AddTag("div");
					// e.g. a href = "\mySubFolder\myFile.html"
					AddTag("a href=\"" + Path.Combine(outputSubFolderName, indexByNumber[key].ToString()) + "\"", indexByNumberTitle[key], true);
					AddTag("/div");
				}
			}
			if (indexType == "Name" || indexType == "Both")
			{
				var list = indexByName.Keys.ToList();
				list.Sort();

				//foreach (KeyValuePair<string, string> kv in indexByName)
				foreach (string key in list)
				{
					string numberKey = indexByName[key];
					AddTag("div");
					AddTag("a href=\"" + Path.Combine(outputSubFolderName, indexByNumber[numberKey].ToString()) + "\"", indexByNameTitle[numberKey], true);
					AddTag("/div");
				}
			}
			AddTag("/div");
		}

		private void AddFooter()
		{
			AddTag("/body");
			AddTag("/html");
		}

		private void WriteToFile(string path)
		{
			Log("Writing " + path + "...");
			File.WriteAllLines(path, htmlFile);
		}

		private void CopyFiles()
		{
			File.Copy(Path.Combine(resourceFolderPath, styleSheet), Path.Combine(baseFolderPath, styleSheet), true);
			File.Copy(Path.Combine(resourceFolderPath, backgroundImage), Path.Combine(baseFolderPath, backgroundImage), true);
		}

		private void CompletionMessage()
		{
			Log("-----------------------------------------------------");
			if (!error && errors.Count == 0)
			{
				Log("[OK]");
			}
			else
			{
				Log("[ER] - Processing ended ABNORMALLY. See previous errors.");
			}
			Log("-----------------------------------------------------");
			ow.ReadKey();
		}

		private string ReplaceSpecialChars(string input)
		{
			// This regex matches either one of the special characters.
			Regex regex = new Regex("[âéëèê]");

			var map = new Dictionary<string, string> {
			{ "â", "&acirc;"},
			{ "é", "&eacute;"},
			{ "ë", "&euml;" },
			{ "è", "&egrave;" },
			{ "ê", "&ecirc;" }
			};

			// Use the dictionary to map the character.
			string output = regex.Replace(input,
				m => map[m.Value]);
			return output;
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
	}

}
