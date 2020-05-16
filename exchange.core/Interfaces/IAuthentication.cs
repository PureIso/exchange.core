using System;

namespace exchange.core.Interfaces
{
    public interface IAuthentication
    {
        string ApiKey { get; }
        string Passphrase { get; }
        string EndpointUrl { get; }
        Uri WebSocketUri { get; }
        IAuthenticationSignature ComputeSignature(IRequest request);
        string HashString(string prehashString, byte[] secret);
    }
}
