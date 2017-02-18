using System;
using System.IO;
using System.Text;

namespace GeneralUtilities
{
    public static class XmlSerializer
    {
        public static void Serialize<T>(T objectToSerialize, string path)
        {
            if (objectToSerialize == null)
            {
                throw new Exception("Serialize<T>, parameter objectToSerialize is null");
            }

            StreamWriter file = null;
			System.Xml.Serialization.XmlSerializer writer = new System.Xml.Serialization.XmlSerializer(typeof(T));
            try
            {
                file = new StreamWriter(path);
                writer.Serialize(file, objectToSerialize);
            }
            catch (Exception ex)
            {
				throw new Exception(string.Format("Exception in {0}: {1}", ex.Source, ex.Message), ex);
            }
            finally
            {
                if (file != null)
                {
                    try
                    {
                        file.Close();
                    }
                    catch (EncoderFallbackException ex)
                    {
                        throw new Exception("Serialize<T>, Error on File.Close() occurred", ex);
                    }
                    finally
                    {
                        file = null;
                    }
                }
            }
        }

        /// <summary>
        /// Serialize the passed type into a string.
        /// </summary>
        /// <returns>The result.</returns>
        //public static string Serialize<T>(T instance) {

        //    XmlSerializer serializer = new XmlSerializer(instance.GetType());

        //    using (StringWriter stringWriter = new Utf8StringWriter()) {
        //        using (XmlWriter xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings())) {
        //            try {
        //                serializer.Serialize(stringWriter, instance);
        //            } catch (InvalidOperationException ex) {
        //                throw ex;
        //            }
        //        };
        //        return stringWriter.GetStringBuilder().ToString();
        //    }
        //}

        /// <summary>
        /// Deserialize an instance of the specified type from the passed 
        /// xmlString.
        /// </summary>
        /// <param name="xmlValue">The string containing the object information.
        /// </param>
        /// <returns>An instance of type T based on the result.</returns>
        public static T DeserializeFromString<T>(string xmlValue)
        {

            if (!string.IsNullOrEmpty(xmlValue))
            {
				System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(T));
                using (StringReader testReader = new StringReader(xmlValue))
                {
                    try
                    {
                        return ((T)serializer.Deserialize(testReader));
                    }
                    catch (InvalidOperationException ex)
                    {
                        // There is a problem with the xml.
                        throw ex;
                    }
                }
            }
            return default(T);
        }

        public static T Deserialize<T>(string path)
        {

            T objectToDeserialize = default(T);
            StreamReader reader = null;

			System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(T));
            try
            {
                reader = new StreamReader(path);
                objectToDeserialize = (T)serializer.Deserialize(reader);
            }
            catch (UnauthorizedAccessException ex)
            {
				throw new Exception(string.Format("Serialize<T>, path '{1}' unauthorized.\n{0}", ex.Message, path), ex);
            }
            catch (ArgumentNullException)
            {
                Console.WriteLine("Serialize<T>, attribute path is null");
            }
            catch (ArgumentException ex)
            {
                throw new Exception(string.Format("Serizalize<T>, invalid path '{1}'\n{0}", ex.Message, path), ex);
            }
            catch (FileNotFoundException ex)
            {
                throw new Exception(string.Format("Serialize<T>, File not found: '{1}'\n{0}", ex, path), ex);
            }
            catch (DirectoryNotFoundException ex)
            {
                throw new Exception(string.Format("Serialize<T>, Directory of '{1}' not found\n{0}", ex, path), ex);
            }
            catch (IOException ex)
            {
                throw new Exception(string.Format("Serialize<T>, Unspecified IO exception for path '{1}'\n{0}", ex, path), ex);
            }
            catch (InvalidOperationException ex)
            {
                throw new Exception(string.Format("Serialize<T>, Failed to deserialize path '{1}'\n{0}", ex, path), ex);
            }
            finally
            {
                reader.Close();
                reader = null;
            }

            return objectToDeserialize;
        }

        /// <summary>
        /// This overrides the Encoding type from UTF16 to UTF8.
        /// </summary>
        public class Utf8StringWriter : StringWriter
        {
            public override Encoding Encoding
            {
                get { return Encoding.UTF8; }
            }
        }
    }
}
