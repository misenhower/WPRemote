using Komodex.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace Komodex.DACP
{
    public partial class DACPServer
    {
        private const int AppleTVEncryptionKey = 0x4567;

        #region Properties

        private bool _isAppleTVKeyboardVisible;
        public bool IsAppleTVKeyboardVisible
        {
            get { return _isAppleTVKeyboardVisible; }
            private set
            {
                if (_isAppleTVKeyboardVisible == value)
                    return;
                _isAppleTVKeyboardVisible = value;
                PropertyChanged.RaiseOnUIThread(this, "IsAppleTVKeyboardVisible");
            }
        }

        private AppleTVKeyboardType _currentAppleTVKeyboardType;
        public AppleTVKeyboardType CurrentAppleTVKeyboardType
        {
            get { return _currentAppleTVKeyboardType; }
            private set
            {
                if (_currentAppleTVKeyboardType == value)
                    return;
                _currentAppleTVKeyboardType = value;
                PropertyChanged.RaiseOnUIThread(this, "CurrentAppleTVKeyboardType");
            }
        }

        private string _currentAppleTVKeyboardTitle;
        public string CurrentAppleTVKeyboardTitle
        {
            get { return _currentAppleTVKeyboardTitle; }
            private set
            {
                if (_currentAppleTVKeyboardTitle == value)
                    return;
                _currentAppleTVKeyboardTitle = value;
                PropertyChanged.RaiseOnUIThread(this, "CurrentAppleTVKeyboardTitle");
            }
        }

        private string _currentAppleTVKeyboardSubText;
        public string CurrentAppleTVKeyboardSubText
        {
            get { return _currentAppleTVKeyboardSubText; }
            private set
            {
                if (_currentAppleTVKeyboardSubText == value)
                    return;
                _currentAppleTVKeyboardSubText = value;
                PropertyChanged.RaiseOnUIThread(this, "CurrentAppleTVKeyboardSubText");
            }
        }

        private string _currentAppleTVKeyboardString;
        public string CurrentAppleTVKeyboardString
        {
            get { return _currentAppleTVKeyboardString; }
            private set
            {
                if (_currentAppleTVKeyboardString == value)
                    return;
                _currentAppleTVKeyboardString = value;
                PropertyChanged.RaiseOnUIThread(this, "CurrentAppleTVKeyboardString", "BindableAppleTVKeyboardString");
            }
        }

        public string BindableAppleTVKeyboardString
        {
            get { return _currentAppleTVKeyboardString; }
            set
            {
                if (_currentAppleTVKeyboardString == value)
                    return;
                _currentAppleTVKeyboardString = value;
                var task = SendAppleTVKeyboardStringUpdateCommandAsync(value, false);
                PropertyChanged.RaiseOnUIThread(this, "CurrentAppleTVKeyboardString");
            }
        }

        #endregion

        #region Remote Control Buttons

        private async Task<bool> SendAppleTVControlCommandAsync(string command)
        {
            List<byte> contentBytes = new List<byte>();
            contentBytes.AddRange(DACPUtility.GetDACPFormattedBytes("cmcc", "0"));
            contentBytes.AddRange(DACPUtility.GetDACPFormattedBytes("cmbe", command));

            ByteArrayContent content = new ByteArrayContent(contentBytes.ToArray());

            DACPRequest request = new DACPRequest("/ctrl-int/1/controlpromptentry");
            request.HttpContent = content;

            try
            {
                await SubmitRequestAsync(request).ConfigureAwait(false);
            }
            catch { return false; }
            return true;
        }

        /// <summary>
        /// Menu command.
        /// </summary>
        public Task<bool> SendAppleTVMenuCommandAsync()
        {
            return SendAppleTVControlCommandAsync("menu");
        }

        /// <summary>
        /// Alternate menu command (equivalent to pressing and holding the Select button).
        /// </summary>
        public Task<bool> SendAppleTVContextMenuCommandAsync()
        {
            return SendAppleTVControlCommandAsync("contextmenu");
        }

        /// <summary>
        /// Top menu command (equivalent to pressing and holding the Menu button).
        /// </summary>
        public Task<bool> SendAppleTVTopMenuCommandAsync()
        {
            return SendAppleTVControlCommandAsync("topmenu");
        }

        /// <summary>
        /// Select command.
        /// </summary>
        public Task<bool> SendAppleTVSelectCommand()
        {
            return SendAppleTVControlCommandAsync("select");
        }

        #endregion

        #region Text Entry

        private string _appleTVKeyboardSessionID;

        private async Task<bool> SendAppleTVKeyboardStringUpdateCommandAsync(string value, bool done)
        {
            string command = (done) ? "PromptDone" : "PromptUpdate";

            List<byte> contentBytes = new List<byte>();
            contentBytes.AddRange(DACPUtility.GetDACPFormattedBytes("cmcc", _appleTVKeyboardSessionID));
            contentBytes.AddRange(DACPUtility.GetDACPFormattedBytes("cmbe", command));
            contentBytes.AddRange(DACPUtility.GetDACPFormattedBytes("cmte", value));

            ByteArrayContent content = new ByteArrayContent(contentBytes.ToArray());

            DACPRequest request = new DACPRequest("/ctrl-int/1/controlpromptentry");
            request.HttpContent = content;

            try
            {
                await SubmitRequestAsync(request).ConfigureAwait(false);
            }
            catch { return false; }
            return true;
        }

        public Task<bool> SendAppleTVKeyboardDoneCommandAsync()
        {
            return SendAppleTVKeyboardStringUpdateCommandAsync(_currentAppleTVKeyboardString, true);
        }

        #endregion

        #region Control Prompt Update

        private int _controlPromptUpdateNumber = 1;
        private CancellationTokenSource _currentControlPromptUpdateCancellationTokenSource;

        protected Task<bool> GetFirstControlPromptUpdateAsync()
        {
            _controlPromptUpdateNumber = 1;
            return GetControlPromptUpdateAsync(CancellationToken.None);
        }

        protected async Task<bool> GetControlPromptUpdateAsync(CancellationToken cancellationToken)
        {
            DACPRequest request = new DACPRequest("/controlpromptupdate");
            request.QueryParameters["prompt-id"] = _controlPromptUpdateNumber.ToString();

            try
            {
                var response = await SubmitRequestAsync(request).ConfigureAwait(false);

                if (cancellationToken.IsCancellationRequested)
                    return false;

                _controlPromptUpdateNumber = response.Nodes.First(n => n.Key == "miid").Value.GetInt32Value();

                // Parse response
                // This comes back as a list of string key/value pairs.
                var nodeDictionary = response.Nodes.Where(n => n.Key == "mdcl").Select(n => DACPNodeDictionary.Parse(n.Value)).ToDictionary(n => n.GetString("cmce"), n => n.GetString("cmcv"));

#if DEBUG
                if (_log.EffectiveLevel <= LogLevel.Trace)
                {
                    string logMessage = "ControlPromptUpdate Response:\r\n";
                    foreach (var kvp in nodeDictionary)
                        logMessage += string.Format("{0}: {1}\r\n", kvp.Key, kvp.Value);
                    _log.Trace(logMessage);
                }
#endif

                if (nodeDictionary.ContainsKey("kKeybMsgKey_MessageType"))
                {
                    switch (nodeDictionary["kKeybMsgKey_MessageType"])
                    {
                        case "0": // Show keyboard
                            CurrentAppleTVKeyboardTitle = nodeDictionary["kKeybMsgKey_Title"];
                            CurrentAppleTVKeyboardSubText = nodeDictionary["kKeybMsgKey_SubText"];
                            CurrentAppleTVKeyboardType = (AppleTVKeyboardType)int.Parse(nodeDictionary["kKeybMsgKey_KeyboardType"]);
                            CurrentAppleTVKeyboardString = nodeDictionary["kKeybMsgKey_String"];
                            _appleTVKeyboardSessionID = nodeDictionary["kKeybMsgKey_SessionID"];
                            IsAppleTVKeyboardVisible = true;
                            break;

                        case "2": // Hide keyboard
                            IsAppleTVKeyboardVisible = false;
                            break;

                        case "5": // Trackpad interface update
                            _appleTVTrackpadPort = int.Parse(nodeDictionary["kKeybMsgKey_String"]) ^ AppleTVEncryptionKey;
                            _appleTVTrackpadKey = BitUtility.NetworkToHostOrder(int.Parse(nodeDictionary["kKeybMsgKey_SubText"]) ^ AppleTVEncryptionKey);
                            _log.Info("Apple TV virtual trackpad parameters updated: Encryption key: {0:X8} Port: {1}", _appleTVTrackpadKey, _appleTVTrackpadPort);
                            break;
                    }
                }
            }
            catch
            {
                return false;
            }
            return true;
        }

        protected async void SubscribeToControlPromptUpdates()
        {
            TimeSpan autoCancelTimeSpan = TimeSpan.FromSeconds(45);
            TimeSpan resubmitDelay = TimeSpan.FromSeconds(2);
            CancellationToken token;

            while (IsConnected)
            {
                _currentControlPromptUpdateCancellationTokenSource = new CancellationTokenSource();
                token = _currentControlPromptUpdateCancellationTokenSource.Token;

#if WP7
                var updateTask = GetControlPromptUpdateAsync(token);
                var cancelTask = TaskEx.Delay(autoCancelTimeSpan, token);
                await TaskEx.WhenAny(updateTask, cancelTask).ConfigureAwait(false);
#else
                var updateTask = GetControlPromptUpdateAsync(token);
                var cancelTask = Task.Delay(autoCancelTimeSpan, token);
                await Task.WhenAny(updateTask, cancelTask).ConfigureAwait(false);
#endif

                if (token.IsCancellationRequested)
                    return;

                _currentControlPromptUpdateCancellationTokenSource.Cancel();

                if (updateTask.Status == TaskStatus.RanToCompletion)
                {
                    if (updateTask.Result == false)
                    {
                        SendConnectionError();
                        return;
                    }

#if WP7
                    await TaskEx.Delay(resubmitDelay).ConfigureAwait(false);
#else
                    await Task.Delay(resubmitDelay).ConfigureAwait(false);
#endif
                }
            }
        }

        /// <summary>
        /// Requests an update for the virtual trackpad connection parameters.
        /// </summary>
        private async Task<bool> RequestAppleTVTrackpadInfoUpdateAsync()
        {
            List<byte> contentBytes = new List<byte>();
            contentBytes.AddRange(DACPUtility.GetDACPFormattedBytes("cmcc", "0"));
            contentBytes.AddRange(DACPUtility.GetDACPFormattedBytes("cmbe", "DRPortInfoRequest"));
            contentBytes.AddRange(DACPUtility.GetDACPFormattedBytes("cmte", string.Format("{0},0x{1}", AppleTVEncryptionKey, PairingCode)));

            ByteArrayContent content = new ByteArrayContent(contentBytes.ToArray());

            DACPRequest request = new DACPRequest("/ctrl-int/1/controlpromptentry");
            request.HttpContent = content;

            try
            {
                await SubmitRequestAsync(request).ConfigureAwait(false);
            }
            catch { return false; }
            return true;
        }

        /// <summary>
        /// Requests an update for the keyboard state and session ID.
        /// </summary>
        private async Task<bool> RequestAppleTVKeyboardInfoUpdateAsync()
        {
            List<byte> contentBytes = new List<byte>();
            contentBytes.AddRange(DACPUtility.GetDACPFormattedBytes("cmcc", "0"));
            contentBytes.AddRange(DACPUtility.GetDACPFormattedBytes("cmbe", "PromptResendReq"));

            ByteArrayContent content = new ByteArrayContent(contentBytes.ToArray());

            DACPRequest request = new DACPRequest("/ctrl-int/1/controlpromptentry");
            request.HttpContent = content;

            try
            {
                await SubmitRequestAsync(request).ConfigureAwait(false);
            }
            catch { return false; }
            return true;
        }

        #endregion

        #region Virtual Trackpad

        private int _appleTVTrackpadPort;
        private int _appleTVTrackpadKey;

        private readonly SemaphoreSlim _trackpadSemaphore = new SemaphoreSlim(0, 1);
        private CancellationTokenSource _trackpadConnectionCancellationTokenSource;
        private short _trackpadX;
        private short _trackpadY;
        private bool _trackpadTouchDown;
        private DateTime _trackpadUpdatedAt;

        public void AppleTVTrackpadTouchStart(short x, short y)
        {
            // Connect the trackpad control socket if necessary
            var token = _trackpadConnectionCancellationTokenSource;
            if (token == null)
                ConnectAppleTVTrackpadSocket();

            lock (_trackpadSemaphore)
            {
                _trackpadX = x;
                _trackpadY = y;
                _trackpadTouchDown = true;
                _trackpadUpdatedAt = DateTime.Now;

                if (_trackpadSemaphore.CurrentCount < 1)
                    _trackpadSemaphore.Release();
            }
        }

        public void AppleTVTrackpadTouchMove(short x, short y)
        {
            lock (_trackpadSemaphore)
            {
                _trackpadX = x;
                _trackpadY = y;
                _trackpadTouchDown = true;
                _trackpadUpdatedAt = DateTime.Now;

                if (_trackpadSemaphore.CurrentCount < 1)
                    _trackpadSemaphore.Release();
            }
        }

        public void AppleTVTrackpadTouchRelease(short x, short y)
        {
            lock (_trackpadSemaphore)
            {
                _trackpadX = x;
                _trackpadY = y;
                _trackpadTouchDown = false;
                _trackpadUpdatedAt = DateTime.Now;

                if (_trackpadSemaphore.CurrentCount < 1)
                    _trackpadSemaphore.Release();
            }
        }

        private async void ConnectAppleTVTrackpadSocket()
        {
            // Cancel any previous connections
            var cancellationTokenSource = _trackpadConnectionCancellationTokenSource;
            if (cancellationTokenSource != null)
                cancellationTokenSource.Cancel();
            cancellationTokenSource = new CancellationTokenSource();
            _trackpadConnectionCancellationTokenSource = cancellationTokenSource;

            // Set up message template
            int[] message = new int[8];
            message[0] = 0x00000020;
            message[1] = 0x00010000;
            //message[2] = // Action
            message[3] = 0x00000000;
            message[4] = 0x0000000C;
            //message[5] = // Time offset
            message[6] = 0x00000001;
            //message[7] = // X/Y position

            // Make socket connection
            using (var socket = new StreamSocket())
            {
                try
                {
                    await socket.ConnectAsync(new HostName(Hostname), _appleTVTrackpadPort.ToString()).AsTask().ConfigureAwait(false);
                }
                catch
                {
                    var task = RequestAppleTVTrackpadInfoUpdateAsync();
                    return;
                }
                if (cancellationTokenSource.IsCancellationRequested)
                    return;

                using (var writer = new DataWriter(socket.OutputStream))
                {
                    DateTime startTime = DateTime.MinValue;
                    bool wasTouchDown = false;
                    byte[] encodedMessage;

                    // Write successive messages
                    while (true)
                    {
                        try
                        {
                            await _trackpadSemaphore.WaitAsync(cancellationTokenSource.Token).ConfigureAwait(false);
                        }
                        catch { }

                        if (cancellationTokenSource.IsCancellationRequested)
                            return;

                        lock (_trackpadSemaphore)
                        {
                            if (_trackpadTouchDown)
                            {
                                if (!wasTouchDown)
                                {
                                    message[2] = 0x00000100; // Touch down
                                    startTime = _trackpadUpdatedAt;
                                }
                                else
                                {
                                    message[2] = 0x00000101; // Touch move
                                }
                            }
                            else
                            {
                                if (!wasTouchDown)
                                    continue;
                                message[2] = 0x00000102; // Touch up
                            }
                            wasTouchDown = _trackpadTouchDown;
                            message[5] = (int)(_trackpadUpdatedAt - startTime).TotalMilliseconds;
                            message[7] = ((_trackpadX << 16) + _trackpadY);
                        }

                        encodedMessage = EncodeAppleTVTrackpadMessage(message);
                        writer.WriteBytes(encodedMessage);
                        await writer.StoreAsync().AsTask().ConfigureAwait(false);
                    }
                }
            }
        }

        // It's important to properly disconnect the virtual trackpad socket when exiting the application. If the socket is not closed when the application exits,
        // the OS will forcefully close the socket (by sending an RST packet) which (at this time) causes the Apple TV to become unresponsive to future trackpad
        // connection attempts. Closing the socket gracefully (with a FIN packet) prevents this issue (although it will trigger the Apple TV to begin using a new
        // port number and encryption key).
        public void DisconnectAppleTVTrackpadSocket()
        {
            var token = _trackpadConnectionCancellationTokenSource;
            if (token != null)
                token.Cancel();
        }

        private byte[] EncodeAppleTVTrackpadMessage(int[] message)
        {
            byte[] result = new byte[message.Length * 4];
            int encoded;
            int offset;
            byte[] bytes;
            for (int i = 0; i < message.Length; i++)
            {
                encoded = message[i] ^ _appleTVTrackpadKey;
                offset = i * 4;
                bytes = BitConverter.GetBytes(encoded);
                result[offset + 0] = bytes[3];
                result[offset + 1] = bytes[2];
                result[offset + 2] = bytes[1];
                result[offset + 3] = bytes[0];
            }

            return result;
        }

        #endregion
    }

    public enum AppleTVKeyboardType
    {
        Standard = 0,
        Email = 7,
    }
}
