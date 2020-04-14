using exchange.core.Interfaces;

namespace exchange.core.models
{
    public struct AuthenticationSignature : IAuthenticationSignature
    {
        public string Signature { get; set; }
        public string Timestamp { get; set; }
    }
}
