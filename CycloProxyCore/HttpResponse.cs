using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace CycloProxyCore
{
    public class HttpResponse
    {
        public string HttpVersion { get; set; } = "HTTP/1.1";

        public int StatusCode { get; set; } = 200;

        public string Description { get; set; } = "OK";

        public Dictionary<string, List<string>> Headers { get; set; } = new Dictionary<string, List<string>>();
        public int ContentLength { get; private set; }

        public byte[] BodyBytes { get; private set; } = Array.Empty<byte>();

        public void SetByteBody(byte[] bytes)
        {
            BodyBytes = bytes;
            ContentLength = bytes.Length;
        }

        public void SetStringBody(string body)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(body);
            SetByteBody(bytes);
        }

        public byte[] GetBytes()
        {
            List<byte> bytes = new List<byte>();
            bytes.AddRange(Encoding.UTF8.GetBytes($"{HttpVersion} {StatusCode} {Description}\r\n"));
            foreach (var hItem in Headers)
            {
                foreach (var hValue in hItem.Value)
                {
                    bytes.AddRange(Encoding.UTF8.GetBytes($"{hItem.Key}: {hValue}\r\n"));
                }
            }

            if (BodyBytes.Length > 0)
            {
                bytes.AddRange(Encoding.UTF8.GetBytes($"Content-Length: {ContentLength}\r\n"));
            }

            bytes.AddRange(Encoding.UTF8.GetBytes("\r\n"));

            if (BodyBytes.Length > 0)
            {
                bytes.AddRange(BodyBytes);
            }

            return bytes.ToArray();
        }
    }
}
