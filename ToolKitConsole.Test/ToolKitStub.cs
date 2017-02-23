using System;
using System.IO;
using GeneralUtilities;
using GeneralUtilities.Data;
using System.Collections.Generic;
using System.Diagnostics;

namespace ToolKitConsole.Test
{
	class MainClass
	{
		const string EMPTY = "";
		const string OK = "[OK]";
		static string configXml = "/Users/peterwerk/GitHub/ToolKitConsole/GeneralUtilities/ToolKitConsole.config";
		static ConsoleWrapper cw = new ConsoleWrapper();
		static OutputWrapper ow;
		static XmlTableManager xm;
		static XmlDBManager xDbM;
		static TxtToHtml txtToHtml;
		static CsvToXml csvToXml;
		static string logPath;
		static string resourcesPath;

		public static void Main(string[] args)
		{
			GetSettings();
			using (ow = new OutputWrapper(configXml))
			{
				using (xm = new XmlTableManager(logPath, "xmlLog"))
				{
					ow.XmlManager = xm;
					LogUtil.Instance.Start(Path.Combine(logPath, configXml));
					Process();
				}
			}
		}
		private static void GetSettings()
		{
			try
			{
				if (File.Exists(configXml))
				{
					SettingsManager settings = new SettingsManager(configXml, "app");
					//root = settings.SelectElementValue("root");
					logPath = settings.SelectElementValue("logPath");
					resourcesPath = settings.SelectElementValue("resourcesPath");
					//outputPath = settings.SelectElementValue("outputPath");
				}
			}
			catch (Exception e)
			{
				throw e;
			}
		}
		private static void Process()
		{
			do
			{
				Log("-----------------------------");
				Log("  GV = GraphViz");
				Log("  CX = Convert .csv to .xml");
				Log("  TH = Convert .txt to .html");
				Log("  XT = XML table");
				Log("-----------------------------");
				List<string> commands = new List<string>() { "GV", "CX", "TH", "XT" };

				string command = cw.Ask(new QAManager(commands, "GV", "Select a tool"));
				switch (command.ToUpper())
				{
					case "C":
						{
							return;
						}
					case "GV":
						{
							GraphViz();
							break;
						}
					case "CX":
						{
							csvToXml = new CsvToXml(ow, configXml);
							csvToXml.Start();
							break;
						}
					case "TH":
						{
							txtToHtml = new TxtToHtml(ow, configXml);
							txtToHtml.Start();
							break;
						}
					case "XT":
						{
							xDbM.CreateDFD();
							xDbM.SaveDFD();
							break;
						}
				}
				Log(OK);
				xm.Save();
			} while (true);
		}

		private static void GraphViz()
		{
			var settings = new SettingsManager(configXml, "graphviz");
			string baseFolderPath = settings.SelectElementValue("baseFolderPath");
			string inputFileName = settings.SelectElementValue("inputFileName");
			string outputRootFolderName = settings.SelectElementValue("outputRootFolderName");
			Log("Starting creating GraphViz...");
			List<string> extensions = new List<string>() { "png", "ico", "tiff", "jpg" };
			List<string> commands = new List<string>() { "all", "dot", "neato", "fdp", "sfdp", "twopi", "circo" };
			// Input file
			string inputFolder = baseFolderPath;
			string Yes = Enums.YN.Y.ToString();
			string answer = cw.Ask(new QAManager(Enums.QAType.YN, Yes, string.Format("Use input directory '{0}'?", inputFolder)));
			if (answer != Yes)
			{
				inputFolder = cw.AskDir(new QAManager(Enums.QAType.Dir));
			}
			answer = cw.Ask(new QAManager(Enums.QAType.YN, Yes, string.Format("Use filename '{0}'?", inputFileName)));
			if (answer != Yes)
			{
				inputFileName = cw.AskFileName(new QAManager(Enums.QAType.File), inputFolder);
			}

			string command = cw.Ask(new QAManager(commands, "all", "GraphViz command"));
			string extension = cw.Ask(new QAManager(extensions, "png", "Extension"));
			// Canceled
			if (cw.CancelMode)
			{
				Log(string.Format("Processing has been canceled by the enduser."));
			}
			else
			{
				try
				{
					// Verify
					string filename = Path.GetFileNameWithoutExtension(inputFileName);
					string path = Path.Combine(inputFolder, inputFileName);
					string outpFile = string.Concat(filename, ".", extension);
					Log("-------------------------------------------------------------------");
					Log(string.Format("Input folder.........: {0}", inputFolder));
					Log(string.Format("Input file...........: {0}", inputFileName));
					Log(string.Format("Output file..........: {0}", outpFile));
					Log(string.Format("Output folder........: {0}", outputRootFolderName));
					Log(string.Format("GraphViz command.....: {0}", command));
					Log(string.Format("Extension............: {0}", extension));
					Log("-------------------------------------------------------------------");
					cw.Pause();
					// Output file
					string outPathPrefix = Path.Combine(outputRootFolderName, filename);
					if (command.ToLower() == "all")
					{
						ExecuteGraphViz("dot", path, outPathPrefix, extension);
						ExecuteGraphViz("neato", path, outPathPrefix, extension);
						ExecuteGraphViz("fdp", path, outPathPrefix, extension);
						ExecuteGraphViz("sfdp", path, outPathPrefix, extension);
						ExecuteGraphViz("twopi", path, outPathPrefix, extension);
						ExecuteGraphViz("circo", path, outPathPrefix, extension);
					}
					else
					{
						ExecuteGraphViz(command, path, outPathPrefix, extension);
					}
				}
				catch (Exception e)
				{
					LogUtil.Instance.AddException(e);
				}
			}
		}
		public static void ExecuteGraphViz(string cmd, string inpF, string outpP, string ext)
		{
			ExecuteCommandSync(cmd, string.Format("{1} -o{2}_{0}.{3} -T{3}", cmd, inpF, outpP, ext));
		}
		public static void ExecuteCommandSync(string command, string args)
		{
			try
			{
				Process p = new Process();
				p.StartInfo.UseShellExecute = false;
				p.StartInfo.RedirectStandardOutput = true;
				p.StartInfo.FileName = command;
				p.StartInfo.Arguments = args;
				p.Start();

				// To avoid deadlocks, always read the output stream first and then wait.
				string output = p.StandardOutput.ReadToEnd();

				if (!string.IsNullOrEmpty(output)) { Log(output);}
				else Log(string.Format("executed {0} {1}", command, args));

				p.WaitForExit();
			}
			catch (Exception e)
			{
				ErrorHandling(e);
			}
		}
		// Log this instance.
		private static void Log(string line)
		{
			if (ow != null) ow.WriteLine(line);
		}
		private static void ErrorHandling(Exception e)
		{
			if (ow == null) throw e;
			string message = (e.InnerException != null) ? e.InnerException.Message : e.Message;
			Log(string.Format("[ER] - {0}", message));
		}
	}
}
