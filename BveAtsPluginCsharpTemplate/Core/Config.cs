using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using System.Xml.Linq;

namespace AtsPlugin.Config
{

    /// <summary>
    /// プロセッサ一個あたりの設定を保持する構造体
    /// </summary>
    public struct ProcessorSetting
    {
        public int PanelIndex; // 出力先のATSパネル番号
        public int Delay;      // 応答遅延時間 (ms)
        public int Lag;        // 更新周期 (ms)
        public int Jitter;     // ゆらぎ幅 (ms)
    }

    public static class Config
    {
        /// <summary>プラグインの実行ディレクトリパス</summary>
        public static string PluginDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        #region デフォルト設定値
        /// <summary>設定による力行ノッチインデックス</summary>
        public static int PowerPanel = 216;
        /// <summary>設定によるブレーキノッチインデックス</summary>
        public static int BrakePanel = 215;
        /// <summary>設定によるレバーサーインデックス</summary>
        public static int ReverserPanel = -1;
        public static int HbkBrake = 0;
        public static int DefaultDelay = 750;
        public static int DefaultLag = 100;
        public static int DefaultJitter = 0;

        /// <summary>任意パネルラグ項目のインデックス (負数は無効)</summary>
        public static ProcessorSetting[] CustomSettings = new ProcessorSetting[6];
        #endregion


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
            // ファイルが存在しない場合はデフォルト値で動作を継続
            if (!File.Exists(path)) return;

            try
            {
                XElement root = XDocument.Load(path).Element("NotchDelay");
                if (root == null) return;

                XElement core = root.Element("Core");
                XElement panel = root.Element("Panel");

                // --- 1. Coreセクションの読み込み (挙動・数値の設定) ---
                if (core != null)
                {
                    // 全体共通のデフォルト挙動
                    DefaultDelay = GetInt(core, "DefaultDelay", 500);
                    DefaultLag = GetInt(core, "DefaultLag", 100);
                    DefaultJitter = GetInt(core, "DefaultJitter", 0);

                    // 計算に使用する数値 (抑速ブレーキ等のオフセット)
                    HbkBrake = GetInt(core, "HbkBrake", 0);
                }

                // --- 2. Panelセクションの読み込み (出力先の割り当て) ---
                if (panel != null)
                {
                    PowerPanel = GetInt(panel, "Power", 216);
                    BrakePanel = GetInt(panel, "Brake", 215);
                    ReverserPanel = GetInt(panel, "Reverser", -1);

                    // --- 3. Custom項目の統合読み込み (1～6) ---
                    for (int i = 0; i < 6; i++)
                    {
                        string name = "Custom" + (i + 1);

                        // Panelセクションから「出力先番号」を取得 (未設定なら-1)
                        CustomSettings[i].PanelIndex = GetInt(panel, name, -1);

                        // Coreセクションから「個別挙動」を属性として取得
                        XElement cElement = core?.Element(name);

                        // Core側に個別設定があれば読み込み、なければDefaultを採用
                        CustomSettings[i].Delay = GetAttrInt(cElement, "Delay", DefaultDelay);
                        CustomSettings[i].Lag = GetAttrInt(cElement, "Lag", DefaultLag);
                        CustomSettings[i].Jitter = GetAttrInt(cElement, "Jitter", DefaultJitter);
                    }
                }
            }
            catch
            {   // XMLの記述ミス（文字混入等）があってもプラグインを落とさず、
                // それまでに読み込めた値、または初期値で安全に動作させる
            }
        }

        #region XMLパース用ヘルパー

        /// <summary>
        /// 指定した要素の値を数値として取得します
        /// </summary>
        private static int GetInt(XElement p, string name, int def) =>
            (p?.Element(name) != null && int.TryParse(p.Element(name).Value, out int r)) ? r : def;

        /// <summary>
        /// 指定した要素の「属性」を数値として取得します
        /// </summary>
        private static int GetAttrInt(XElement e, string name, int def) =>
            (e?.Attribute(name) != null && int.TryParse(e.Attribute(name).Value, out int r)) ? r : def;

        #endregion

    }
}
