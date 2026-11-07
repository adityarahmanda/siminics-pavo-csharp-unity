using System;
using System.Collections.Concurrent;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;

namespace Rahmanda.PavoSDK
{
    public class PavoLidar : MonoBehaviour
    {
        [Header("Connection Settings")]
        [SerializeField] public string ipAddress = "10.10.10.101";
        [SerializeField] public int port = 2368;
        [SerializeField] public bool connectOnAwake = true;

        [Header("Device Settings")]
        [Tooltip("LiDAR operating mode. Unknown leaves the current device mode unchanged.")]
        [SerializeField] public PavoMode mode = PavoMode.Unknown;
        [Tooltip("Requested motor speed. The valid range is defined by the device SDK.")]
        [SerializeField] public int motorSpeed = 30;
        [Tooltip("Zero-angle offset in hundredths of a degree. Valid range: 0-35999.")]
        [SerializeField] public int degreeShift = 25;
        [Tooltip("Merge coefficient used for downsampling or filtering. Allowed values: 1, 2, 4, or 8.")]
        [SerializeField] public int mergeCoefficient = 1;
        [Tooltip("Minimum scan angle in hundredths of a degree. Valid range: 0-35999 and less than maxDegreeScope.")]
        [SerializeField] public int minDegreeScope = 3500;
        [Tooltip("Maximum scan angle in hundredths of a degree. Valid range: 0-35999 and greater than minDegreeScope.")]
        [SerializeField] public int maxDegreeScope = 35999;

        [Header("Device Info")]
        [SerializeField] private string m_firmwareVersion;
        [SerializeField] private uint m_productNumber;
        [SerializeField] private uint m_serialNumber;
        [SerializeField] private int m_deviceType;

        [Header("Events")]
        public UnityEvent<bool> onIsConnectedChanged = new UnityEvent<bool>();
        public UnityEvent<ScanFrame> onScanFrame = new UnityEvent<ScanFrame>();

        private const int ScanTimeoutMilliseconds = 100;
        private readonly ConcurrentQueue<ScanFrame> m_pendingScanFrames = new ConcurrentQueue<ScanFrame>();
        private readonly object m_scanFrameLock = new object();
        private ScanFrame m_latestScanFrame;
        private readonly ConcurrentQueue<bool> m_pendingConnectionStates = new ConcurrentQueue<bool>();
        private Thread m_worker;
        private volatile bool m_stopRequested;
        private bool m_isConnected;

        public string FirmwareVersion => m_firmwareVersion;
        public uint ProductNumber => m_productNumber;
        public uint SerialNumber => m_serialNumber;
        public int DeviceType => m_deviceType;
        public bool IsConnected => m_isConnected;

        private void Awake()
        {
            if (connectOnAwake)
                Connect(ipAddress, port);
        }

        private void Update()
        {
            while (m_pendingConnectionStates.TryDequeue(out bool connected))
                SetConnectionState(connected);

            while (m_pendingScanFrames.TryDequeue(out ScanFrame frame))
            {
                lock (m_scanFrameLock)
                    m_latestScanFrame = frame;
                onScanFrame.Invoke(m_latestScanFrame);
            }
        }

        private void OnDestroy()
        {
            Disconnect();
        }


        public void Connect(string newIpAddress = null, int newPort = -1, PavoMode newMode = PavoMode.Unknown, int newMotorSpeed = -1, int newDegreeShift = -1, int newMergeCoefficient = -1, int newMinDegreeScope = -1, int newMaxDegreeScope = -1)
        {
            string requestedIpAddress = !string.IsNullOrEmpty(newIpAddress) ? newIpAddress : ipAddress;
            int requestedPort = newPort >= 1 && newPort <= ushort.MaxValue ? newPort : port;
            int requestedMode = newMode != PavoMode.Unknown ? (int)newMode : (int)mode;
            int requestedMotorSpeed = newMotorSpeed >= 0 ? newMotorSpeed : motorSpeed;
            int requestedDegreeShift = newDegreeShift >= 0 ? newDegreeShift : degreeShift;
            int requestedMergeCoefficient = newMergeCoefficient >= 0 ? newMergeCoefficient : mergeCoefficient;
            int requestedMinDegreeScope = newMinDegreeScope >= 0 ? newMinDegreeScope : minDegreeScope;
            int requestedMaxDegreeScope = newMaxDegreeScope >= 0 ? newMaxDegreeScope : maxDegreeScope;

            if (string.IsNullOrWhiteSpace(requestedIpAddress))
                throw new ArgumentException("[PavoLidar] An IP address is required.", nameof(newIpAddress));

            if (requestedDegreeShift < 0 || requestedDegreeShift > 35999)
                throw new ArgumentOutOfRangeException(nameof(requestedDegreeShift), "[PavoLidar] The degree shift must be between 0 and 35999.");

            if (requestedMergeCoefficient != 1 && requestedMergeCoefficient != 2 && requestedMergeCoefficient != 4 && requestedMergeCoefficient != 8)
                throw new ArgumentOutOfRangeException(nameof(requestedMergeCoefficient), "[PavoLidar] The merge coefficient must be 1, 2, 4, or 8.");

            if (requestedMinDegreeScope < 0 || requestedMinDegreeScope > 35999 || requestedMaxDegreeScope < 0 || requestedMaxDegreeScope > 35999 || requestedMinDegreeScope >= requestedMaxDegreeScope)
                throw new ArgumentException("[PavoLidar] The degree scope must satisfy 0 <= min < max <= 35999.");

            Disconnect();

            ipAddress = requestedIpAddress;
            port = requestedPort;
            mode = (PavoMode)requestedMode;
            motorSpeed = requestedMotorSpeed;
            degreeShift = requestedDegreeShift;
            mergeCoefficient = requestedMergeCoefficient;
            minDegreeScope = requestedMinDegreeScope;
            maxDegreeScope = requestedMaxDegreeScope;
            
            m_stopRequested = false;
            m_worker = new Thread(ReadLidar) { IsBackground = true, Name = "PavoLidar" };
            m_worker.Start();
        }

