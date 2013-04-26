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
        private static readonly Log _log = new Log("Pairing");

        private const string DeviceName = "Windows Phone 8 Device";
        private const string DeviceHostnameFormat = "WP8-{0}";

        private static int _port = 8080;
        private static string _hostname;
        private static string _pin;
        private static string _pairingCode;

        private static NetService _pairingService;
        private static HttpServer _httpServer;

        private static Random _random = new Random();

        public static async void Start()
        {
            if (_pairingService != null)
                return;

            // Generate hostname
            if (_hostname == null)
            {
                var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
                var charArray = new char[8];
                for (int i = 0; i < charArray.Length; i++)
                    charArray[i] = chars[_random.Next(chars.Length)];
                _hostname = string.Format(DeviceHostnameFormat, new string(charArray));
            }

            // Generate PIN
            _pin = _random.Next(1, 9999).ToString("0000");

            // Generate pairing code
            var buffer = new byte[sizeof(ulong)];
            _random.NextBytes(buffer);
            ulong code = (ulong)BitConverter.ToInt64(buffer, 0);
            if (code == 0)
                code = 1;
            _pairingCode = code.ToString("X16");

            // Set up NetService
            _pairingService = new NetService();
            _pairingService.FullServiceInstanceName = _hostname + "._touch-remote._tcp.local.";
            _pairingService.Port = _port;
            _pairingService.Hostname = _hostname + ".local.";
            Dictionary<string, string> txt = new Dictionary<string, string>();
            txt["txtvers"] = "1";
            txt["DvNm"] = DeviceName;
            txt["RemV"] = "10000";
            txt["DvTy"] = "iPhone";
            txt["RemN"] = "Remote";
            txt["Pair"] = _pairingCode;
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
        }

        public static void Stop()
        {
            NetworkManager.NetworkAvailabilityChanged -= NetworkManager_NetworkAvailabilityChanged;

            if (_pairingService != null)
            {
                _pairingService.Stop();
                _pairingService = null;
            }

            if (_httpServer != null)
            {
                _httpServer.Stop();
                _httpServer = null;
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
            await e.Request.SendResponse(HttpStatusCode.NotFound, "Not found");
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
                _pairingService.Stop();
                _httpServer.Stop();
            }
        }

    }
}
