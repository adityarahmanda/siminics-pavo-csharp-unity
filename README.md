# Siminics Pavo SDK for Unity

A Unity package and native wrapper for connecting to Siminics Pavo LiDAR devices over the network. The package exposes scan data and device controls through a C# API and a Unity `MonoBehaviour`.

<img width="479" height="588" alt="Screenshot 2026-11-06 092557" src="https://github.com/user-attachments/assets/95cd2087-bc13-4e06-94f3-81d1a5f7e356" />

<img width="895" height="433" alt="Screenshot 2026-11-06 092718" src="https://github.com/user-attachments/assets/dd3cb5be-c1d4-4958-9e2c-111e322e103c" />

## Installation

### Install via Clone

Clone this project using this command line

```sh
git clone https://github.com/adityarahmanda/siminics-pavo-csharp-unity.git
```

### Install via Git Url

1. Open Unity Package Manager

2. Click `+`

3. Select `Add package from git URL...`

4. Paste this URL

```
https://github.com/adityarahmanda/siminics-pavo-csharp-unity.git?path=/unity-siminics-pavo/Packages/com.adityarahmanda.siminics-pavo
```

### Install via Tarball Releases

1. Go to [Releases](https://github.com/adityarahmanda/unity-balinese-date/releases) page

2. Download `.tgz` from the latest release

3. In Unity, open Window/Package Manager

    - Click `+`
    - Select `Add package by tarball...`
    - Select the downloaded file to import it

### Install via Scoped Registry

1. In Unity, Open `Edit -> Project Settings -> Package Manager`

2. Add a new Scoped Registry (or edit the existing OpenUPM entry)

    |Label|Value|
    |----|----|
    |Name|Aditya Rahmanda|
    |URL|https://package.openupm.com|
    |Scope(s)|com.adityarahmanda|

3. Click `Save` or `Apply`

4. Open Window/Package Manager

5. There are several ways to add the package

    - Go to My Registries section, 
    - Find `Aditya Rahmanda`'s `Siminics Pavo` package 
    - Click `Install` Button
  
    or

    - Click `+`
    - Select `Add package by name...`
    - Paste `com.adityarahmanda.siminics-pavo` into name
    - Click Add

### Use in a Unity script

Add a `PavoLidarInstance` component to a GameObject and configure its IP address and port in the Inspector. It connects on `Awake` by default and raises `onScanFrame` when a scan arrives:

```csharp
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
        if (pointCount > 0 && lidar.TryGetScanPoint(pointCount - 1, out PavoScanPoint point))
            Debug.Log($"Scan {timestamp}: {point.angle}, {point.distance}, {point.intensity}");
    }
}
```
