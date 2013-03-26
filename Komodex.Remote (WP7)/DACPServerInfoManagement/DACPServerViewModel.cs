using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO.IsolatedStorage;
using System.Linq;
using Komodex.Common;

namespace Komodex.Remote.DACPServerInfoManagement
{
    public class DACPServerViewModel
    {
        private static IsolatedStorageSettings isolatedSettings = IsolatedStorageSettings.ApplicationSettings;


        private static DACPServerViewModel _Instance = null;
        public static DACPServerViewModel Instance
        {
            get
            {
                if (_Instance == null)
                    _Instance = new DACPServerViewModel();
                return _Instance;
            }
        }

        protected DACPServerViewModel() { }

        #region Properties

        private readonly Setting<ObservableCollection<DACPServerInfo>> _items = new Setting<ObservableCollection<DACPServerInfo>>("DACPServerList", new ObservableCollection<DACPServerInfo>());
        public ObservableCollection<DACPServerInfo> Items
        {
            get { return _items.Value; }
        }

        private readonly Setting<Guid> _selectedServerGuid = new Setting<Guid>("SelectedServerGuid");
        public Guid SelectedServerGuid
        {
            get { return _selectedServerGuid.Value; }
            set
            {
                if (_selectedServerGuid.Value == value)
                    return;

                _selectedServerGuid.Value = value;
            }
        }

        public DACPServerInfo CurrentDACPServer
        {
            get { return Items.FirstOrDefault(si => si.ID == SelectedServerGuid); }
        }

        #endregion

        #region Methods

        public void Save()
        {
            _items.Save();
        }

        #endregion

    }
}
