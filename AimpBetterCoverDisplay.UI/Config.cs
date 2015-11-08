using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using LordJZ.WinAPI;

namespace AimpBetterCoverDisplay.UI
{
    public class Config
    {
        static readonly XmlSerializer s_ser = new XmlSerializer(typeof(Config));
        static readonly string s_filename =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                         "AimpBetterCoverDisplay.UI.Config.xml");

        static Config s_instance;

        public static Config Instance
        {
            get
            {
                if (s_instance == null)
                    Load();

                return s_instance;
            }
        }

        static void Load()
        {
            Config c;

            try
            {
                using (StreamReader tr = new StreamReader(s_filename))
                    c = (Config)s_ser.Deserialize(tr);
            }
            catch
            {
                c = new Config();
            }

            Interlocked.CompareExchange(ref s_instance, c, null);
        }

        public WindowPlacement Placement { get; set; }

        public void Save()
        {
            try
            {
                using (StreamWriter wr = new StreamWriter(s_filename))
                    s_ser.Serialize(wr, this);
            }
            catch
            {
                // ignore
            }
        }
    }
}
