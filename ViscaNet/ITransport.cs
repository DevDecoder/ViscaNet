// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;

namespace ViscaNet
{
    public interface IViscaTransportFactory
    {
        IViscaTransport Create();
    }

    public interface IViscaTransport : IDisposable
    {
        Task ConnectAsync(int timeout);
        Task<ViscaResponse> SendCommandAsync(ViscaCommand command);
    }
}