        public void Disconnect()
        {
            Thread worker = m_worker;
            if (worker == null)
            {
                SetConnectionState(false);
                return;
            }

            m_stopRequested = true;
            if (worker != Thread.CurrentThread && worker.IsAlive)
            {
                worker.Join(2000);
                if (worker.IsAlive)
                    Debug.Log("[PavoLidar] Worker did not stop before the timeout.");
            }

            if (!worker.IsAlive)
                m_worker = null;

            SetConnectionState(false);
        }

        private void ReadLidar()
        {
            PavoDriver lidar = null;
            try
            {
                Debug.Log($"[PavoLidar] Connecting to {ipAddress}:{port}.");

                lidar = new PavoDriver();
                lidar.Open(ipAddress, (ushort)port);
                if (mode != PavoMode.Unknown)
                    lidar.SetMode(mode);
                lidar.SetMotorSpeed(motorSpeed);
                lidar.SetDegreeShift(degreeShift);
                lidar.SetMergeCoefficient(mergeCoefficient);
                lidar.SetDegreeScope(minDegreeScope, maxDegreeScope);

                SaveDeviceInfo(lidar);

                Debug.Log($"[PavoLidar] Connected to {ipAddress}:{port}.");
                Debug.Log("[PavoLidar] Device Information:\n" +
                    $"Firmware Version: {m_firmwareVersion}\n" +
                    $"Product Number: {m_productNumber}\n" +
                    $"Serial Number: {m_serialNumber}\n" +
                    $"Device Type: {m_deviceType}\n" +
                    $"Mode: {mode}\n" +
                    $"Degree Shift: {degreeShift}\n" +
                    $"Degree Scope: {minDegreeScope} - {maxDegreeScope}");
                
                m_pendingConnectionStates.Enqueue(true);

                while (!m_stopRequested)
                {
                    if (!lidar.TryGetScanFrame(ScanTimeoutMilliseconds, out int pointCount, out uint timestamp))
                        continue;

                    var points = new PavoScanPoint[pointCount];
                    Array.Copy(lidar.Points, points, pointCount);

                    m_pendingScanFrames.Enqueue(new ScanFrame
                    {
                        timestamp = timestamp,
                        points = points
                    });
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[PavoLidar] Error: \n" + e);
                m_pendingConnectionStates.Enqueue(false);
            }
            finally
            {
                if (lidar != null)
                {
                    try
                    {
                        lidar.Dispose();
                        Debug.Log("[PavoLidar] Device disposed.");
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("[PavoLidar] Failed to dispose device. Error: \n" + e);
                    }
                    m_pendingConnectionStates.Enqueue(false);
                }

                Debug.Log("[PavoLidar] Disconnected.");
            }
        }

        private void SetConnectionState(bool connected)
        {
            if (m_isConnected == connected)
                return;
            m_isConnected = connected;
            onIsConnectedChanged.Invoke(connected);
        }

        private void SaveDeviceInfo(PavoDriver lidar)
        {
            m_firmwareVersion = lidar.GetFirmwareVersion();
            m_productNumber = lidar.GetDeviceProductNumber();
            m_serialNumber = lidar.GetDeviceSerialNumber();
            m_deviceType = lidar.GetDeviceType();
            mode = lidar.GetMode();
            degreeShift = lidar.GetDegreeShift();
            lidar.GetDegreeScope(out minDegreeScope, out maxDegreeScope);
        }
    }

    public struct ScanFrame
    {
        public uint timestamp;
        public PavoScanPoint[] points;
    }
}