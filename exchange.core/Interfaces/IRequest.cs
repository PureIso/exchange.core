using System;
using System.Net.Http;

namespace exchange.core.Interfaces
{
    public interface IRequest
    {
        string Method { get; } 
        string RequestQuery { get; set; }
        string RequestBody { get; set; }
        string RequestUrl { get; set; }
        long TimeStamp { get; set; }
        string RequestSignature { get; set; }
        Uri AbsoluteUri { get; set; }
        /// <summary>
        /// All request bodies should have content type application/json and be valid JSON.
        /// </summary>
        /// <param name="contentType">The content type</param>
        /// <returns>String Content</returns>
        StringContent GetRequestBody(string contentType = "application/json");

        string Compose();
    }
}