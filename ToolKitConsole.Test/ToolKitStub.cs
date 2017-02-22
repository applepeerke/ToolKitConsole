using System;
using System.IO;
using UtilConsole;
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
		static ConsoleWrapper cw = new ConsoleWrapper();
		static OutputWrapper ow;
		static XmlTableManager xm;
		static XmlDBManager xDbM;
		static TxtToHtml txtToHtml;
		static CsvToXml csvToXml;
		static string configXml;
		static string configXmlName = "ToolKitConsole.config";
		static string rootDir = "/users/peterwerk/Projects/";
		static string logDir = "/users/peterwerk/Projects/Log";
		static string resourcesDir = "/users/peterwerk/Projects/TestResources/";

		public static void Main(string[] args)
		{
			configXml = (Path.Combine(resourcesDir, configXmlName));
			using (ow = new OutputWrapper(configXml))
			{
				using (xm = new XmlTableManager(logDir, "xmlLog"))
				{
					ow.XmlManager = xm;
					LogUtil.Instance.Start(Path.Combine(rootDir, configXml));
					Process();

				}
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
			Log("Starting creating GraphViz...");
			List<string> extensions = new List<string>() { "png", "ico", "tiff", "jpg" };
			List<string> commands = new List<string>() { "all", "dot", "neato", "fdp", "sfdp", "twopi", "circo" };
			// Input file
			string dir = resourcesDir;
			string Yes = Enums.YN.Y.ToString();
			string answer = cw.Ask(new QAManager(Enums.QAType.YN, Yes, string.Format("Use directory '{0}'?", resourcesDir)));
			if (answer != Yes)
			{
				dir = cw.AskDir(new QAManager(Enums.QAType.Dir));
			}
			string inputFile = "GraphViz_Color.dot";
			answer = cw.Ask(new QAManager(Enums.QAType.YN, Yes, string.Format("Use filename '{0}'?", inputFile)));
			if (answer != Yes)
			{
				inputFile = cw.AskFileName(new QAManager(Enums.QAType.File), dir);
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
					string filename = Path.GetFileNameWithoutExtension(inputFile);
					string path = Path.Combine(dir, inputFile);
					string outpFile = string.Concat(inputFile, ".", extension);
					Log("-------------------------------------------------------------------");
					Log(string.Format("Input file...........: {0}", inputFile));
					Log(string.Format("Output file..........: {0}", outpFile));
					Log(string.Format("Output folder........: {0}", dir));
					Log(string.Format("GraphViz command.....: {0}", command));
					Log(string.Format("Extension............: {0}", extension));
					Log("-------------------------------------------------------------------");
					cw.Pause();
					// Output file
					string outPathPrefix = Path.Combine(dir, filename);
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
				Log(output);
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
