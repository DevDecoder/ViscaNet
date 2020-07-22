// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using System;
using System.Net;
using Microsoft.Extensions.Logging;

namespace ViscaNet
{
    public class Camera
    {
        private readonly ILogger<Camera>? _logger;

        public Camera(IPAddress ipAddress, int port, string name, ILogger<Camera>? logger = null)
        {
            EndPoint = new IPEndPoint(ipAddress, port);
            Name = name;
            _logger = logger;
        }

        public IPEndPoint EndPoint { get; }
        public string Name { get; set; }
    }
}
