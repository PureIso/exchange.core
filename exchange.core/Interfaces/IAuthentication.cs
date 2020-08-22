using System;
using exchange.core.implementations;

namespace exchange.core.Interfaces
{
    public interface IAuthentication
    {
        string ApiKey { get; }
        string Passphrase { get; }
        string EndpointUrl { get; }
        Uri WebSocketUri { get; }
        AuthenticationSignature ComputeSignature(IRequest request);
        string ComputeSignature(string message);
        string HashString(string prehashString, byte[] secret);
    }
}