# Siminics Pavo SDK for Unity

[![openupm](https://img.shields.io/npm/v/com.adityarahmanda.siminics-pavo?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.adityarahmanda.siminics-pavo/)

Siminics Pavo SDK Wrapper for Unity. It allows you to connect to and process data from the Siminics Pavo sensor.

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

1. Go to [Releases](https://github.com/adityarahmanda/siminics-pavo-csharp-unity/releases) page

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

### Using in Code

```csharp
using Rahmanda.PavoSDK;
using UnityEngine;

public class SampleScript : MonoBehaviour
{
    [SerializeField] private PavoLidar lidar;

    private void Awake()
    {
        lidar.onScanFrame.AddListener(OnScanFrame);
    }

    private void OnScanFrame(ScanFrame scanFrame)
    {
        PavoScanPoint[] points = scanFrame.points;
        if (points != null && points.Length > 0)
        {
            var lastPoint = points[points.Length - 1];
            Debug.Log($"Received scan frame with {points.Length} points. Last scan point: {lastPoint.angle}, {lastPoint.distance}, Intensity: {lastPoint.intensity}");
        }
        else
        {
            Debug.Log("Received scan frame with no points.");
        }
    }
}
```
