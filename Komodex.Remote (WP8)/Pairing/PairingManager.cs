using Komodex.Bonjour;
using Komodex.Common;
using Komodex.HTTP;
using Komodex.Remote.ServerManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Connectivity;

namespace Komodex.Remote.Pairing
{
    public static class PairingManager
    {
        private static readonly Log _log = new Log("Pairing") { Level = LogLevel.All };

        private const string DeviceName = "Windows Phone 8 Device";
        private const string DeviceHostnameFormat = "WP8-{0}";

        private static string _validPairingHash;

        private static NetService _pairingService;
        private static HttpServer _httpServer;

        private static Random _random = new Random();

        public static event EventHandler<ServerConnectionInfoEventArgs> PairingComplete;

        #region Properties

        private static readonly Setting<string> _hostname = new Setting<string>("PairingHostname");
        public static string Hostname
        {
            get { return _hostname.Value; }
            private set
            {
                if (_hostname.Value == value)
                    return;

                _hostname.Value = value;
            }
        }

        private static int _port = 8080;
        public static int Port
        {
            get { return _port; }
            private set { _port = value; }
        }

        public static string PIN { get; private set; }

        #endregion

        public static async void Start()
        {
            if (_pairingService != null)
                return;

            // Generate hostname
            if (Hostname == null)
            {
                var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
                var charArray = new char[8];
                for (int i = 0; i < charArray.Length; i++)
                    charArray[i] = chars[_random.Next(chars.Length)];
                Hostname = string.Format(DeviceHostnameFormat, new string(charArray));
            }

            // Generate PIN
            PIN = _random.Next(1, 9999).ToString("0000");

            // Generate pairing code
            string pairingCode = GetRandomUInt64().ToString("X16");

            // Generate valid pairing code hash
            string validPairingString = string.Format("{0}{1}\0{2}\0{3}\0{4}\0", pairingCode, PIN[0], PIN[1], PIN[2], PIN[3]);
            _validPairingHash = MD5Core.GetHashString(validPairingString).ToLower();

            // Set up NetService
            _pairingService = new NetService();

            _pairingService.FullServiceInstanceName = Hostname + "._touch-remote._tcp.local.";
            _pairingService.Port = _port;
            _pairingService.Hostname = _hostname.Value + ".local.";
            Dictionary<string, string> txt = new Dictionary<string, string>();
            txt["txtvers"] = "1";
            txt["DvNm"] = DeviceName;
            txt["RemV"] = "10000";
            txt["DvTy"] = "iPhone";
            txt["RemN"] = "Remote";
            txt["Pair"] = pairingCode;
            _pairingService.TXTRecordData = txt;

            // Set up HttpServer
            _httpServer = new HttpServer();
            _httpServer.ServiceName = _port.ToString();
            _httpServer.RequestReceived += HttpServer_RequestReceived;

            // Listen for network availability changes
            NetworkManager.NetworkAvailabilityChanged += NetworkManager_NetworkAvailabilityChanged;

            if (NetworkManager.IsLocalNetworkAvailable)
            {
                UpdateNetServiceIPs();
                await _httpServer.Start();
                _pairingService.Publish();
            }

            _log.Trace("Pairing parameters:\nHostname: {0}\nPort: {1}\nPIN: {2}\nPairing Code: {3}", Hostname, _port, PIN, pairingCode);
        }

        public static void Stop()
        {
            NetworkManager.NetworkAvailabilityChanged -= NetworkManager_NetworkAvailabilityChanged;

            if (_httpServer != null)
            {
                _httpServer.RequestReceived -= HttpServer_RequestReceived;
                _httpServer.Stop();
                _httpServer = null;
            }

            if (_pairingService != null)
            {
                _pairingService.Stop();
                _pairingService = null;
            }
        }

        private static void UpdateNetServiceIPs()
        {
            if (_pairingService == null)
                return;

            var hostnames = NetworkInformation.GetHostNames();

            _pairingService.IPAddresses.Clear();

            foreach (var host in hostnames)
            {
                if (host.Type != HostNameType.Ipv4)
                    continue;
                _pairingService.IPAddresses.Add(IPAddress.Parse(host.CanonicalName));
                _log.Debug("Listening on IP: " + host.DisplayName);
            }
        }

