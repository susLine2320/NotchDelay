#define TM16000
//#define TM13000

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace AtsPlugin.Core
{
    /// <summary>
    /// Basics of ATS plug-in.
    /// </summary>
    public static class AtsMain
    {
        /// <summary>
        /// ATS Plug-in Version
        /// </summary>
        const int Version = 0x00020000;

        /// <summary>
        /// ATS Keys
        /// </summary>
        public enum AtsKey
        {
            S = 0,          // S Key
            A1,             // A1 Key
            A2,             // A2 Key
            B1,             // B1 Key
            B2,             // B2 Key
            C1,             // C1 Key
            C2,             // C2 Key
            D,              // D Key
            E,              // E Key
            F,              // F Key
            G,              // G Key
            H,              // H Key
            I,              // I Key
            J,              // J Key
            K,              // K Key
            L               // L Key
        }

        /// <summary>
        /// Initial Position of Handle
        /// </summary>
        public enum AtsInitialHandlePosition
        {
            ServiceBrake = 0,   // Service Brake
            EmergencyBrake,     // Emergency Brake
            Removed             // Handle Removed
        }

        /// <summary>
        /// Sound Control Instruction
        /// </summary>
        public static class AtsSoundControlInstruction
        {
            public const int Stop = -10000;     // Stop
            public const int Play = 1;          // Play Once
            public const int PlayLooping = 0;   // Play Repeatedly
            public const int Continue = 2;      // Continue
        }

        /// <summary>
        /// Type of Horn
        /// </summary>
        public enum AtsHornType
        {
            Primary = 0,    // Horn 1
            Secondary,      // Horn 2
            Music           // Music Horn
        }

        /// <summary>
        /// Constant Speed Control Instruction
        /// </summary>
        public static class AtsCscInstruction
        {
            public const int Continue = 0;       // Continue
            public const int Enable = 1;         // Enable
            public const int Disable = 2;        // Disable
        }

        /// <summary>
        /// Vehicle Specification
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct AtsVehicleSpec
        {
            public int BrakeNotches;   // Number of Brake Notches
            public int PowerNotches;   // Number of Power Notches
            public int AtsNotch;       // ATS Cancel Notch
            public int B67Notch;       // 80% Brake (67 degree)
            public int Cars;           // Number of Cars
        };

        /// <summary>
        /// State Quantity of Vehicle
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct AtsVehicleState
        {
            public double Location;    // Train Position (Z-axis) (m)
            public float Speed;        // Train Speed (km/h)
            public int Time;           // Time (ms)
            public float BcPressure;   // Pressure of Brake Cylinder (Pa)
            public float MrPressure;   // Pressure of MR (Pa)
            public float ErPressure;   // Pressure of ER (Pa)
            public float BpPressure;   // Pressure of BP (Pa)
            public float SapPressure;  // Pressure of SAP (Pa)
            public float Current;      // Current (A)
        };

        /// <summary>
        /// Received Data from Beacon
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct AtsBeaconData
        {
            public int Type;       // Type of Beacon
            public int Signal;     // Signal of Connected Section
            public float Distance; // Distance to Connected Section (m)
            public int Optional;   // Optional Data
        };

        /// <summary>
        /// Train Operation Instruction
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct AtsHandles
        {
            public int Brake;               // Brake Notch
            public int Power;               // Power Notch
            public int Reverser;            // Reverser Position
            public int ConstantSpeed;       // Constant Speed Control
        };

        /// <summary>
        /// Unmanaged array operations for Panel / Sound.
        /// </summary>
        public class AtsIoArray
        {
            /// <summary>
            /// Address of unmanaged array.
            /// </summary>
            private IntPtr Address { get; set; } = IntPtr.Zero;

            /// <summary>
            /// Array length of unmanaged array.
            /// </summary>
            public int Length { get; private set; } = -1;

            /// <summary>
            /// Gets an element from unmanaged array by index.
            /// </summary>
            /// <param name="index">The array index that indicates position of element in unmanaged array.</param>
            /// <returns>Element of unmanaged array.</returns>
            public unsafe int this[int index]
            {
                get
                {
                    if ((index >= Length) || (index < 0))
                    {
                        throw new IndexOutOfRangeException("Unmanaged array index is out of range: " + AppDomain.CurrentDomain.BaseDirectory);
                    }

                    var pointer = (int*)Address.ToPointer();
                    return pointer[index];      // Get an element.
                }
                set
                {
                    if ((index >= Length) || (index < 0))
                    {
                        throw new IndexOutOfRangeException("Unmanaged array index is out of range: " + AppDomain.CurrentDomain.BaseDirectory);
                    }

                    var pointer = (int*)Address.ToPointer();
                    pointer[index] = value;     // Set an element.
                }
            }

            /// <summary>
            /// Constructor.
            /// </summary>
            public AtsIoArray()
            {
            }

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="source">Pointer of unmanaged array.</param>
            /// <param name="length">Array length of unmanaged array.</param>
            public AtsIoArray(IntPtr source, int length = 256)
            {
                SetSource(source, length);
            }

            /// <summary>
            /// Sets an unmanaged array.
            /// </summary>
            /// <param name="source">Pointer of unmanaged array.</param>
            /// <param name="length">Array length of unmanaged array.</param>
            public void SetSource(IntPtr source, int length = 256)
            {
                Address = source;
                Length = length;
            }
        }


        #region BVE標準
        /// <summary>ユーザーの入力した力行ノッチ段数</summary>
        public static int userPower = 0;
        /// <summary>ユーザーの入力したブレーキノッチ段数</summary>
        public static int userBrake = 0;
        /// <summary>ユーザーの入力したレバーサー方向</summary>
        public static int userReverser = 0;
        public static float g_speed = 0;
        public static int g_time = 0;
        public static int DeltaT = 0;
        #endregion

        #region 設定
        /// <summary>設定による力行ノッチインデックス</summary>
        private static int PowerIndex;
        /// <summary>設定によるブレーキノッチインデックス</summary>
        private static int BrakeIndex;
        /// <summary>設定によるレバーサーインデックス</summary>
        private static int ReverserIndex;
        #endregion

        #region 遅延処理インスタンス
        private static LagDelayProcessor _speedDisp = new LagDelayProcessor();
        private static LagDelayProcessor _panelSelector = new LagDelayProcessor();
        private static LagDelayProcessor _bcPressure = new LagDelayProcessor();

        /// <summary>ブレーキノッチ段数ラグ</summary>
        private static LagDelayProcessor _brake = new LagDelayProcessor();
        /// <summary>力行ノッチ段数ラグ</summary>
        private static LagDelayProcessor _power = new LagDelayProcessor();
        /// <summary>レバーサーラグ</summary>
        private static LagDelayProcessor _reverser = new LagDelayProcessor();
        /// <summary>Custom1-6用</summary>
        private static LagDelayProcessor[] _pCustoms = new LagDelayProcessor[6];
        #endregion

        /// <summary>
        /// Called when this plug-in is loaded
        /// </summary>
        [DllExport(CallingConvention.StdCall)]
        public static void Load()
        {
            // XML設定の読み込み
            //AtsPlugin.Config.Config.Init();
            AtsPlugin.Config.Config.Load(Path.Combine(AtsPlugin.Config.Config.PluginDir, "NotchDelay.xml"));
            BrakeIndex = AtsPlugin.Config.Config.BrakePanel;
            PowerIndex = AtsPlugin.Config.Config.PowerPanel;
            ReverserIndex = AtsPlugin.Config.Config.ReverserPanel;

            // Custom用プロセッサの配列を初期化
            for (int i = 0; i < _pCustoms.Length; i++)
            {
                _pCustoms[i] = new LagDelayProcessor();
            }
        }

        /// <summary>
        /// Called when this plug-in is unloaded
        /// </summary>
        [DllExport(CallingConvention.StdCall)]
        public static void Dispose()
        {

        }

        /// <summary>
        /// Returns the version numbers of ATS plug-in
        /// </summary>
        /// <returns>Version numbers of ATS plug-in.</returns>
        [DllExport(CallingConvention.StdCall)]
        public static int GetPluginVersion()
        {
            return Version;
        }

        /// <summary>
        /// Called when the train is loaded
        /// </summary>
        /// <param name="vehicleSpec">Spesifications of vehicle.</param>
        [DllExport(CallingConvention.StdCall)]
        public static void SetVehicleSpec(AtsVehicleSpec vehicleSpec)
        {

        }

        /// <summary>
        /// Called when the game is started
        /// </summary>
        /// <param name="initialHandlePosition">Initial position of control handle.</param>
        [DllExport(CallingConvention.StdCall)]
        public static void Initialize(int initialHandlePosition)
        {
            g_speed = 0;

            _brake.Reset();
            _power.Reset();
            _reverser.Reset();
            foreach (var p in _pCustoms)
            {
                p.Reset();
            }
        }

        /// <summary>
        /// Called every frame
        /// </summary>
        /// <param name="vehicleState">Current state of vehicle.</param>
        /// <param name="panel">Current state of panel.</param>
        /// <param name="sound">Current state of sound.</param>
        /// <returns>Driving operations of vehicle.</returns>
        [DllExport(CallingConvention.StdCall)]
        public static AtsHandles Elapse(AtsVehicleState vehicleState, IntPtr panel, IntPtr sound)
        {
            var panelArray = new AtsIoArray(panel);
            var soundArray = new AtsIoArray(sound);
            DeltaT = vehicleState.Time - g_time;
            g_time = vehicleState.Time;
            g_speed = vehicleState.Speed;

            int speed = (int)vehicleState.Speed;

            // --- 1. 標準ノッチのラグ処理 --
            if (BrakeIndex >= 0) { panelArray[BrakeIndex] = _brake.Process(userBrake, g_time, Config.Config.DefaultDelay, Config.Config.DefaultLag, Config.Config.DefaultJitter); }
            if (PowerIndex >= 0) { panelArray[PowerIndex] = _power.Process(userPower, g_time, Config.Config.DefaultDelay, Config.Config.DefaultLag, Config.Config.DefaultJitter) + AtsPlugin.Config.Config.HbkBrake; }
            if (ReverserIndex >= 0) { panelArray[ReverserIndex] = _reverser.Process(userReverser, g_time, Config.Config.DefaultDelay, Config.Config.DefaultLag, Config.Config.DefaultJitter); }

            // --- 2. Custom項目のラグ処理 (1～6) ---
            for (int i = 0; i < 6; i++)
            {
                var setting = Config.Config.CustomSettings[i];

                // パネル番号が有効(0～)な場合のみ実行
                if (setting.PanelIndex >= 0 && setting.PanelIndex < 256)
                {
                    // 現在のパネル値を「入力」として読み取り、ラグを付けて「同じ場所」に書き戻す
                    // これにより、先行する他プラグインの出力値に対して後付けでラグを付与できる
                    int currentValue = panelArray[setting.PanelIndex];
                    panelArray[setting.PanelIndex] = _pCustoms[i].Process(currentValue, g_time, setting.Delay, setting.Lag, setting.Jitter);
                }
            }

            return new AtsHandles() { Power = userPower, Brake = userBrake, ConstantSpeed = AtsCscInstruction.Continue, Reverser = userReverser };
        }

        public class LagDelayProcessor
        {
            private int[] _valueHistory = new int[1000];
            private int[] _timeHistory = new int[1000];
            private int _writePtr = 0;
            private int _lastInputValue = -999;
            private int _bufferedValue;
            private int _outputValue;
            private int _nextRefreshTime = -1;

            // 初期化・リセット用の関数
            public void Reset()
            {
                System.Array.Clear(_valueHistory, 0, _valueHistory.Length);
                System.Array.Clear(_timeHistory, 0, _timeHistory.Length);
                _writePtr = 0;
                _lastInputValue = -999;
                _bufferedValue = 0;
                _outputValue = 0;
                _nextRefreshTime = -1;
            }

            // inputID: 現在点灯しているパネル番号などの「状態ID」
            public int Process(int inputID, int currentTime, int delay, int lag, int jitter)
            {
                // 状態（ID）が変わった瞬間だけ記録
                if (inputID != _lastInputValue)
                {
                    _writePtr = (_writePtr + 1) % 1000;
                    _valueHistory[_writePtr] = inputID;
                    _timeHistory[_writePtr] = currentTime + delay;
                    _lastInputValue = inputID;
                }

                // Delay: 予約時刻を過ぎた最新の状態を探す
                for (int i = 0; i < 1000; i++)
                {
                    if (_timeHistory[i] != 0 && _timeHistory[i] <= currentTime)
                    {
                        _bufferedValue = _valueHistory[i];
                    }
                }

                // Lag: 指定周期ごとに表示を確定させる
                if (currentTime >= _nextRefreshTime || _nextRefreshTime == -1)
                {
                    _outputValue = _bufferedValue;

                    // 次回の更新時刻を「基本Lag + 時刻依存のゆらぎ」で決定する
                    // jitterが50なら、0〜50msの範囲でランダムに遅れる
                    int currentJitter = (jitter > 0) ? (currentTime % jitter) : 0;
                    _nextRefreshTime = currentTime + lag + currentJitter;
                }
                return _outputValue;
            }
        }

        /// <summary>
        /// Called when the power is changed
        /// </summary>
        /// <param name="handlePosition">Position of traction control handle.</param>
        [DllExport(CallingConvention.StdCall)]
        public static void SetPower(int handlePosition)
        {
            userPower = handlePosition;
        }

        /// <summary>
        /// Called when the brake is changed
        /// </summary>
        /// <param name="handlePosition">Position of brake control handle.</param>
        [DllExport(CallingConvention.StdCall)]
        public static void SetBrake(int handlePosition)
        {
            userBrake = handlePosition;
        }

        /// <summary>
        /// Called when the reverser is changed
        /// </summary>
        /// <param name="handlePosition">Position of reveerser handle.</param>
        [DllExport(CallingConvention.StdCall)]
        public static void SetReverser(int handlePosition)
        {
            userReverser = handlePosition;
        }

        /// <summary>
        /// Called when any ATS key is pressed
        /// </summary>
        /// <param name="keyIndex">Index of key.</param>
        [DllExport(CallingConvention.StdCall)]
        public static void KeyDown(int keyIndex)
        {

        }

        /// <summary>
        /// Called when any ATS key is released
        /// </summary>
        /// <param name="keyIndex">Index of key.</param>
        [DllExport(CallingConvention.StdCall)]
        public static void KeyUp(int keyIndex)
        {

        }

        /// <summary>
        /// Called when the horn is used
        /// </summary>
        /// <param name="hornIndex">Type of horn.</param>
        [DllExport(CallingConvention.StdCall)]
        public static void HornBlow(int hornIndex)
        {

        }

        /// <summary>
        /// Called when the door is opened
        /// </summary>
        [DllExport(CallingConvention.StdCall)]
        public static void DoorOpen()
        {

        }

        /// <summary>
        /// Called when the door is closed
        /// </summary>
        [DllExport(CallingConvention.StdCall)]
        public static void DoorClose()
        {

        }

        /// <summary>
        /// Called when current signal is changed
        /// </summary>
        /// <param name="signalIndex">Index of signal.</param>
        [DllExport(CallingConvention.StdCall)]
        public static void SetSignal(int signalIndex)
        {

        }

        /// <summary>
        /// Called when the beacon data is received
        /// </summary>
        /// <param name="beaconData">Received data of beacon.</param>
        [DllExport(CallingConvention.StdCall)]
        public static void SetBeaconData(AtsBeaconData beaconData)
        {

        }
    }
}
