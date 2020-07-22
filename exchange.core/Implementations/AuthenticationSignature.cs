using exchange.core.Interfaces;

namespace exchange.core.implementations
{
    public struct AuthenticationSignature : IAuthenticationSignature
    {
        public string Signature { get; set; }
        public string Timestamp { get; set; }
    }
}
