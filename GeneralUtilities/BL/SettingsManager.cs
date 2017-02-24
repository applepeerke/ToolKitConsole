using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
		private string parentNodeRoot = "/configuration/appSettings/";
		private string APPLICATION = "app";
		private XmlDocument xmlDocument;
		private XmlNode parentAppNode;
		private XmlNode parentNode;
<<<<<<< HEAD
		private string appNodeName;
=======
>>>>>>> XML-Database
		private string xmlPath;
		private Dictionary<string, string> appSettings;

		#region Constructor
		public SettingsManager(string path, string appName)
		{
			xmlPath = path;

			if (string.IsNullOrEmpty(xmlPath) || !File.Exists(xmlPath))
			{
				xmlPath = string.Empty;
				throw new ApplicationException(string.Format("Xml path not found: '{0}'", xmlPath));
			}

			xmlDocument = new XmlDocument();
			xmlDocument.Load(xmlPath);

			appNodeName = string.Concat(parentNodeRoot, APPLICATION.ToLower());
			string parentNodeName = string.Concat(parentNodeRoot, subNodeName.ToLower());

			// Populate a dictionary with the general <app> settings.
			parentAppNode = GetParentNode(appNodeName);
			appSettings = GetSettings(APPLICATION);

			// Get the requested parent node.
			if (parentNodeName == appNodeName)
			{
<<<<<<< HEAD
				parentNode = parentAppNode;
			}
			else
			{
				parentNode = GetParentNode(parentNodeName);
=======
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
>>>>>>> XML-Database
			}
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
						result = SubstituteSpecialValues(innerText);
					}
				}
			}
			return result;
		}

		private XmlNode GetParentNode(string nodeName)
		{
			var node = xmlDocument.SelectSingleNode(nodeName);
			if (node == null)
			{
				throw new ApplicationException(string.Format("ParentNode name '{0}' not found in '{1}'", nodeName, xmlPath));
			}
			return node;
		}
		private string SubstituteSpecialValues(string name)
		{
			string result = name;
			if (appSettings != null && appSettings.Count > 0 && name.StartsWith("*app", StringComparison.CurrentCulture))
			{
				var key = name.Remove(0, 4).ToLower();
				if (!string.IsNullOrEmpty(key))
				{
					if (appSettings.ContainsKey(key))
					{
						result = appSettings[key];
					}
				}
			}
			return result;
		}
		#endregion
		#region public methods
		// Returns a dict of application settings. Key is in lowercase.
		public Dictionary<string, string> GetSettings(string appname)
		{
			var settings = new Dictionary<string, string>();

			if (!string.IsNullOrEmpty(xmlPath))
			{
				XmlNode parentN = parentNode;
				// <app> is a special node
				if (appname == APPLICATION) parentN = parentAppNode;

				if (parentN != null)

					foreach (XmlNode node in parentN.ChildNodes)
					{
						settings.Add(node.Name.ToLower(), SubstituteSpecialValues(node.InnerText));
					}
			}
			return settings;
		}

		#endregion public methods
	}
}
