using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace Komodex.CommonLibrariesTestApp.SampleData
{
    public partial class SampleDataTests : PhoneApplicationPage
    {
        public SampleDataTests()
        {
            InitializeComponent();
            DataContext = new SampleDataViewSource();
        }
    }
}