// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using System;

namespace ViscaNet.Commands
{
    public readonly struct CameraVersion : IEquatable<CameraVersion>
    {
        public static readonly CameraVersion Unknown = default;

        private readonly ulong _version;
        
        public CameraVersion(ushort vendor, ushort model, ushort romVersion, byte socketNumber) =>
            _version = ((ulong)vendor << 40) +
                       ((ulong)model << 24) +
                       ((ulong)romVersion << 8) +
                       socketNumber;

        public byte SocketNumber => (byte)(_version & 0xff);
        public ushort ROMVersion => (ushort)((_version >> 8) & 0xffff);
        public ushort Model => (ushort)((_version >> 24) & 0xffff);
        public ushort Vendor => (ushort)((_version >> 40) & 0xffff);

        /// <inheritdoc />
        public bool Equals(CameraVersion other) => _version == other._version;

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is CameraVersion other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => _version.GetHashCode();

        public static bool operator ==(CameraVersion left, CameraVersion right) => left.Equals(right);

        public static bool operator !=(CameraVersion left, CameraVersion right) => !left.Equals(right);

        /// <inheritdoc />
        public override string ToString() => this == Unknown
            ? "Unknown"
            : $"Vendor: {Vendor:X4}; Model: {Model:X4}; ROM Version: {ROMVersion:X4}; Socket #: {SocketNumber:X2}";
    }
}
