using Komodex.Bonjour;
using Komodex.Common;
using Komodex.Common.Networking;
using Komodex.HTTP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.Connectivity;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Streams;

namespace Komodex.DACP.Pairing
{
    public static class PairingManager
    {
        private static readonly Log _log = new Log("DACP Pairing Service");

        private static bool _running;

        public static event EventHandler<PairingCompleteEventArgs> PairingComplete;

        #region Properties

        /// <summary>
        /// Gets or sets the "friendly" name of the device that appears in iTunes, e.g., "Windows Phone Device".
        /// </summary>
        public static string DeviceName { get; set; }

        /// <summary>
        /// Gets or sets the format of the network hostname for the device if no other hostname is specified, e.g., "WP-{0}".
        /// </summary>
        public static string DeviceHostnameFormat { get; set; }

        private static readonly Setting<string> _deviceHostname = new Setting<string>("DACPPairingHostname");
        /// <summary>
        /// Gets or sets the network hostname used for this device.
        /// </summary>
        public static string DeviceHostname
        {
            get { return _deviceHostname.Value; }
            set
            {
                if (_deviceHostname.Value == value)
                    return;
                _deviceHostname.Value = value;
            }
        }

        private static int _port = 8080;
        /// <summary>
        /// Gets or sets the port the HTTP server listens on for incoming pairing requests. Default value is 8080.
        /// </summary>
        public static int Port
        {
            get { return _port; }
            set { _port = value; }
        }

        /// <summary>
        /// Gets the 4-digit PIN (1-9999) that will need to be entered to complete pairing.
        /// </summary>
        public static int PIN { get; private set; }

        #endregion

        #region Pairing

        private static string _validPairingHash;
        private static NetService _pairingService;

        public static async void Start()
        {
            if (_running)
                return;

            _running = true;

            // Generate hostname if necessary
            if (DeviceHostname == null)
                DeviceHostname = string.Format(DeviceHostnameFormat, GetRandomCharacters(8));

            // Generate PIN
            PIN = GetRandomPIN();
            string pinString = PIN.ToString("0000");

            // Generate pairing code
            string pairingCode = GetRandomUInt64().ToString("X16");

            // Generate valid pairing code hash
            string validPairingString = string.Format("{0}{1}\0{2}\0{3}\0{4}\0", pairingCode, pinString[0], pinString[1], pinString[2], pinString[3]);
            _validPairingHash = GetMD5Hash(validPairingString).ToLower();

            // Set up NetService
            if (_pairingService == null)
                _pairingService = new NetService();

            _pairingService.FullServiceInstanceName = DeviceHostname + "._touch-remote._tcp.local.";
            _pairingService.Port = _port;
            _pairingService.Hostname = DeviceHostname + ".local.";
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
            NetworkInformation.NetworkStatusChanged += NetworkInformation_NetworkStatusChanged;

            UpdateNetServiceIPs();

            await _httpServer.Start();
            _pairingService.Publish();

            _log.Info("Pairing parameters:\nHostname: {0}\nPort: {1}\nPIN: {2}\nPairing Code: {3}", DeviceHostname, _port, PIN, pairingCode);
        }

        
        public static async Task StopAsync()
        {
            if (!_running)
                return;

            _running = false;

            NetworkInformation.NetworkStatusChanged -= NetworkInformation_NetworkStatusChanged;

            _httpServer.RequestReceived -= HttpServer_RequestReceived;
            _httpServer.Stop();
            _httpServer = null;

            await _pairingService.StopPublishingAsync().ConfigureAwait(false);
        }

        public static async void Stop()
        {
            await StopAsync().ConfigureAwait(false);
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

        #endregion

        #region Network

        private static void UpdateNetServiceIPs()
        {
            if (!_running)
                return;

            var hostnames = NetUtility.GetLocalHostNames().Select(h => h.CanonicalName.ToString());
            _pairingService.IPAddresses.Clear();
            _pairingService.IPAddresses.AddRange(hostnames);
            _pairingService.Publish();
        }

        private static void NetworkInformation_NetworkStatusChanged(object sender)
        {
            if (!_running)
                return;

            UpdateNetServiceIPs();
            _pairingService.Publish();
        }

        #endregion

        #region HTTP Server

        private static HttpServer _httpServer;

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
                await e.Request.SendResponse(response).ConfigureAwait(false);

                // Notify that we successfully paired
                PairingComplete.RaiseOnUIThread(null, new PairingCompleteEventArgs(servicename, pairingCode.ToString("X16")));
            }
            else
            {
                await e.Request.SendResponse(HttpStatusCode.NotFound, string.Empty).ConfigureAwait(false);
            }
        }

        #endregion

        #region Random Data

        private static Random _random = new Random();

        private static string GetRandomCharacters(int count)
        {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var charArray = new char[count];
            for (int i = 0; i < charArray.Length; i++)
                charArray[i] = chars[_random.Next(chars.Length)];
            return new string(charArray);
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

        private static int GetRandomPIN()
        {
            return _random.Next(1, 10000);
        }

        #endregion

        #region MD5

        // TODO: Make this compatible with Windows Phone as well (using the MD5Core class)
        private static string GetMD5Hash(string input)
        {
            HashAlgorithmProvider provider = HashAlgorithmProvider.OpenAlgorithm("MD5");
            IBuffer inputBuffer = CryptographicBuffer.ConvertStringToBinary(input, BinaryStringEncoding.Utf8);
            IBuffer outputBuffer = provider.HashData(inputBuffer);
            return CryptographicBuffer.EncodeToHexString(outputBuffer);
        }

        #endregion

        #region DACP

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

        #endregion
    }
}
