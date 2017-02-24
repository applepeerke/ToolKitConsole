using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using GeneralUtilities.Data;

namespace GeneralUtilities
{
	/// <summary>
	/// Settings manager.
	/// Maintains the settings for 1 app, and the general <app> settings.
	/// </summary>
	public class SettingsManager
	{
		private XmlDocument xmlDocument;
		private XmlNode parentNode;
		private string xmlPath;
		private string parentNodeRoot = "/configuration/appSettings/";

		#region Constructor
		public SettingsManager(string path, string appName)
		{
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
				if (xmlDocument == null)
				{
					throw new ApplicationException(string.Format("Xml document '{0}' could not be loaded", xmlPath));
				}
			}
			GetSettings(appName);
		}
		#endregion
		#region public methods
		public Dictionary<string, string> GetSettings(string appname)
		{
			var settings = new Dictionary<string, string>();

			if (!string.IsNullOrEmpty(appname))
			{
				string parentNodePath = string.Concat(parentNodeRoot, appname.ToLower());
				parentNode = xmlDocument.SelectSingleNode(parentNodePath);

				if (parentNode != null)
				{
					foreach (XmlNode node in parentNode.ChildNodes)
					{
						settings.Add(node.Name, node.InnerText);
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
