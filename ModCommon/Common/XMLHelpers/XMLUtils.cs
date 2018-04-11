using System.Xml.Serialization;
using System.IO;

namespace ModCommon
{
    public class XMLUtils
    {
        /*
        [XmlRoot("AppSettings")]
        public class ExampleData
        {
            [XmlElement("Data")]
            public string data;
            [XmlElement("MoreData")]
            public string moreData;

            [XmlArray("ListOfData")]
            public List<string> someListOfData;

            [XmlElement(ElementName ="OptionalData", IsNullable = true)]
            public bool? someOptionalData;
        }
        */

        public static bool WriteDataToFile<T>(string path, T settings) where T : class
        {
            bool result = false;
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            FileStream fstream = null;
            try
            {
                fstream = new FileStream(path, FileMode.Create);
                serializer.Serialize(fstream, settings);
                result = true;
            }
            catch(System.Exception e)
            {
                Dev.Log( "Error creating/saving file " + e.Message );
                //System.Windows.Forms.MessageBox.Show("Error creating/saving file "+ e.Message);
            }
            finally
            {
                fstream.Close();
            }
            return result;
        }

        public static bool ReadDataFromFile<T>(string path, out T settings) where T : class
        {
            settings = null;

            if(!File.Exists(path))
            {
                //System.Windows.Forms.MessageBox.Show("No file found at " + path );
                return false;
            }

            bool returnResult = true;

            XmlSerializer serializer = new XmlSerializer(typeof(T));
            FileStream fstream = null;
            try
            {
                fstream = new FileStream(path, FileMode.Open);
                settings = serializer.Deserialize(fstream) as T;
            }
            catch(System.Exception e)
            {
                Dev.Log( "Error loading file " + e.Message );
                //System.Windows.Forms.MessageBox.Show("Error loading file " + e.Message);
                returnResult = false;
            }
            finally
            {
                fstream.Close();
            }

            return returnResult;
        }
    }
}