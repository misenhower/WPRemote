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

namespace Komodex.WP7DACPRemote.DACPServerInfoManagement
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

        private const string ItemsKey = "DACPServerList";
        private ObservableCollection<DACPServerInfo> _Items = null;
        public ObservableCollection<DACPServerInfo> Items
        {
            get
            {
                if (_Items == null)
                {
                    if (isolatedSettings.Contains(ItemsKey))
                        _Items = isolatedSettings[ItemsKey] as ObservableCollection<DACPServerInfo>;

                    if (_Items == null)
                    {
                        _Items = new ObservableCollection<DACPServerInfo>();
                        isolatedSettings[ItemsKey] = _Items;
                    }

                    _Items.CollectionChanged += Items_CollectionChanged;
                }

                return _Items;
            }
        }

        void Items_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            Save();
        }

        public DACPServerInfo CurrentDACPServer
        {
            get { return Items.FirstOrDefault(si => si.ID == SelectedServerGuid); }
        }

        private const string SelectedServerGuidKey = "SelectedServerGuid";
        private Guid _SelectedServerGuid = Guid.Empty;
        public Guid SelectedServerGuid
        {
            get
            {
                if (_SelectedServerGuid == Guid.Empty)
                    if (isolatedSettings.Contains(SelectedServerGuidKey))
                        if (isolatedSettings[SelectedServerGuidKey] is Guid)
                            _SelectedServerGuid = (Guid)isolatedSettings[SelectedServerGuidKey];

                return _SelectedServerGuid;
            }
            set
            {
                _SelectedServerGuid = value;
                isolatedSettings[SelectedServerGuidKey] = _SelectedServerGuid;
                Save();
            }
        }

        #endregion

        #region Methods

        public void Save()
        {
            isolatedSettings.Save();
        }

        #endregion

    }
}
