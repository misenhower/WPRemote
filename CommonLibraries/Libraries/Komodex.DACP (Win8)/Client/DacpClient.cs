using Komodex.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Komodex.DACP
{
    public sealed partial class DacpClient : BindableBase
    {
        private readonly Log _log = new Log("DACP Client");

        #region Connection Configuration Properties

        private string _hostname;
        public string Hostname
        {
            get { return _hostname; }
            set
            {
                if (_hostname == value)
                    return;
                _hostname = value;
                UpdateHttpClient();
                SendPropertyChanged();
            }
        }

        private int _port = 3689;
        public int Port
        {
            get { return _port; }
            set
            {
                if (_port == value)
                    return;
                _port = value;
                UpdateHttpClient();
                SendPropertyChanged();
            }
        }

        private string _pairingCode;
        public string PairingCode
        {
            get { return _pairingCode; }
            set
            {
                if (_pairingCode == value)
                    return;
                _pairingCode = value;
                SendPropertyChanged();
            }
        }

        #endregion

        #region Other Connection Properties

        private string _httpPrefix;
        internal string HttpPrefix
        {
            get { return _httpPrefix; }
            private set
            {
                if (_httpPrefix == value)
                    return;
                _httpPrefix = value;
                SendPropertyChanged();
            }
        }

        private int _sessionID;
        internal int SessionID
        {
            get { return _sessionID; }
            private set
            {
                if (_sessionID == value)
                    return;
                _sessionID = value;
                SendPropertyChanged();
                // TODO: Update current album art link
            }
        }

        private int _currentLibraryUpdateNumber;
        internal int CurrentLibraryUpdateNumber
        {
            get { return _currentLibraryUpdateNumber; }
            private set
            {
                if (_currentLibraryUpdateNumber == value)
                    return;
                _currentLibraryUpdateNumber = value;
                SendPropertyChanged();
            }
        }

        #endregion

        #region Connection Methods

        public Task<ConnectionResult> ConnectAsync()
        {
            return ConnectAsync(CancellationToken.None);
        }

        public async Task<ConnectionResult> ConnectAsync(CancellationToken cancellationToken)
        {
            bool success;

            _log.Info("Connecting to server...");

            // Server info
            _log.Info("Connecting: Server info");
            success = await GetServerInfoAsync().ConfigureAwait(false);
            if (!success || cancellationToken.IsCancellationRequested)
                return ConnectionResult.ConnectionError;

            // Server capabilities
            _log.Info("Connecting: Server capabilities");
            success = await GetServerCapabilitiesAsync().ConfigureAwait(false);
            if (!success || cancellationToken.IsCancellationRequested)
                return ConnectionResult.ConnectionError;

            // Login
            _log.Info("Connecting: Login");
            success = await LoginAsync().ConfigureAwait(false);
            if (cancellationToken.IsCancellationRequested)
                return ConnectionResult.ConnectionError;
            if (!success)
                return ConnectionResult.InvalidPIN;

            // Databases
            _log.Info("Connecting: Databases");
            success = await GetDatabasesAsync().ConfigureAwait(false);
            if (!success || cancellationToken.IsCancellationRequested)
                return ConnectionResult.ConnectionError;

            // Library update version
            _log.Info("Connecting: Library update version");
            success = await GetFirstLibraryUpdateAsync().ConfigureAwait(false);
            if (!success || cancellationToken.IsCancellationRequested)
                return ConnectionResult.ConnectionError;

            // Play status
            _log.Info("Connecting: Initial play status");
            success = await GetFirstPlayStatusUpdateAsync().ConfigureAwait(false);
            if (!success || cancellationToken.IsCancellationRequested)
                return ConnectionResult.ConnectionError;

            _log.Info("Connecting: Complete!");

            IsConnected = true;

            return ConnectionResult.Success;
        }

        public void StartUpdateRequests()
        {
            SubscribeToLibraryUpdates();
            SubscribeToPlayStatusUpdates();
        }

        public void Disconnect()
        {
            if (IsConnected)
                ServerDisconnected.RaiseOnUIThread(this, new EventArgs());

            IsConnected = false;
            // TODO: Cancel library and play status updates
        }

        #endregion

        #region Connection Errors

        public event EventHandler<EventArgs> ServerDisconnected;

        private void HandleConnectionError()
        {
            Disconnect();
        }

        #endregion

        #region Play Status Updated

        public event EventHandler<EventArgs> PlayStatusUpdated;

        #endregion
    }
}
