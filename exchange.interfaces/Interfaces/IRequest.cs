using System;
using System.Net.Http;

namespace exchange.core.Interfaces
{
    public interface IRequest
    {
        bool IsExpired => GetCurrentUnixTimeStamp() - TimeStamp >= 30;
        string Method { get; }
        string RequestBody { get; set; }
        string RequestUrl { get; set; }
        long TimeStamp { get; set; }
        Uri AbsoluteUri { get; set; }

        StringContent GetRequestBody(string contentType = "application/json");
        long GetCurrentUnixTimeStamp();
    }
}