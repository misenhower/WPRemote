using Komodex.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace Komodex.HTTP
{
    public class HttpRequest
    {
        private static readonly Log _log = new Log("HTTP Request");

        private readonly StreamSocket _socket;
        private byte[] _lineBuffer = new byte[2048];

        private HttpRequest(StreamSocket socket)
        {
            _socket = socket;
        }

        internal static async Task<HttpRequest> GetHttpRequest(StreamSocket socket)
        {
            HttpRequest request = new HttpRequest(socket);

            bool parsed = false;

            try
            {
                parsed = await request.HandleRequest();
            }
            catch { }

            if (parsed)
                return request;

            request.SendResponse(HttpStatusCode.BadRequest, "Bad request");
            _log.Debug("Could not handle request from " + socket.Information.RemoteAddress.DisplayName);
            return null;
        }

        #region Properties

        public HttpMethod Method { get; protected set; }

        public string Host { get; protected set; }

        public string Path { get; protected set; }

        public Uri Uri { get; protected set; }

        public Dictionary<string, string> Headers { get; protected set; }

        public Dictionary<string, string> QueryString { get; protected set; }

        #endregion

        #region Request Parsing

        private async Task<bool> HandleRequest()
        {
            using (DataReader reader = new DataReader(_socket.InputStream))
            {
                reader.InputStreamOptions = InputStreamOptions.Partial;

                // Get the request string (first line)
                // Example: "GET /index.html HTTP/1.1"
                string requestString = await ReadLine(reader);
                if (string.IsNullOrEmpty(requestString))
                    return false;

                _log.Trace("Request: " + requestString);

                // Split up the request string
                string[] requestParts = requestString.Split(' ');
                if (requestParts.Length < 2)
                    return false;

                // Parse the HTTP method (GET, POST, etc.)
                HttpMethod method;
                if (!Enum<HttpMethod>.TryParse(requestParts[0], true, out method))
                    return false;
                Method = method;

                // Request path (will be parsed later)
                Path = requestParts[1];

                // Read request headers
                Headers = new Dictionary<string, string>();
                while (true)
                {
                    string line = await ReadLine(reader);

                    if (string.IsNullOrEmpty(line))
                        break;

                    ProcessHeaderLine(line);

                    if (Headers.Count > 50)
                        break;
                }

                // Process URI
                if (Host == null)
                    Host = _socket.Information.LocalAddress.CanonicalName + ":" + _socket.Information.LocalPort;
                string uriString = "http://" + Host + Path;
                _log.Trace("URI String: " + uriString);
                Uri = new Uri(uriString);

                // Process query string
                QueryString = new Dictionary<string, string>();
                if (!string.IsNullOrEmpty(Uri.Query))
                {
                    string query = Uri.Query;
                    if (query.StartsWith("?"))
                        query = query.Substring(1);
                    var queryParts = query.Split('&');
                    foreach (string queryPart in queryParts)
                    {
                        int index = queryPart.IndexOf('=');
                        if (index < 1 || queryPart.Length - 1 <= index)
                            continue;

                        string name = HttpUtility.UrlDecode(queryPart.Substring(0, index));
                        string value = HttpUtility.UrlDecode(queryPart.Substring(index + 1));

                        QueryString[name] = value;

                        _log.Trace("Query String: {0} => {1}", name, value);
                    }
                }

                reader.DetachStream();
            }

            return true;
        }

        protected async Task<string> ReadLine(DataReader reader)
        {
            byte b;
            int pos = 0;
            uint bytesRead;

            while (true)
            {
                while (reader.UnconsumedBufferLength > 0)
                {
                    b = reader.ReadByte();

                    // Ignore CR
                    if (b == '\r')
                        continue;

                    // LF indicates a new line, return the string we found
                    if (b == '\n')
                        return Encoding.UTF8.GetString(_lineBuffer, 0, pos).Trim();

                    // If we've overrun our buffer, thrown an exception
                    if (pos >= _lineBuffer.Length)
                        throw new OverflowException();

                    _lineBuffer[pos++] = b;
                }

                bytesRead = await reader.LoadAsync(1024);
                if (bytesRead == 0)
                    return null;
            }
        }

        protected void ProcessHeaderLine(string line)
        {
            int index = line.IndexOf(':');
            if (index < 1 || line.Length - 1 <= index)
                return;

            string name = line.Substring(0, index).Trim();
            string value = line.Substring(index + 1).Trim();

            Headers[name] = value;

            _log.Trace("Header: {0} => {1}", name, value);

            switch (name.ToLower())
            {
                case "host":
                    Host = value;
                    break;
            }
        }

        #endregion

        #region Response

        public async Task SendResponse(HttpStatusCode statusCode, string body)
        {
            HttpResponse response = new HttpResponse();
            response.StatusCode = statusCode;
            var bodyBytes = Encoding.UTF8.GetBytes(body);
            response.Body.Write(bodyBytes, 0, bodyBytes.Length);
            await SendResponse(response);
        }

        public async Task SendResponse(HttpResponse response)
        {
            response.Body.Seek(0, SeekOrigin.Begin);
            response.Headers["Content-Length"] = response.Body.Length.ToString();

            using (DataWriter writer = new DataWriter(_socket.OutputStream))
            {
                writer.WriteString(string.Format("HTTP/1.1 {0} {1}\r\n", (int)response.StatusCode, response.StatusCode.ToString()));

                // Write headers
                foreach (var header in response.Headers)
                    writer.WriteString(string.Format("{0}: {1}\r\n", header.Key, header.Value));

                writer.WriteString("\r\n");

                // Write body
                writer.WriteBytes(response.Body.ToArray());

                // Send response
                await writer.StoreAsync();
                writer.DetachStream();
            }

            _socket.Dispose();

            _log.Info("Sent response code {0} for path: {1}", (int)response.StatusCode, Uri.PathAndQuery);
        }

        #endregion

    }
}
