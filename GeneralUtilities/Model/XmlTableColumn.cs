using System;
using System.Xml.Serialization;
using System.ComponentModel;

namespace GeneralUtilities
{
    public class XmlTableColumn
    {
		// Constructors
        public XmlTableColumn()
        {
        }

        public XmlTableColumn(string name, string value)
        {
            Name = name;
            Value = value;
            DataType = typeof(string).Name;
        }

        public XmlTableColumn(string name, int value)
        {
            Name = name;
            Value = value.ToString();
            DataType = typeof(int).Name;
        }

        public XmlTableColumn(string name, DateTime value)
        {
            Name = name;
            if ((value.ToOADate() - (int)(value.ToOADate())) > 0)
            {
                Value = value.ToString("yyyyMMddTHHmmss");
            }
            else
            {
                Value = value.ToString("yyyyMMdd");
            }
            DataType = typeof(DateTime).Name;
        }
		// Data
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("value")]
        public string Value { get; set; }

        [DefaultValue("string"), XmlAttribute("datatype")]
        public string DataType { get; set; }

        [XmlAttribute("optional")]
        public bool Optional { get; set; }
    }
}
