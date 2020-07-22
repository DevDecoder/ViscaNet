[![Publish](https://github.com/DevDecoder/ViscaNet/workflows/Build%20and%20Publish/badge.svg)](https://github.com/DevDecoder/ViscaNet/actions?query=workflow%3A%22Build+and+Publish%22)
<!-- Add badge once released
![Nuget](https://img.shields.io/nuget/v/ViscaNet)
-->

# Description
This library provides a cross-platform service for communicating with PTZ cameras using Sony's VISCA protocol over *TCP* (note UDP is not currently supported due to it's reliability issues which can result in a camera's command sockets filling).

**NOTE**:  Package is a WIP and currently in beta, so is subject to frequent breaking changes.

# Installation
The library is [available via NuGet](https://www.nuget.org/packages?q=ViscaNet) and is delivered via NuGet Package Manager:

```
Install-Package ViscaNet
```

If you are targeting .NET Core, use the following command:

```
dotnet add package 
Install-Package ViscaNet
```

# Usage

Create a CameraConnection using:
```csharp
using var connection = new CameraConnection(
                new IPEndPoint(IPAddress.Parse("192.168.1.201"), 5678));
```

TODO

### Testing status

The following cameras have been tested:
* Minnray UV510A-20

The following OS's have been tested:
* Windows 10 Pro 2004 (19041.330)

Please let me know if you've confirmed it as working with other devices/OS's.

# Acknowledgements

* [Sony Visca Protocol](https://www.sony.net/Products/CameraSystem/CA/BRC_X1000_BRC_H800/Technical_Document/C456100121.pdf)