using System;
using System.Collections.Generic;

namespace GeneralUtilities
{
	public class QAManager
	{
		private const string EMPTY = "";

		public string Question { get; set; }
		public Enums.QAType QAType { get; set; }
		public bool IsInputRequired { get; set; }
		public List<string> AnswerValues { get; set; }
		public bool ShowCancelText { get; set; } = true;
		public string CancelText { get; set; } = "C=Cancel";
		public string DefaultText { get; set; } = "Default={0}";

		#region Properties
		private bool isAnswerUC = false;
		public bool IsAnswerUC { get { return isAnswerUC; } }
		private string defaultValue = string.Empty;
		public string DefaultValue { get { return defaultValue; } }
		#endregion

		#region Constructors
		public QAManager(string questionText)
		{
			Question = questionText;
			QAType = Enums.QAType.Str;
			AnswerValues = new List<string>() { "C" };
			SetProperties();
		}
		public QAManager(Enums.QAType qaType, string answerDft = EMPTY, string questionText = EMPTY)
		{
			Question = questionText;
			QAType = qaType;
			AnswerValues = new List<string>() { "C" };
			defaultValue = answerDft;
			SetProperties();
		}
		public QAManager(List<string> answerValues, string answerDft = EMPTY, string questionText = EMPTY)
		{
			Question = questionText;
			QAType = Enums.QAType.Str;
			AnswerValues = answerValues;
			defaultValue = answerDft;
			SetProperties();
		}
		#endregion

		#region private methods
		private void SetProperties()
		{
			// Set questions
			if (Question == EMPTY)
			{
				if (QAType == Enums.QAType.Dir) Question = "Please specify an existing directory";
				else if (QAType == Enums.QAType.File) Question = "Please specify a valid filename";
				else Question = "Please specify a value";
			}
			// Set defaults
			if (QAType == Enums.QAType.YN && defaultValue == EMPTY) defaultValue = Enums.YN.Y.ToString();
			// Set "Input required?".
			// - Default present: Default is taken if nothing specified --> Input not required.
			// - Else, ico "List of values": value must be specified.
			IsInputRequired = false;
			isAnswerUC = false;
			if ((QAType == Enums.QAType.YN) || (QAType == Enums.QAType.ListValue))
			{
				if (defaultValue == EMPTY) IsInputRequired = true;
				if (defaultValue.Length > 1) isAnswerUC = true;
			}
			SetQuestion();
		}

		// Gets the question text.
		private void SetQuestion()
		{
			// a. Values text
			string valuestext = EMPTY;
			if (AnswerValues != null)
			{
				// Instead of "... [C]? (C=Cancel)", display "...? (C=Cancel)"
				if (!ShowCancelText || AnswerValues.Count > 1 || AnswerValues[0] != "C")
				{
					valuestext = string.Format(" [{0}]", string.Join("/", AnswerValues));
				}
			}
			// b. Default text
			string defaulttext = defaultValue != EMPTY ? string.Format(DefaultText, defaultValue) : string.Empty;
			// c. Cancel text
			string cancelText = ShowCancelText ? CancelText : string.Empty;

			// 4 options
			if ((defaulttext == string.Empty) && (cancelText == string.Empty)) Question = string.Format("{0}{1}", Question, valuestext);
			else if (defaulttext == string.Empty) Question = string.Format("{0}{1} ({2})", Question, valuestext, cancelText);
			else if (cancelText == string.Empty) Question = string.Format("{0}{1} ({2})", Question, valuestext, defaulttext);
			else Question = string.Format("{0}{1} ({2}, {3})", Question, valuestext, defaulttext, CancelText);
		}
		#endregion
	}
}