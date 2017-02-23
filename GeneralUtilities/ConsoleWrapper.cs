using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace GeneralUtilities
{
	public class ConsoleWrapper
	{
		public static void Main(string[] args)
		{

		}
		private bool error = false;
		private const string EMPTY = "";
		private string CancelCode = Enums.YN.C.ToString().ToUpper();
		// Properties
		private bool cancelMode = false;
		public bool CancelMode { get { return this.cancelMode; } set { this.cancelMode = value; } }
		private bool showCancelText = true;
		public bool ShowCancelText { get { return this.showCancelText; } set { this.showCancelText = value; } }
		private string cancelText = string.Format("({0}=Cancel)", Enums.YN.C.ToString());
		public string CancelText { get { return this.cancelText; } set { this.cancelText = value; } }
		private string defaultText = " Default={0}";
		public string DefaultText { get { return this.defaultText; } set { this.defaultText = value; } }
		private List<Enums.YN> values = new List<Enums.YN>();
		public List<Enums.YN> Values { get { return this.values; } set { this.values = value; } }
		private List<string> valueList = new List<string>();
		public List<string> ValueList { get { return this.valueList; } set { this.valueList = value; } }

		// Constructor
		public ConsoleWrapper()
		{
			Values.Add(Enums.YN.C);
		}

		// Asks the value.
		public string Ask(QAManager qa)
		{
			if (CancelMode) return CancelCode;
			string answer = string.Empty;

			WriteLine(string.Format("{0}", qa.Question));
			answer = ReadLine(qa);
			if (CancelMode) return string.Empty;
			return (qa.IsAnswerUC) ? answer.ToUpper() : answer;
		}
		// Asks the valid filename (incl. path).
		public string AskFileName(QAManager qa, string dir)
		{
			if (CancelMode) return CancelCode;

			string filename;
			do
			{
				error = false;

				WriteLine(qa.Question);
				filename = ReadLine(qa);
				if (CancelMode) return string.Empty;
				if (string.IsNullOrEmpty(filename) || filename.IndexOfAny(Path.GetInvalidFileNameChars()) != -1)
				{
					error = true;
					WriteLine(string.Format("'{0}' is not a valid file name.", filename));
				}
				string ext = Path.GetExtension(filename);
				if (string.IsNullOrEmpty(ext)) { filename = string.Format("{0}.txt", filename); }
				string path = Path.Combine(dir, filename);

				if (qa.IsInputRequired && !File.Exists(path))
				{
					error = true;
					WriteLine(string.Format("Path '{0}' does not exist.", path));
				}
			} while (error);
			return (error) ? string.Empty : filename;
		}

		// Asks the valid path.
		public string AskDir(QAManager qa)
		{
			if (cancelMode) return CancelCode;
			string dir = string.Empty;
			do
			{
				WriteLine(qa.Question);
				dir = ReadLine(qa);
				if (CancelMode) return string.Empty;
				if (dir.IndexOfAny(Path.GetInvalidPathChars()) != -1)
				{
					error = true;
					WriteLine(string.Format("'{0}' is not a valid directory name.", dir));
				}
				if (!error && string.IsNullOrEmpty(dir))
				{
					dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase).Replace("file:", "");
					string answer = Ask(new QAManager(Enums.QAType.YN, dir, "Directory"));
					if (CancelMode) return string.Empty;
					if (answer == Enums.YN.N.ToString()) { dir = string.Empty; }
				}
				if (!Directory.Exists(dir))
				{
					error = true;
					WriteLine(string.Format("Directory '{0}' does not exist.", dir));
				}
			} while (error);
			return (error) ? string.Empty : dir;
		}

		// Reads the line.
		public string ReadLine(QAManager qa)
		{
			bool first = true;
			bool substituted = false;
			if (cancelMode) return CancelCode;
			string answer;
			string errorline = string.Empty;
			do
			{
				if (!first) WriteLine(errorline);
				first = false;
				answer = Console.ReadLine();
				// Cancel
				if (ShowCancelText && (answer.ToUpper() == CancelCode))
				{
					cancelMode = true;
					break;
				}
				// Default substitution
				if (string.IsNullOrEmpty(answer) && answer != qa.DefaultValue)
				{
					answer = qa.DefaultValue;
					substituted = true;
				}
				// Valid?
				errorline = EMPTY;
				if (qa.IsInputRequired == true && string.IsNullOrEmpty(answer))
				{
					errorline = "A value is required.";
				}
				else if (qa.AnswerValues != null &&
						 answer != qa.DefaultValue &&
						 !qa.AnswerValues.Contains(answer.ToUpper()))
				{
					errorline = string.Format("Value '{0}' is not supported.", answer);
				}
			} while (!(errorline == EMPTY));

			if (substituted) WriteLine(string.Format("[{0}]", answer));
			return answer;
		}

		// Writes the line.
		public void WriteLine(string text = "")
		{
			if (cancelMode) return;
			Console.WriteLine(text);
		}

		// Pause the specified pauseText.
		public void Pause(string pauseText = "Press any key to continue.")
		{
			if (!string.IsNullOrEmpty(pauseText)) { WriteLine(pauseText); }
			Console.ReadKey();
		}
	}
}