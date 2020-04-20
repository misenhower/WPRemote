using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Komodex.Common
{
    public abstract class BindableBase : INotifyPropertyChanged
    {
        protected void SendPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (propertyName == null)
                throw new ArgumentNullException("propertyName");

            PropertyChanged.RaiseOnUIThread(this, propertyName);
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
