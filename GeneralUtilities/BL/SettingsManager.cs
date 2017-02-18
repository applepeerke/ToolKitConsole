using System;
using System.IO;
using System.Xml;
using GeneralUtilities.Data;

namespace GeneralUtilities
{
	public class SettingsManager
	{
		private XmlDocument xmlDocument;
		private XmlNode parentNode;
		private string parentNodeName;
		private string xmlPath;
		private string parentNodeRoot = "/configuration/appSettings/";

		#region Constructor
		public SettingsManager(string path, string subNodeName)
		{
			parentNodeName = string.Concat(parentNodeRoot, subNodeName.ToLower());
			xmlPath = path;
			if (string.IsNullOrEmpty(xmlPath) || !File.Exists(xmlPath))
			{
				parentNode = null;
				throw new ApplicationException(string.Format("Xml path not found: '{0}'", xmlPath));

			}
			else
			{
				xmlDocument = new XmlDocument();
				xmlDocument.Load(xmlPath);
				parentNode = xmlDocument.SelectSingleNode(parentNodeName);
				if (parentNode == null)
				{
					throw new ApplicationException(string.Format("ParentNode name '{0}' not found in '{1}'", parentNodeName, xmlPath));
				}
			}
		}
		#endregion Constructor
		#region public methods
		public string SelectElementValue(string name, AsT asT = AsT.strT)
		{
			// Default return value
			string result;
			switch (asT)
			{
				case AsT.boolT:
					{
						result = "false";
						break;
					}
				case AsT.intT:
					{
						result = "0";
						break;
					}
				default:
					{
						result = string.Empty;
						break;
					}
			}

			// Process
			if (parentNode != null)
			{
				XmlNode node = parentNode.SelectSingleNode(name);
				if (node != null)
				{
					string innerText = node.InnerText.Trim('"').TrimEnd('>');
					if (!string.IsNullOrEmpty(innerText))
					{
						result = innerText;
					}
				}
			}
			return result;
		}
		#endregion public methods
	}
}
