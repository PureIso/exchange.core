namespace exchange.core.Interfaces
{
    public interface IAuthenticationSignature
    {
        string Signature { get; set; }
        string Timestamp { get; set; }
    }
}
