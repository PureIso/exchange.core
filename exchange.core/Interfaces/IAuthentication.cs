using exchange.core.implementations;
using System;

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
