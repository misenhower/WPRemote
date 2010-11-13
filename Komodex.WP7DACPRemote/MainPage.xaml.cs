using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Komodex.WP7DACPRemote.DACPServerInfoManagement;
using Microsoft.Unsupported;

namespace Komodex.WP7DACPRemote
{
    public partial class MainPage : PhoneApplicationPage
    {
        // Constructor
        public MainPage()
        {
            InitializeComponent();


            TiltEffect.SetIsTiltEnabled(this, true);

            DACPServerViewModel vm = new DACPServerViewModel();
            DACPServerInfo si;
            si = new DACPServerInfo();
            si.LibraryName = "lib 1";
            vm.Items.Add(si);
            si = new DACPServerInfo();
            si.LibraryName = "lib 2";
            vm.Items.Add(si);
            si = new DACPServerInfo();
            si.LibraryName = "lib 3";
            vm.Items.Add(si);
            si = new DACPServerInfo();
            si.LibraryName = "lib 1";
            vm.Items.Add(si);
            si = new DACPServerInfo();
            si.LibraryName = "lib 2";
            vm.Items.Add(si);
            si = new DACPServerInfo();
            si.LibraryName = "lib 3";
            vm.Items.Add(si);
            si = new DACPServerInfo();
            si.LibraryName = "lib 1";
            vm.Items.Add(si);
            si = new DACPServerInfo();
            si.LibraryName = "lib 2";
            vm.Items.Add(si);
            si = new DACPServerInfo();
            si.LibraryName = "lib 3";
            vm.Items.Add(si);
            si = new DACPServerInfo();
            si.LibraryName = "lib 1";
            vm.Items.Add(si);
            si = new DACPServerInfo();
            si.LibraryName = "lib 2";
            vm.Items.Add(si);
            si = new DACPServerInfo();
            si.LibraryName = "lib 3";
            vm.Items.Add(si);
            si = new DACPServerInfo();
            si.LibraryName = "lib 1";
            vm.Items.Add(si);
            si = new DACPServerInfo();
            si.LibraryName = "lib 2";
            vm.Items.Add(si);
            si = new DACPServerInfo();
            si.LibraryName = "lib 3";
            vm.Items.Add(si);

            DataContext = vm;

        }
    }
}