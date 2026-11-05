using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Pavo
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PavoScanPoint
    {
        public ushort angle;
        public ushort distance;
        public ushort intensity;
    }

    public enum PavoMode
    {
        Unknown = -1,
        Normal = 0,
        Echo = 1,
        Anti = 2
    }

    public sealed class PavoLidar : IDisposable
    {
        private delegate int StringGetter(IntPtr handle, StringBuilder buffer, uint bufferCapacity, out uint requiredCapacity);

        private const string NativeLibrary = "pavo-sharp-wrapper";
        private const int StatusOk = 0;
        private const int StatusTimeout = 1;
        private const int StatusBufferTooSmall = 2;
        private const int InitialScanCapacity = 4096;

        private IntPtr handle;

        public PavoScanPoint[] Points { get; private set; }

        public PavoLidar()
        {
            handle = NativeMethods.Create();
            if (handle == IntPtr.Zero)
                throw new InvalidOperationException(GetLastError());

            Points = new PavoScanPoint[InitialScanCapacity];
        }

        public void Open(string deviceIp, ushort devicePort)
        {
            ThrowIfError(NativeMethods.Open(handle, deviceIp, devicePort));
        }

        public bool IsConnected()
        {
            int connected;
            ThrowIfError(NativeMethods.IsConnected(handle, out connected));
            return connected != 0;
        }

        public bool TryGetScanFrame(int timeoutMilliseconds, out int pointCount, out uint timestamp)
        {
            uint count;
            int status = NativeMethods.GetScanFrame(
                handle,
                Points,
                (uint)Points.Length,
                out count,
                out timestamp,
                timeoutMilliseconds);

            pointCount = (int)count;
            if (status == StatusTimeout)
                return false;
            if (status == StatusBufferTooSmall)
            {
                PavoScanPoint[] resizedPoints = Points;
                Array.Resize(ref resizedPoints, pointCount);
                Points = resizedPoints;
                status = NativeMethods.GetScanFrame(
                    handle,
                    Points,
                    (uint)Points.Length,
                    out count,
                    out timestamp,
                    timeoutMilliseconds);
                pointCount = (int)count;
            }

            ThrowIfError(status);
            return true;
        }

        public void EnableData(bool enabled)
        {
            ThrowIfError(NativeMethods.EnableData(handle, enabled ? 1 : 0));
        }

        public void EnableMotor(bool enabled)
        {
            ThrowIfError(NativeMethods.EnableMotor(handle, enabled ? 1 : 0));
        }

        public int GetDeviceType()
        {
            int value;
            ThrowIfError(NativeMethods.GetDeviceType(handle, out value));
            return value;
        }

        public uint GetDeviceSerialNumber()
        {
            uint value;
            ThrowIfError(NativeMethods.GetDeviceSerialNumber(handle, out value));
            return value;
        }

        public uint GetDeviceProductNumber()
        {
            uint value;
            ThrowIfError(NativeMethods.GetDeviceProductNumber(handle, out value));
            return value;
        }

        public int GetMotorSpeed()
        {
            int value;
            ThrowIfError(NativeMethods.GetMotorSpeed(handle, out value));
            return value;
        }

        public void SetMotorSpeed(int speed)
        {
            ThrowIfError(NativeMethods.SetMotorSpeed(handle, speed));
        }

        public int GetMergeCoefficient()
        {
            int value;
            ThrowIfError(NativeMethods.GetMergeCoefficient(handle, out value));
            return value;
        }

        public void SetMergeCoefficient(int coefficient)
        {
            ThrowIfError(NativeMethods.SetMergeCoefficient(handle, coefficient));
        }

        public int GetDegreeShift()
        {
            int value;
            ThrowIfError(NativeMethods.GetDegreeShift(handle, out value));
            return value;
        }

        public void SetDegreeShift(int shift)
        {
            ThrowIfError(NativeMethods.SetDegreeShift(handle, shift));
        }

        public void GetDegreeScope(out int minimum, out int maximum)
        {
            ThrowIfError(NativeMethods.GetDegreeScope(handle, out minimum, out maximum));
        }

        public void SetDegreeScope(int minimum, int maximum)
        {
            ThrowIfError(NativeMethods.SetDegreeScope(handle, minimum, maximum));
        }

        public PavoMode GetMode()
        {
            int value;
            ThrowIfError(NativeMethods.GetMode(handle, out value));
            return (PavoMode)value;
        }

        public void SetMode(PavoMode mode)
        {
            ThrowIfError(NativeMethods.SetMode(handle, (int)mode));
        }

        public void EnableTailFilter(int method)
        {
            ThrowIfError(NativeMethods.EnableTailFilter(handle, method));
        }

        public void Reset()
        {
            ThrowIfError(NativeMethods.Reset(handle));
        }

        public string GetFirmwareVersion()
        {
            var buffer = new StringBuilder(32);
            uint requiredCapacity;
            int status = NativeMethods.GetFirmwareVersion(
                handle, buffer, (uint)buffer.Capacity, out requiredCapacity);
            if (status == StatusBufferTooSmall)
            {
                buffer = new StringBuilder((int)requiredCapacity);
                status = NativeMethods.GetFirmwareVersion(
                    handle, buffer, (uint)buffer.Capacity, out requiredCapacity);
            }
            ThrowIfError(status);
            return buffer.ToString();
        }

        public int GetDeviceErrorCode()
        {
            int value;
            ThrowIfError(NativeMethods.GetDeviceErrorCode(handle, out value));
            return value;
        }

        public string GetDestinationIp()
        {
            return GetString(NativeMethods.GetDestinationIp);
        }

        public void SetDestinationIp(string ip)
        {
            ThrowIfError(NativeMethods.SetDestinationIp(handle, ip));
        }

        public ushort GetDestinationPort()
        {
            ushort value;
            ThrowIfError(NativeMethods.GetDestinationPort(handle, out value));
            return value;
        }

        public void SetDestinationPort(ushort port)
        {
            ThrowIfError(NativeMethods.SetDestinationPort(handle, port));
        }

        public string GetLidarIp()
        {
            return GetString(NativeMethods.GetLidarIp);
        }

        public void SetLidarIp(string ip)
        {
            ThrowIfError(NativeMethods.SetLidarIp(handle, ip));
        }

        public ushort GetLidarPort()
        {
            ushort value;
            ThrowIfError(NativeMethods.GetLidarPort(handle, out value));
            return value;
        }

        public void SetLidarPort(ushort port)
        {
            ThrowIfError(NativeMethods.SetLidarPort(handle, port));
        }

        public void SetNetworkConfig(string destinationIp, ushort destinationPort, string lidarIp, ushort lidarPort)
        {
            ThrowIfError(NativeMethods.SetNetworkConfig(
                handle, destinationIp, destinationPort, lidarIp, lidarPort));
        }

        public void ApplyNetworkConfig()
        {
            ThrowIfError(NativeMethods.ApplyNetworkConfig(handle));
        }

        public void Dispose()
        {
            if (handle == IntPtr.Zero)
                return;

            int closeStatus = NativeMethods.Close(handle);
            string closeError = closeStatus == StatusOk ? null : GetLastError();
            NativeMethods.Destroy(handle);
            handle = IntPtr.Zero;

            if (closeStatus != StatusOk)
                throw new InvalidOperationException(closeError);
        }

        private static void ThrowIfError(int status)
        {
            if (status != StatusOk)
                throw new InvalidOperationException(GetLastError());
        }

        private static string GetLastError()
        {
            var message = new StringBuilder(512);
            NativeMethods.GetLastError(message, (uint)message.Capacity);
            return message.Length == 0 ? "Pavo native API call failed." : message.ToString();
        }

        private string GetString(StringGetter getter)
        {
            var buffer = new StringBuilder(64);
            uint requiredCapacity;
            int status = getter(handle, buffer, (uint)buffer.Capacity, out requiredCapacity);
            if (status == StatusBufferTooSmall)
            {
                buffer = new StringBuilder((int)requiredCapacity);
                status = getter(handle, buffer, (uint)buffer.Capacity, out requiredCapacity);
            }
            ThrowIfError(status);
            return buffer.ToString();
        }

        public void SetMergeCoefficient(object mergeCoefficient)
        {
            throw new NotImplementedException();
        }

        private static class NativeMethods
        {
            [DllImport(NativeLibrary, EntryPoint = "Pavo_Create", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern IntPtr Create();

            [DllImport(NativeLibrary, EntryPoint = "Pavo_Destroy", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern void Destroy(IntPtr handle);

            [DllImport(NativeLibrary, EntryPoint = "Pavo_Open", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern int Open(IntPtr handle, [MarshalAs(UnmanagedType.LPStr)] string deviceIp, ushort devicePort);

            [DllImport(NativeLibrary, EntryPoint = "Pavo_Close", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern int Close(IntPtr handle);

            [DllImport(NativeLibrary, EntryPoint = "Pavo_GetLastError", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern uint GetLastError(StringBuilder buffer, uint bufferCapacity);

            [DllImport(NativeLibrary, EntryPoint = "Pavo_IsConnected", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern int IsConnected(IntPtr handle, out int connected);

            [DllImport(NativeLibrary, EntryPoint = "Pavo_GetScanFrame", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern int GetScanFrame(
                IntPtr handle,
                [Out] PavoScanPoint[] points,
                uint pointCapacity,
                out uint pointCount,
                out uint timestamp,
                int timeoutMilliseconds);

            [DllImport(NativeLibrary, EntryPoint = "Pavo_EnableData", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern int EnableData(IntPtr handle, int enabled);

            [DllImport(NativeLibrary, EntryPoint = "Pavo_EnableMotor", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern int EnableMotor(IntPtr handle, int enabled);

            [DllImport(NativeLibrary, EntryPoint = "Pavo_GetDeviceType", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern int GetDeviceType(IntPtr handle, out int deviceType);

            [DllImport(NativeLibrary, EntryPoint = "Pavo_GetDeviceSerialNumber", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern int GetDeviceSerialNumber(IntPtr handle, out uint serialNumber);

            [DllImport(NativeLibrary, EntryPoint = "Pavo_GetDeviceProductNumber", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern int GetDeviceProductNumber(IntPtr handle, out uint productNumber);

            [DllImport(NativeLibrary, EntryPoint = "Pavo_GetMotorSpeed", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern int GetMotorSpeed(IntPtr handle, out int speed);

            [DllImport(NativeLibrary, EntryPoint = "Pavo_SetMotorSpeed", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern int SetMotorSpeed(IntPtr handle, int speed);

            [DllImport(NativeLibrary, EntryPoint = "Pavo_GetMergeCoefficient", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern int GetMergeCoefficient(IntPtr handle, out int coefficient);

            [DllImport(NativeLibrary, EntryPoint = "Pavo_SetMergeCoefficient", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern int SetMergeCoefficient(IntPtr handle, int coefficient);

            [DllImport(NativeLibrary, EntryPoint = "Pavo_GetDegreeShift", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern int GetDegreeShift(IntPtr handle, out int shift);

            [DllImport(NativeLibrary, EntryPoint = "Pavo_SetDegreeShift", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern int SetDegreeShift(IntPtr handle, int shift);

            [DllImport(NativeLibrary, EntryPoint = "Pavo_GetDegreeScope", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern int GetDegreeScope(IntPtr handle, out int minimum, out int maximum);

            [DllImport(NativeLibrary, EntryPoint = "Pavo_SetDegreeScope", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern int SetDegreeScope(IntPtr handle, int minimum, int maximum);

            [DllImport(NativeLibrary, EntryPoint = "Pavo_GetMode", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern int GetMode(IntPtr handle, out int mode);

            [DllImport(NativeLibrary, EntryPoint = "Pavo_SetMode", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern int SetMode(IntPtr handle, int mode);

            [DllImport(NativeLibrary, EntryPoint = "Pavo_EnableTailFilter", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern int EnableTailFilter(IntPtr handle, int method);

            [DllImport(NativeLibrary, EntryPoint = "Pavo_Reset", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern int Reset(IntPtr handle);

            [DllImport(NativeLibrary, EntryPoint = "Pavo_GetFirmwareVersion", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern int GetFirmwareVersion(
                IntPtr handle, StringBuilder buffer, uint bufferCapacity, out uint requiredCapacity);

            [DllImport(NativeLibrary, EntryPoint = "Pavo_GetDeviceErrorCode", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern int GetDeviceErrorCode(IntPtr handle, out int errorCode);

            [DllImport(NativeLibrary, EntryPoint = "Pavo_GetDestinationIp", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern int GetDestinationIp(IntPtr handle, StringBuilder buffer, uint bufferCapacity, out uint requiredCapacity);

            [DllImport(NativeLibrary, EntryPoint = "Pavo_SetDestinationIp", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern int SetDestinationIp(IntPtr handle, [MarshalAs(UnmanagedType.LPStr)] string ip);

            [DllImport(NativeLibrary, EntryPoint = "Pavo_GetDestinationPort", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern int GetDestinationPort(IntPtr handle, out ushort port);

            [DllImport(NativeLibrary, EntryPoint = "Pavo_SetDestinationPort", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern int SetDestinationPort(IntPtr handle, ushort port);

            [DllImport(NativeLibrary, EntryPoint = "Pavo_GetLidarIp", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern int GetLidarIp(IntPtr handle, StringBuilder buffer, uint bufferCapacity, out uint requiredCapacity);

            [DllImport(NativeLibrary, EntryPoint = "Pavo_SetLidarIp", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern int SetLidarIp(IntPtr handle, [MarshalAs(UnmanagedType.LPStr)] string ip);

            [DllImport(NativeLibrary, EntryPoint = "Pavo_GetLidarPort", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern int GetLidarPort(IntPtr handle, out ushort port);

            [DllImport(NativeLibrary, EntryPoint = "Pavo_SetLidarPort", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern int SetLidarPort(IntPtr handle, ushort port);

            [DllImport(NativeLibrary, EntryPoint = "Pavo_SetNetworkConfig", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern int SetNetworkConfig(
                IntPtr handle,
                [MarshalAs(UnmanagedType.LPStr)] string destinationIp,
                ushort destinationPort,
                [MarshalAs(UnmanagedType.LPStr)] string lidarIp,
                ushort lidarPort);

            [DllImport(NativeLibrary, EntryPoint = "Pavo_ApplyNetworkConfig", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern int ApplyNetworkConfig(IntPtr handle);
        }
    }
}
