using System;
using System.Net;

namespace FeiniuBus.Restful.Exceptions
{
    public class WebServiceException : Exception
    {
        public WebServiceException()
        {
        }

        public WebServiceException(string message) : base(message)
        {
        }

        public WebServiceException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public HttpStatusCode StatusCode { get; set; }

        public string ResponseBody { get; set; }
    }
}
