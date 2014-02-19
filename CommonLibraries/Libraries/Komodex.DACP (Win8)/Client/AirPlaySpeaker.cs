using Komodex.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Komodex.DACP
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class AirPlaySpeaker : BindableBase
    {
        public AirPlaySpeaker(DacpClient client, DacpNodeDictionary nodes)
        {
            _client = client;
            ProcessNodes(nodes);
        }

        private readonly DacpClient _client;
        public DacpClient Client { get { return _client; } }

        public UInt64 ID { get; private set; }

        private string _name;
        public string Name
        {
            get { return _name; }
            private set
            {
                if (_name == value)
                    return;
                _name = value;
                SendPropertyChanged();
            }
        }

        private bool _isActive;
        public bool IsActive
        {
            get { return _isActive; }
            private set
            {
                if (_isActive == value)
                    return;
                _isActive = value;
                SendPropertyChanged();
            }
        }

        private bool _hasVideo;
        public bool HasVideo
        {
            get { return _hasVideo; }
            private set
            {
                if (_hasVideo == value)
                    return;
                _hasVideo = value;
                SendPropertyChanged();
            }
        }

        private bool _hasPassword;
        public bool HasPassword
        {
            get { return _hasPassword; }
            private set
            {
                if (_hasPassword == value)
                    return;
                _hasPassword = value;
                SendPropertyChanged();
            }
        }

        private int _volumeLevel;
        public int VolumeLevel
        {
            get { return _volumeLevel; }
            private set
            {
                if (_volumeLevel == value)
                    return;
                _volumeLevel = value;
                SendPropertyChanged();
            }
        }

        private double _bindableVolumeLevel;
        public double BindableVolumeLevel
        {
            get { return _bindableVolumeLevel; }
            set { UpdateBoundVolumeLevel(value); }
        }

        private void SendBindableVolumeLevelChanged()
        {
            if (!_updatingBoundVolumeLevel)
                SendPropertyChanged("BindableVolumeLevel");
        }

        private int _newBoundVolumeLevel;
        private bool _newBoundVolumeReplacesMasterVolume;
        private bool _updatingBoundVolumeLevel;

        private async void UpdateBoundVolumeLevel(double volumeLevel)
        {
            // Determine the adjusted volume level
            var masterVolumeLevel = Client.CurrentVolumeLevel;
            // This adjustment should replace the master volume level if it is higher than the current volume level or higher than all other active speakers
            if (volumeLevel > masterVolumeLevel || !Client.Speakers.Any(s => s != this && s.IsActive && s.BindableVolumeLevel > volumeLevel))
            {
                _newBoundVolumeLevel = (int)volumeLevel;
                _newBoundVolumeReplacesMasterVolume = true;
            }
            else
            {
                _newBoundVolumeLevel = (int)(100 * volumeLevel / masterVolumeLevel);
                _newBoundVolumeReplacesMasterVolume = false;
            }

            if (_updatingBoundVolumeLevel)
                return;

            _updatingBoundVolumeLevel = true;

            bool success;
            int value;
            bool replaceMaster;

            do
            {
                value = _newBoundVolumeLevel;
                replaceMaster = _newBoundVolumeReplacesMasterVolume;
                success = await SetVolumeLevelAsync(value, replaceMaster).ConfigureAwait(false);
            } while (success && (value != _newBoundVolumeLevel || replaceMaster != _newBoundVolumeReplacesMasterVolume));

            _newBoundVolumeLevel = -1;
            _updatingBoundVolumeLevel = false;
        }

        private async Task<bool> SetVolumeLevelAsync(int value, bool replaceMasterVolume)
        {
            DacpRequest request = new DacpRequest("/ctrl-int/1/setproperty");
            request.QueryParameters["dmcp.volume"] = value.ToString();
            if (replaceMasterVolume)
                request.QueryParameters["include-speaker-id"] = ID.ToString();
            else
                request.QueryParameters["speaker-id"] = ID.ToString();

            try { await Client.SendRequestAsync(request).ConfigureAwait(false); }
            catch { return false; }
            return true;
        }

        public void ProcessNodes(DacpNodeDictionary nodes)
        {
            ID = (UInt64)nodes.GetLong("msma");
            Name = nodes.GetString("minm");
            IsActive = nodes.GetBool("caia");
            HasVideo = nodes.GetBool("caiv");
            HasPassword = nodes.GetBool("cahp");
            int volumeLevel = nodes.GetInt("cmvo");
            VolumeLevel = volumeLevel;

            // Set the adjusted volume level
            _bindableVolumeLevel = ((double)Client.CurrentVolumeLevel / 100) * volumeLevel;
            SendBindableVolumeLevelChanged();
        }

        protected virtual string DebuggerDisplay
        {
            get { return string.Format("Speaker Name: \"{0}\", Active: {1}, Volume: {2}", Name, IsActive, VolumeLevel); }
        }
    }
}
