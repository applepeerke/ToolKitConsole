using GeneralUtilities;
using System;
using System.Text;
using System.Linq;
using System.IO;

namespace GeneralUtilities
{
	public class OutputWrapper : IDisposable
	{
		private const string EMPTY = "";
		private ConsoleWrapper consoleWrapper = new ConsoleWrapper();

		private bool hasConsoleOutput = false;
		private bool hasXmlOutput = false;
		private bool hasLogUtilOutput = false;
		private string stripe;
		private string className;
		private string xmlDirName;
		private string xmlFileName;

		#region Properties
		private XmlTableManager xmlTableManager;
		public XmlTableManager XmlManager
		{
			get { return this.xmlTableManager; }
			set { this.xmlTableManager = value; }
		}
		private bool isOutputSwitchedOn = true;
		public bool IsOutputSwitchedOn
		{
			get { return this.isOutputSwitchedOn; }
			set { this.isOutputSwitchedOn = value; }
		}

		private int width;
		public int Width
		{
			get { return this.width; }
			set { this.width = value; }
		}
		#endregion Properties
		#region Constructor
		public OutputWrapper(string configXml)
		{
			Initialize(configXml);
			// Fill stripe
			StringBuilder sb = new StringBuilder();
			for (uint i = 0; i < Width; i++)
				sb.Append("-");
			stripe = sb.ToString();

			// 
			if (hasLogUtilOutput)
			{
				LogUtil.Instance.Start(configXml);
			}
		}
		#endregion

		#region public methods
		private void Initialize(string configXml)
		{
			className = GetType().Name.Split('.').Last();
			try
			{
				GetSettings(configXml);
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
					hasConsoleOutput = Convert.ToBoolean(settings.SelectElementValue("hasConsoleOutput"));
					hasLogUtilOutput = Convert.ToBoolean(settings.SelectElementValue("hasLogUtilOutput"));
					hasXmlOutput = Convert.ToBoolean(settings.SelectElementValue("hasXmlOutput"));
					xmlDirName = settings.SelectElementValue("xmlDirName");
					xmlFileName = settings.SelectElementValue("xmlFileName");
					// Derived
					if (hasXmlOutput) xmlTableManager = new XmlTableManager(xmlDirName, xmlFileName);
				}
			}
			catch (Exception e)
			{
				throw e;
			}
		}

		// Verify input
		private void Verify()
		{
			Log("=======================================================================");
			Log(string.Format("{0}", className.ToUpper()));
			Log("=======================================================================");
			Log(string.Format("Has Console output  . . . . . . . . . . : {0}", hasConsoleOutput));
			Log(string.Format("Has LogUtil output  . . . . . . . . . . : {0}", hasLogUtilOutput));
			Log(string.Format("Has xml output  . . . . . . . . . . . . : {0}", hasXmlOutput));
			Log(string.Format("Xml folder name . . . . . . . . . . . . : {0}", xmlDirName));
			Log(string.Format("Xml file name . . . . . . . . . . . . . : {0}", xmlFileName));
			Log("=======================================================================");
			Log("Press any key to continue (Ctrl-C = Cancel)");
			ReadKey();
		}
		/// <summary>
		/// Writes the line.
		/// </summary>
		/// <param name="line">Line.</param>
		public void WriteLine(string line)
		{
			if (IsOutputSwitchedOn)
			{
				if (hasConsoleOutput) consoleWrapper.WriteLine(line);
				if (hasLogUtilOutput) LogUtil.Instance.AddLine(line);
				if (hasXmlOutput) xmlTableManager.Create(line);
			}
		}

		public void AddException(Exception ex, string firstText = EMPTY, bool stacktrace = true)
		{
			if (firstText != EMPTY)
			{
				WriteLine(firstText);
				AddStripe();
			}
			WriteLine("Message  . . . : " + ex.Message);
			if (ex.InnerException != null) WriteLine("Inner Exception: " + ex.InnerException.Message);
			if (stacktrace && ex.StackTrace != null)
			{
				WriteLine("Stack trace: ");
				WriteLine(ex.StackTrace);
			}
			AddStripe();
		}

		public void ReadKey()
		{
			if (hasConsoleOutput) consoleWrapper.Pause();
		}
		public void AddStripe()
		{
			WriteLine(stripe);
		}
		public void Log(string line)
		{
			WriteLine(line);
		}
		private void ErrorHandling(Exception e)
		{
			string message = (e.InnerException != null) ? e.InnerException.Message : e.Message;
			WriteLine(string.Format("[ER] - {0}", message));
		}

		public void Dispose()
		{
			if (xmlTableManager != null) xmlTableManager.Dispose();
		}
		#endregion
	}
}
