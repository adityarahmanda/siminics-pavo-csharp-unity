# Unity integration

1. Add `PavoLidar.cs` to your Unity project's `Assets` folder.
2. In Visual Studio, configure `BOOST_ROOT` to the Boost directory containing its `boost` headers and `BOOST_LIBRARYDIR` to the matching compiled Boost libraries. Build this project as **Release | x64**.
3. Copy `pavo-sharp-wrapper.dll` and any Boost runtime DLLs it depends on into `Assets/Plugins/x86_64`.
4. Ensure the Unity Editor/player is 64-bit when loading this plugin.

Example:

```csharp
using Pavo;

PavoLidar lidar = new PavoLidar();
lidar.Open("10.10.10.101", 2368);

int pointCount;
uint timestamp;
if (lidar.TryGetScanFrame(5, out pointCount, out timestamp))
{
    for (int i = 0; i < pointCount; i++)
    {
        PavoScanPoint point = lidar.Points[i];
        // point.angle is in hundredths of a degree; distance and intensity
        // are the raw values returned by the SDK.
    }
}

lidar.Dispose();
```

Use a positive scan timeout. A timeout of zero waits indefinitely; do not make that call from Unity's main thread. Device-register operations can also wait for a UDP response (up to 500 ms in the SDK), so perform LiDAR I/O on a worker thread if frame-time stalls matter. The native API serializes operations per handle; use one thread for a given `PavoLidar` instance to keep calls predictable.
