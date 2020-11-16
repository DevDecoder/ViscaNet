// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using System;
using DevDecoder.ViscaNet.Commands;
using DynamicData.Kernel;

namespace DevDecoder.ViscaNet
{
    public class CameraStatus
    {
        public static readonly CameraStatus Unknown = new CameraStatus(CameraVersion.Unknown, PowerMode.Unknown, ConnectionState.Unknown);

        private CameraStatus(CameraVersion version, PowerMode powerMode, ConnectionState connection)
        {
            Version = version;
            PowerMode = powerMode;
            Connection = connection;
        }

        public CameraVersion Version { get; }
        public PowerMode PowerMode { get; }
        public ConnectionState Connection { get; }

        public CameraStatus With(
            Optional<CameraVersion> version = default,
            Optional<PowerMode> powerMode = default,
            Optional<ConnectionState> connected = default) =>
            TryWith(out var status, version, powerMode, connected)
                ? status
                : this;

        public bool TryWith(
            out CameraStatus status,
            Optional<CameraVersion> version = default,
            Optional<PowerMode> powerMode = default,
            Optional<ConnectionState> connected = default)
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

            var b = Connection;
            if (connected.HasValue && connected.Value != b)
            {
                b = connected.Value;
                changed = true;
            }

            if (!changed)
            {
                status = this;
                return false;
            }

            status = v != CameraVersion.Unknown || p!= PowerMode.Unknown || b != ConnectionState.Unknown
                ? new CameraStatus(v, p, b)
                : Unknown;
            return true;
        }

        /// <inheritdoc />
        public override string ToString() => this == Unknown
            ? "Unknown"
            : $"Version: {Version}; Power: {PowerMode}; Connect: {Connection}";
    }
}
