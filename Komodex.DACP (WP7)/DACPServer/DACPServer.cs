﻿using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Komodex.Common;
using System.Threading;
using System.Windows.Threading;
using System.Threading.Tasks;

namespace Komodex.DACP
{
    public partial class DACPServer
    {
        private static readonly Log _log = new Log("DACP");

        private DACPServer()
        {
            Utility.BeginInvokeOnUIThread(() =>
            {
                timerTrackTimeUpdate = new DispatcherTimer();
                timerTrackTimeUpdate.Interval = TimeSpan.FromSeconds(1);
                timerTrackTimeUpdate.Tick += timerTrackTimeUpdate_Tick;
            });
        }

        public DACPServer(string hostname, int port, string pairingCode)
            : this()
        {
            string assemblyName = System.Reflection.Assembly.GetCallingAssembly().FullName;
            if (!assemblyName.StartsWith("Komodex.Remote,"))
                throw new Exception();

            _hostname = hostname;
            _port = port;
            UpdateHTTPPrefix();
            PairingCode = pairingCode;
        }

        public static string GetAssemblyName()
        {
            return System.Reflection.Assembly.GetExecutingAssembly().FullName;
        }

        #region Fields

        internal string HTTPPrefix;

        #endregion

        #region Properties

        private string _hostname;
        public string Hostname
        {
            get { return _hostname; }
            set
            {
                if (_hostname == value)
                    return;

                _hostname = value;
                UpdateHTTPPrefix();
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
                UpdateHTTPPrefix();
            }
        }

        protected void UpdateHTTPPrefix()
        {
            HTTPPrefix = "http://" + Hostname + ":" + Port;
            UpdateHttpClient();
        }

        public string PairingCode { get; protected set; }

        private int _sessionID;
        public int SessionID
        {
            get { return _sessionID; }
            protected set
            {
                if (_sessionID == value)
                    return;
                _sessionID = value;
                int pixels = ResolutionUtility.GetScaledPixels(284);
                CurrentAlbumArtURL = HTTPPrefix + "/ctrl-int/1/nowplayingartwork?mw=" + pixels + "&mh=" + pixels + "&session-id=" + SessionID;
            }
        }

        #endregion

        #region Connection Management

        public Task<ConnectionResult> ConnectAsync()
        {
            return ConnectAsync(CancellationToken.None);
        }

        public async Task<ConnectionResult> ConnectAsync(CancellationToken cancellationToken)
        {
            bool success;

            // Server info
            success = await GetServerInfoAsync().ConfigureAwait(false);
            if (!success || cancellationToken.IsCancellationRequested)
                return ConnectionResult.ConnectionError;

            // Server capabilities
            success = await GetServerCapabilitiesAsync().ConfigureAwait(false);
            if (!success || cancellationToken.IsCancellationRequested)
                return ConnectionResult.ConnectionError;

            // Login
            success = await LoginAsync().ConfigureAwait(false);
            if (cancellationToken.IsCancellationRequested)
                return ConnectionResult.ConnectionError;
            if (!success)
                return ConnectionResult.InvalidPIN;

            // Databases
            success = await GetDatabasesAsync().ConfigureAwait(false);
            if (!success || cancellationToken.IsCancellationRequested)
                return ConnectionResult.ConnectionError;

            // Library version
            success = await GetFirstLibraryUpdateAsync().ConfigureAwait(false);
            if (!success || cancellationToken.IsCancellationRequested)
                return ConnectionResult.ConnectionError;

            // Play status
            success = await GetFirstPlayStatusUpdateAsync().ConfigureAwait(false);
            if (!success || cancellationToken.IsCancellationRequested)
                return ConnectionResult.ConnectionError;

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
            IsConnected = false;
            if (_currentLibraryUpdateCancellationTokenSource != null)
                _currentLibraryUpdateCancellationTokenSource.Cancel();
            if (_currentPlayStatusCancellationTokenSource != null)
                _currentPlayStatusCancellationTokenSource.Cancel();
            if (_currentRepeatedTrackTimeRequestCancellationTokenSource != null)
                _currentRepeatedTrackTimeRequestCancellationTokenSource.Cancel();
        }

        #endregion


        #region Connection State

        internal void HandleHTTPException(DACPRequest request, Exception e)
        {
            HandleHTTPException(request.GetURI(), e);
        }

        internal void HandleHTTPException(string uri, Exception e)
        {
            _log.Error("HTTP Exception for URI: " + uri);
            _log.Debug("Exception details: " + e.ToString());
            //ConnectionError("HTTP Exception:\nURI: " + uri + "\n" + e.ToString());
        }

        protected void HandleConnectionError(string errorDetails)
        {
            //ConnectionError(ServerErrorType.General, errorDetails);
        }

        protected void HandleConnectionError(ServerErrorType errorType = ServerErrorType.General, string errorDetails = null)
        {
            IsConnected = false;

            //if (!Stopped)
            {
                Disconnect();
                //SendServerUpdate(ServerUpdateType.Error, errorType, errorDetails);
            }
        }

        #endregion

        #region Events

        public event EventHandler<EventArgs> ConnectionError;

        protected void SendConnectionError()
        {
            if (!IsConnected)
                return;

            Disconnect();
            if (ConnectionError != null)
                ConnectionError.Raise(this, new EventArgs());
        }

        public event EventHandler<EventArgs> LibraryUpdate;

        protected void SendLibraryUpdate()
        {
            if (!IsConnected)
                return;

            if (LibraryUpdate != null)
                LibraryUpdate.Raise(this, new EventArgs());
        }

        #endregion

    }
}
