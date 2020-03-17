using System;

namespace exchange.core.connectivity
{
    public interface IAuthentication
    {
        string APIKey { get; }
        string PassPhrase { get; }
        string Signature { get; }
        string TimeStamp { get; }
        Uri Uri { get; }
        void Initialise(string apiKey, string passPhrase, string secret, Uri uri);
        string ComputeSignature(string message);
        void ComputeSignature(IRequest request);
        string ComputeHash(string message, byte[] secret);
    }
}