        private static async void HttpServer_RequestReceived(object sender, HttpRequestEventArgs e)
        {
            if (sender != _httpServer)
                return;

            if (IsValidPairingRequest(e.Request))
            {
                string servicename = e.Request.QueryString["servicename"];
                _log.Info("Paired with service ID: " + servicename);

                // Stop HTTP server and Bonjour publisher
                Stop();

                // Generate new pairing code
                ulong pairingCode = GetRandomUInt64();

                // Build response
                List<byte> bodyBytes = new List<byte>();
                bodyBytes.AddRange(GetDACPFormattedBytes("cmpg", pairingCode));
                bodyBytes.AddRange(GetDACPFormattedBytes("cmnm", DeviceName));
                bodyBytes.AddRange(GetDACPFormattedBytes("cmty", "iPhone"));
                byte[] responseBytes = GetDACPFormattedBytes("cmpa", bodyBytes.ToArray());

                // Send response
                HttpResponse response = new HttpResponse();
                response.Body.Write(responseBytes, 0, responseBytes.Length);
                await e.Request.SendResponse(response);

                // Save server connection info
                ServerConnectionInfo info = new ServerConnectionInfo();
                info.ServiceID = servicename;
                info.PairingCode = pairingCode.ToString("X16");

                ServerManager.AddServerInfo(info);

                PairingComplete.Raise(null, new ServerConnectionInfoEventArgs(info));
            }
            else
            {
                await e.Request.SendResponse(HttpStatusCode.NotFound, string.Empty);
            }
        }

        private static bool IsValidPairingRequest(HttpRequest request)
        {
            if (request.Uri.AbsolutePath != "/pair")
                return false;

            if (!request.QueryString.ContainsKey("pairingcode") || !request.QueryString.ContainsKey("servicename"))
                return false;

            string pairingcode = request.QueryString["pairingcode"].ToLower();

            if (pairingcode != _validPairingHash)
                return false;

            return true;
        }

        private static async void NetworkManager_NetworkAvailabilityChanged(object sender, NetworkAvailabilityChangedEventArgs e)
        {
            if (_pairingService == null)
                return;

            if (e.IsLocalNetworkAvailable)
            {
                UpdateNetServiceIPs();
                _pairingService.Publish();
                await _httpServer.Start();
            }
            else
            {
                _httpServer.Stop();

                // If the application is shutting down or suspending, wait a few seconds so the "stop publishing" messages can be sent
                if (e.ShuttingDown)
                {
                    ThreadUtility.RunOnBackgroundThread(() => _pairingService.Stop());

                    _log.Info("Delaying shutdown so the pairing service can be unpublished...");
                    ThreadUtility.Delay(4000);
                    _log.Info("Delay complete.");
                }
                else
                {
                    _pairingService.Stop();
                }
            }
        }

        private static ulong GetRandomUInt64()
        {
            var buffer = new byte[sizeof(ulong)];
            _random.NextBytes(buffer);
            ulong value = (ulong)BitConverter.ToInt64(buffer, 0);
            if (value == 0)
                value = 1;
            return value;
        }

        private static byte[] GetDACPFormattedBytes(string tag, string value)
        {
            return GetDACPFormattedBytes(tag, Encoding.UTF8.GetBytes(value));
        }

        private static byte[] GetDACPFormattedBytes(string tag, ulong value)
        {
            value = (ulong)BitUtility.HostToNetworkOrder((long)value);
            return GetDACPFormattedBytes(tag, BitConverter.GetBytes(value));
        }

        private static byte[] GetDACPFormattedBytes(string tag, byte[] value)
        {
            byte[] result = new byte[8 + value.Length];

            // Tag
            result[0] = (byte)tag[0];
            result[1] = (byte)tag[1];
            result[2] = (byte)tag[2];
            result[3] = (byte)tag[3];

            // Length
            BitConverter.GetBytes(BitUtility.HostToNetworkOrder(value.Length)).CopyTo(result, 4);

            value.CopyTo(result, 8);

            return result;
        }
    }
}
