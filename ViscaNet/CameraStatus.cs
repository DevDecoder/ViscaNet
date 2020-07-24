// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using DynamicData.Kernel;
using ViscaNet.Commands;

namespace ViscaNet
{
    public class CameraStatus
    {
        public static readonly CameraStatus Unknown = new CameraStatus(CameraVersion.Unknown, PowerMode.Unknown, false);

        public CameraStatus With(
            Optional<CameraVersion> version = default,
            Optional<PowerMode> powerMode = default,
            Optional<bool> connected = default) =>
            TryWith(out var status, version, powerMode, connected)
                ? status
                : this;

        public bool TryWith(
            out CameraStatus status,
            Optional<CameraVersion> version = default,
            Optional<PowerMode> powerMode = default,
            Optional<bool> connected = default)
        {
            var changed = false;
            var v = Version;
            if (version.HasValue && version.Value != v)
            {
                v = version.Value;
                changed = true;
            }

            var p = PowerMode;
            if (powerMode.HasValue && powerMode.Value != p)
            {
                p = powerMode.Value;
                changed = true;
            }

            var b = Connected;
            if (connected.HasValue && connected.Value != b)
            {
                b = connected.Value;
                changed = true;
            }

            status = changed ? new CameraStatus(v, p, b) : this;
            return changed;
        }

        private CameraStatus(CameraVersion version, PowerMode powerMode, bool connected)
        {
            Version = version;
            PowerMode = powerMode;
            Connected = connected;
        }

        public CameraVersion Version { get; }
        public PowerMode PowerMode { get; }
        public bool Connected { get; }

        /// <inheritdoc />
        public override string ToString() => this == Unknown
            ? "Unknown"
            : $"Version: {Version}; Power: {PowerMode}; Connect: {Connected}";
    }
}
