using System.IO;

namespace GeneralUtilities
{
    public static class Serializer
    {

        public static void Serialize<T>(T objectToSerialize, string path)
        {

			System.Xml.Serialization.XmlSerializer writer = new System.Xml.Serialization.XmlSerializer(typeof(T));

            StreamWriter file = new StreamWriter(path);
            writer.Serialize(file, objectToSerialize);
            file.Close();
        }

        public static T Deserialize<T>(string path)
        {

            T objectToDeserialize = default(T);

			System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(T));

            StreamReader reader = new StreamReader(path);
            objectToDeserialize = (T)serializer.Deserialize(reader);
            reader.Close();

            return objectToDeserialize;
        }
    }
}
