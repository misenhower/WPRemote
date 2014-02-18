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

        private int _volume;
        public int Volume
        {
            get { return _volume; }
            private set
            {
                if (_volume == value)
                    return;
                _volume = value;
                SendPropertyChanged();
            }
        }

        public void ProcessNodes(DacpNodeDictionary nodes)
        {
            ID = (UInt64)nodes.GetLong("msma");
            Name = nodes.GetString("minm");
            IsActive = nodes.GetBool("caia");
            HasVideo = nodes.GetBool("caiv");
            HasPassword = nodes.GetBool("cahp");
            Volume = nodes.GetInt("cmvo");
        }

        protected virtual string DebuggerDisplay
        {
            get { return string.Format("Speaker Name: \"{0}\", Active: {1}, Volume: {2}", Name, IsActive, Volume); }
        }
    }
}
