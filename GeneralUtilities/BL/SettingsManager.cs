using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
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
				xmlPath = string.Empty;
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
		public Dictionary<string, string> GetSettings(string appname)
		{
			var settings = new Dictionary<string, string>();
			if (!string.IsNullOrEmpty(xmlPath))
			{
				XDocument doc = XDocument.Load(xmlPath);
				var appName = doc.Element(appname);
				if (!string.IsNullOrEmpty(appname))
				{
					var elems = appName.Descendants().ToArray();
					foreach (XElement e in elems)
					{
						settings.Add(e.Name.ToString(), e.Value);
					}
				}
			}
			return settings;
		}

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
