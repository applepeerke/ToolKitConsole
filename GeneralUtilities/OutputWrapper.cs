using UtilConsole;
using GeneralUtilities.Data;
using System;
using System.Text;

namespace GeneralUtilities
{
	public class OutputWrapper : IDisposable
	{
		private const string EMPTY = "";
		private const string LOGROW = "row";
		private ConsoleWrapper consoleWrapper = new ConsoleWrapper();

		private bool IsConsoleOutput = false;
		private bool IsXmlOutput = false;
		private bool IsLogUtilOutput = false;
		private string stripe;

		#region Properties
		private XmlManager xmlManager;
		public XmlManager XmlManager
		{
			get { return this.xmlManager; }
			set { this.xmlManager = value; }
		}
		private bool isOutputSwitchedOn = true;
		public bool IsOutputSwitchedOn
		{
			get { return this.isOutputSwitchedOn; }
			set { this.isOutputSwitchedOn = value; }
		}
		private string xmlLogTable = "xmlLog";
		public string XmlLogTable
		{
			get { return this.xmlLogTable; }
			set { this.xmlLogTable = value; }
		}
		private int width;
		public int Width
		{
			get { return this.width; }
			set { this.width = value; }
		}
		#endregion Properties
		#region Constructor
		public OutputWrapper(Output output, string configXml = EMPTY)
		{
			// Fill stripe
			StringBuilder sb = new StringBuilder();
			for (uint i = 0; i < Width; i++)
				sb.Append("-");
			stripe = sb.ToString();

			if (output == Output.Console) IsConsoleOutput = true;
			else if (output == Output.Xml) IsXmlOutput = true;
			else if (output == Output.LogUtil) IsLogUtilOutput = true;
			else if (output == Output.All)
			{
				IsConsoleOutput = true;
				if (!string.IsNullOrEmpty(configXml))
				{
					IsXmlOutput = true;
				}
				IsLogUtilOutput = true;
			}
			// Xml output
			if (IsXmlOutput)
			{
				using (xmlManager = new XmlManager(configXml))
				{
					xmlManager.Execute(CRUD.Delete, ObjectType.Table, XmlLogTable);
					xmlManager.Execute(CRUD.Create, ObjectType.Table, XmlLogTable);
				}
			}
			// 
			if (IsLogUtilOutput)
			{
				LogUtil.Start(configXml);
			}
		}
		#endregion Constructor

		#region public methods
		/// <summary>
		/// Writes the line.
		/// </summary>
		/// <param name="line">Line.</param>
		public void WriteLine(string line)
		{
			if (IsOutputSwitchedOn)
			{
				if (IsConsoleOutput) consoleWrapper.WriteLine(line);
				if (IsLogUtilOutput) LogUtil.AddLine(line);
				if (IsXmlOutput) xmlManager.Execute(CRUD.Create, ObjectType.Row, LOGROW, line, XmlLogTable);
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
			if (IsConsoleOutput) consoleWrapper.Pause();
		}
		public void AddStripe()
		{
			WriteLine(stripe);
		}
		private void SwitchOn()
		{
		}

		private void ErrorHandling(Exception e)
		{
			string message = (e.InnerException != null) ? e.InnerException.Message : e.Message;
			WriteLine(string.Format("[ER] - {0}", message));
		}

		public void Dispose()
		{
			if (xmlManager != null) xmlManager.Save();
		}
		#endregion
	}
}
