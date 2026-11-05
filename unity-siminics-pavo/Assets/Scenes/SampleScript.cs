using Pavo;
using UnityEngine;

public class SampleScript : MonoBehaviour
{
    [SerializeField] private PavoLidarInstance lidar;

    private void Awake()
    {
        lidar.onScanFrame.AddListener(OnScanFrame);
    }

    private void OnScanFrame(int pointCount, uint timestamp)
    {
        if (pointCount == 0)
        {
            Debug.Log("Received empty scan frame at timestamp " + timestamp);
            return;
        }

        lidar.TryGetScanPoint(pointCount - 1, out PavoScanPoint lastScanPoint);
        Debug.Log($"Received scan frame with {pointCount} points at timestamp {timestamp}. Last scan point: ({lastScanPoint.angle}, {lastScanPoint.distance}, {lastScanPoint.intensity})");
    }
}
