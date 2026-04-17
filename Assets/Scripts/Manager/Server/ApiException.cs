using System;

namespace Manager.Server
{
    public sealed class ApiException : Exception
    {
        public long StatusCode { get; }
        public string ResponseText { get; }

        public ApiException(long statusCode, string responseText, string message) : base(message)
        {
            StatusCode = statusCode;
            ResponseText = responseText;
        }
    }
}
