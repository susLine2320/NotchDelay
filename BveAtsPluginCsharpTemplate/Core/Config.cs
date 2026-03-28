using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using System.Xml.Linq;

namespace AtsPlugin.Config
{
    public static class Config
    {
        public static string PluginDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public static int Power = 216;
        public static int Brake = 215;
        public static int Reverser = -1;
        public static int Hbk = 0;
        public static int Delay = 750;
        public static int Lag = 100;

        public static void Init()
        {
            Power = 216;
            Brake = 215;
            Delay = 750;
            Lag = 100;
        }
        /*
        private static void Cfg(this Dictionary<string, string> configDict, string key, ref double param)
        {
            if (configDict.ContainsKey(key))
            {
                var value = configDict[key].ToLowerInvariant();
                if (value == "inf")
                {
                    param = LessInf;
                }
                else if (value == "-inf")
                {
                    param = -LessInf;
                }
                else
                {
                    double result;
                    if (!double.TryParse(configDict[key], out result)) return;
                    param = result;
                }
            }
        }

        private static void Cfg(this Dictionary<string, string> configDict, string key, ref bool param)
        {
            if (configDict.ContainsKey(key))
            {
                var str = configDict[key].ToLowerInvariant();
                param = (str == "true" || str == "1");
            }
        }

        private static void Cfg(this Dictionary<string, string> configDict, string key, ref string param)
        {
            if (configDict.ContainsKey(key))
            {
                param = configDict[key];
            }
        }

        private static void Cfg(this Dictionary<string, string> configDict, string key, ref int[] param)
        {
            if (configDict.ContainsKey(key))
            {
                var outputList = new List<int>();
                foreach (var value in configDict[key].Split(','))
                {
                    int result;
                    if (!int.TryParse(value.Trim(), out result)) return;
                    outputList.Add(result);
                }
                param = outputList.ToArray();
            }
        }
        */
        public static void Load(string path)
        {
            if (!File.Exists(path)) return;


            var table = XDocument.Load(path).Element("NotchDelay");
            if(table == null) return;
            // Coreセクション
            var row = table.Element("Core");
            if (row != null)
            {
                var row11 = row.Element("Delay");
                if(row11 != null)
                    Delay = int.Parse(row11.Value);
                
                var row12 = row.Element("Lag");
                if (row12 != null)
                    Lag = int.Parse(row12.Value);

                var row13 = row.Element("HbkNotch");
                if (row13 != null)
                    Hbk = int.Parse(row13.Value);
            }

            // Panelセクション
            var row2 = table.Element("Panel");
            if (row2 != null)
            {
                var row11 = row2.Element("Power");
                if (row11 != null)
                    Power = int.Parse(row11.Value);

                var row12 = row2.Element("Brake");
                if (row12 != null)
                    Brake = int.Parse(row12.Value);

                var row13 = row2.Element("Reverser");
                if (row13 != null)
                    Reverser = int.Parse(row13.Value);
            }
            /*
            //Mainセクション
            var row3 = table.Element("Main");
            if (row3 != null)
            {
                var row11 = row3.Element("MasconKey");
                if (row11 != null)
                    Key = Int32.Parse(row11.Value);
            }

            /*
            var dict = new Dictionary<string, string>();
            StreamReader configFile = File.OpenText(path);
            string line;
            while ((line = configFile.ReadLine()) != null)
            {
                line = line.Trim();
                if (line.Length > 0 && line[0] != '#')
                {
                    string[] commentTokens = line.Split('#');
                    string[] tokens = commentTokens[0].Trim().Split('=');
                    dict.Add(tokens[0].Trim().ToLowerInvariant(), tokens[1].Trim());
                }
            }
            configFile.Close();

            dict.Cfg("autopilot", ref Load_bve_autopilot);
            dict.Cfg("cscplugin", ref Load_csc_plugin);
            dict.Cfg("other", ref Load_Other_plugin);
            dict.Cfg("maxemergencydeceleration", ref EBDec);
            dict.Cfg("maxservicedeceleration", ref MaxDec);*/
        }
    }
}
