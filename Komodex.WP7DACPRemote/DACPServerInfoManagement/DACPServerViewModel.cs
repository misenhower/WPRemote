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

                    _Items.CollectionChanged+=new NotifyCollectionChangedEventHandler(Items_CollectionChanged);
                }

                
                return _Items;
            }
        }

        void Items_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            Save();
        }

        private Guid _SelectedServerGUID = Guid.Empty;
        public Guid SelectedServerGuid
        {
            get { return _SelectedServerGUID; }
            set
            {
                _SelectedServerGUID = value;
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
