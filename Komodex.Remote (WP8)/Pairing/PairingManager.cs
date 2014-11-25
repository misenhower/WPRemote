using Komodex.Bonjour;
using Komodex.Common;
using Komodex.Common.Networking;
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
        private static readonly Log _log = new Log("Pairing");

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

        public static string PIN { get; private set; }

        #endregion

        public static void Start()
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
            _httpServer.ServiceName = string.Empty; // Random port
            _httpServer.RequestReceived += HttpServer_RequestReceived;

            // Listen for network availability changes
            NetworkManager.NetworkAvailabilityChanged += NetworkManager_NetworkAvailabilityChanged;

            if (NetworkManager.IsLocalNetworkAvailable)
                UpdateIPsAndStartServices();
            else
                _log.Info("Network not available, delaying service start...");
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
                _pairingService.StopPublishing();
                _pairingService = null;
            }
        }

        private static async void UpdateIPsAndStartServices()
        {
            if (_pairingService == null)
                return;

            var hostnames = NetUtility.GetLocalHostNames().Select(h => h.CanonicalName.ToString());
            _pairingService.IPAddresses.Clear();
            _pairingService.IPAddresses.AddRange(hostnames);

            await _httpServer.Start();
            int port = int.Parse(_httpServer.ServiceName);
            _pairingService.Port = port;
            _pairingService.Publish();

            _log.Info("Pairing parameters:\nHostname: {0}\nPort: {1}\nPIN: {2}\nPairing Code: {3}", Hostname, port, PIN, _pairingService.TXTRecordData["Pair"]);
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

        private static void NetworkManager_NetworkAvailabilityChanged(object sender, NetworkAvailabilityChangedEventArgs e)
        {
            if (_pairingService == null)
                return;

            if (e.IsLocalNetworkAvailable)
            {
                UpdateIPsAndStartServices();
            }
            else
            {
                _httpServer.Stop();

                var task = _pairingService.StopPublishingAsync();

                // If the application is shutting down or suspending, wait a few seconds so the "stop publishing" messages can be sent
                if (e.ShuttingDown)
                {
                    _log.Debug("Delaying shutdown so the pairing service can be unpublished...");
                    task.Wait();
                    _log.Debug("Delay complete.");
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
