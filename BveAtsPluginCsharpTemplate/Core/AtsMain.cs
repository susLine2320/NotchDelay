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


        // 同名大丈夫？？
        public static int userPower = 0;
        public static int userBrake = 0;
        public static int userReverser = 0;
        public static float g_speed = 0;
        public static int g_time = 0;
        public static int DeltaT = 0;
        private static int Delay;
        private static int Lag;
        private static int Reflesh;
        private static int PowerIndex;
        private static int BrakeIndex;
        private static int ReverserIndex;
        private static int[] outPower = new int[10000];
        private static int[] outBrake = new int[10000];
        private static int[] outReverser = new int[10000];
        private static int[] PowerTime = new int[10000];
        private static int[] BrakeTime = new int[10000];
        private static int[] ReverserTime = new int[10000];
        private static int PowerNum;
        private static int BrakeNum;
        private static int ReverserNum;
        private static int outputPower;
        private static int outputBrake;
        private static int outputReverser2;
        private static int outputPower2;
        private static int outputBrake2;
        private static int outputReverser;
        //public static HashSet<AtsKey> userKey = new HashSet<AtsKey>();

        /// <summary>
        /// Called when this plug-in is loaded
        /// </summary>
        [DllExport(CallingConvention.StdCall)]
        public static void Load()
        {
            AtsPlugin.Config.Config.Init();
            AtsPlugin.Config.Config.Load(Path.Combine(AtsPlugin.Config.Config.PluginDir, "NotchDelay.xml"));
            Delay = Math.Abs(AtsPlugin.Config.Config.Delay);
            Lag = Math.Abs((AtsPlugin.Config.Config.Lag));
            BrakeIndex = AtsPlugin.Config.Config.Brake;
            PowerIndex = AtsPlugin.Config.Config.Power;
            ReverserIndex = AtsPlugin.Config.Config.Reverser;
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
            PowerNum = 0;
            BrakeNum = 0;
            ReverserNum = 0;
            Reflesh = -1;
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

            //PowerNum+1のデータから始めてPowerNumのデータを最後に行う
            for (int i = 0; i < 9999; ++i)
            {
                if (PowerTime[NumConvert(i, 0)] != 0 && PowerTime[NumConvert(i, 0)] < g_time)
                    outputPower = outPower[NumConvert(i, 0)];
            }
            for (int i = 0; i < 9999; ++i)
            {
                if (BrakeTime[NumConvert(i, 1)] != 0 && BrakeTime[NumConvert(i, 1)] < g_time)
                    outputBrake = outBrake[NumConvert(i, 1)];
            }
            for (int i = 0; i < 9999; ++i)
            {
                if (ReverserTime[NumConvert(i, 10000)] != 0 && ReverserTime[NumConvert(i, 10000)] < g_time)
                    outputReverser = outReverser[NumConvert(i, 1)];
            }

            if (g_time > Reflesh || Reflesh == -1)
            {
                outputBrake2 = outputBrake;
                outputPower2 = outputPower;
                outputReverser2 = outputReverser;
                Reflesh = g_time + Lag;
            }

            if(true)
            {
                if (BrakeIndex >= 0) { panelArray[BrakeIndex] = outputBrake2; }
                if (PowerIndex >= 0) { panelArray[PowerIndex] = outputPower2 + AtsPlugin.Config.Config.Hbk; }
                if (ReverserIndex >= 0) { panelArray[ReverserIndex] = outputReverser2 + 1; }
            }

            return new AtsHandles() { Power = userPower, Brake = userBrake, ConstantSpeed = AtsCscInstruction.Continue, Reverser = userReverser };
        }

        //num=0,PowerNum=6の時は7を出力、num=9999,PowerNum=6の時は6を出力
        private static int NumConvert(int num, int option)
        {
            if (option <= 0)
                return (num + PowerNum + 2) % 10000;
            else if (option >= 10000)
                return (num + ReverserNum + 2) % 10000;
            else
                return (num + BrakeNum + 2) % 10000;
        }

        /// <summary>
        /// Called when the power is changed
        /// </summary>
        /// <param name="handlePosition">Position of traction control handle.</param>
        [DllExport(CallingConvention.StdCall)]
        public static void SetPower(int handlePosition)
        {
            userPower = handlePosition;
            PowerNum += 1;
            if (PowerNum == 10000) { PowerNum = 0; }
            outPower[PowerNum] = handlePosition;
            PowerTime[PowerNum] = g_time + Delay;
        }

        /// <summary>
        /// Called when the brake is changed
        /// </summary>
        /// <param name="handlePosition">Position of brake control handle.</param>
        [DllExport(CallingConvention.StdCall)]
        public static void SetBrake(int handlePosition)
        {
            userBrake = handlePosition;
            BrakeNum += 1;
            if(BrakeNum == 10000) { BrakeNum = 0; }
            outBrake[BrakeNum] = handlePosition;
            BrakeTime[BrakeNum] = g_time + Delay;
        }

        /// <summary>
        /// Called when the reverser is changed
        /// </summary>
        /// <param name="handlePosition">Position of reveerser handle.</param>
        [DllExport(CallingConvention.StdCall)]
        public static void SetReverser(int handlePosition)
        {
            userReverser = handlePosition;
            ReverserNum += 1;
            if (ReverserNum == 10000) { ReverserNum = 0; }
            outReverser[ReverserNum] = handlePosition;
            ReverserTime[ReverserNum] = g_time + Delay;
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
