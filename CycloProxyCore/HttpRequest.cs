using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace CycloProxyCore
{
    public class HttpRequest
    {
        public HttpMethod Method { get; private set; }
        public Uri Url { get; private set; }
        public string HttpVersion { get; private set; }

        public HttpContentHeaders Headers { get; private set; }

        public HttpRequest()
        {
            //HttpParser
        }

        public byte[] Body { get; private set; }

        public bool TryParseHeaderFromBytes(byte[] bytes)
        {
            // Split header line by line
            string headerString = Encoding.UTF8.GetString(bytes);
            string[] headerSplit = headerString.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
            if (headerSplit.Length == 0) return false;

            // Get request line at first
            string requestLine = headerSplit[0];
            string[] requestLineSplit = requestLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (requestLineSplit.Length < 3) return false;

            // Try to set method
            Method = new HttpMethod(requestLineSplit[0]);

            // Try to parse url
            Uri? parsedUri;
            bool isValidUrl = Uri.TryCreate(requestLineSplit[1], UriKind.Absolute, out parsedUri);
            if (!isValidUrl || parsedUri == null) return false;

            // Set valid url
            Url = parsedUri;

            // Set protocol version
            HttpVersion = requestLineSplit[2];

            // Parse header
            for (int i = 1; i < headerSplit.Length; i++)
            {
                string headerLine = headerSplit[i];

                // Detect first colon
                int delimiterPosition = headerLine.IndexOf(':');
                if (delimiterPosition == -1) return false;

                // Get header name and value
                string headerName = headerLine.Substring(0, delimiterPosition).ToLower();
                string headerValue = headerLine.Substring(delimiterPosition + 1);

                // Add to header collection
                Headers.Add(headerName, headerValue);
            }

            return true;
        }

        public void SetByteBody(byte[] bytes)
        {
            Body = bytes;
            Headers.ContentLength = bytes.Length;
        }

        public void SetStringBody(string body)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(body);
            SetByteBody(bytes);
        }
    }
}
