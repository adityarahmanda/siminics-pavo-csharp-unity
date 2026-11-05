# Siminics Pavo SDK for Unity

A Unity package and native wrapper for connecting to Siminics Pavo LiDAR devices over the network. The package exposes scan data and device controls through a C# API and a Unity `MonoBehaviour`.

## Installation

### Install from a cloned repository

Clone this repository:

```sh
git clone <repository-url>
```

In Unity, open **Window > Package Manager**, click **+**, choose **Add package from disk...**, then select:

```text
unity-siminics-pavo/Packages/com.adityarahmanda.siminics-pavo/package.json
```

### Install via OpenUPM scoped registry

Use this method when the package version you need has been published to OpenUPM.

1. In Unity, open **Edit > Project Settings > Package Manager**.
2. Add a scoped registry with these values:

   | Label | Value |
   | --- | --- |
   | Name | Aditya Rahmanda |
   | URL | `https://package.openupm.com` |
   | Scope(s) | `com.adityarahmanda` |

3. Click **Save** or **Apply**, then open **Window > Package Manager**.
4. Install **Siminics Pavo** from **My Registries**, or click **+ > Add package by name...** and enter:

   ```text
   com.adityarahmanda.siminics-pavo
   ```

### Use in a Unity script

Add a `PavoLidarInstance` component to a GameObject and configure its IP address and port in the Inspector. It connects on `Awake` by default and raises `onScanFrame` when a scan arrives:

```csharp
using Pavo;
using UnityEngine;

public class ScanListener : MonoBehaviour
{
    [SerializeField] private PavoLidarInstance lidar;

    private void Awake()
    {
        lidar.onScanFrame.AddListener(OnScanFrame);
    }

    private void OnScanFrame(int pointCount, uint timestamp)
    {
        if (pointCount > 0 && lidar.TryGetScanPoint(pointCount - 1, out PavoScanPoint point))
            Debug.Log($"Scan {timestamp}: {point.angle}, {point.distance}, {point.intensity}");
    }
}
```
