// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using ViscaNet.Commands;

namespace ViscaNet.Transports
{
    public interface IViscaTransportFactory
    {
        IViscaTransport Create();
    }

    public interface IViscaTransport : IDisposable
    {
        Task<bool> ConnectAsync(CancellationToken cancellationToken = default);
        Task<Response> SendAsync(Command command, CancellationToken cancellationToken = default);
        IObservable<bool> ConnectionState { get; }
        bool IsConnected { get; }
    }
}
