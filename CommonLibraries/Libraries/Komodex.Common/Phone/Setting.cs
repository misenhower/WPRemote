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
using System.IO.IsolatedStorage;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Komodex.Common.Phone
{
    public class Setting<T>
    {
        private static IsolatedStorageSettings _isolatedSettings = IsolatedStorageSettings.ApplicationSettings;

        public Setting(string keyName, T defaultValue = default(T), Action<T> changeAction = null)
        {
            _keyName = keyName;
            _changeAction = changeAction;

            // Try to load the value from isolated storage, or use the default value
            // TryGetValue<T> will throw an exception if the value in isolated storage is of a different type
            bool valueLoaded = false;
            try
            {
                valueLoaded = _isolatedSettings.TryGetValue<T>(_keyName, out _value);
            }
            catch { }
            if (valueLoaded)
                AttachValueEvents();
            else
                Value = defaultValue; // This will save the default value to isolated storage as well

            // If an action was specified, run it on the initial value
            if (_changeAction != null)
                _changeAction(_value);
        }

        protected string _keyName;
        protected T _value;
        protected Action<T> _changeAction;

        protected void AttachValueEvents()
        {
            if (_value is INotifyPropertyChanged)
                ((INotifyPropertyChanged)_value).PropertyChanged += new PropertyChangedEventHandler(Value_PropertyChanged);
            if (_value is INotifyCollectionChanged)
                ((INotifyCollectionChanged)_value).CollectionChanged += new NotifyCollectionChangedEventHandler(Value_CollectionChanged);
        }

        protected void DetatchValueEvents()
        {
            if (_value is INotifyPropertyChanged)
                ((INotifyPropertyChanged)_value).PropertyChanged -= new PropertyChangedEventHandler(Value_PropertyChanged);
            if (_value is INotifyCollectionChanged)
                ((INotifyCollectionChanged)_value).CollectionChanged -= new NotifyCollectionChangedEventHandler(Value_CollectionChanged);
        }

        public T Value
        {
            get { return _value; }
            set
            {
                DetatchValueEvents();

                _value = value;

                AttachValueEvents();

                // Save the new value to isolated storage
                _isolatedSettings[_keyName] = _value;
                Save();
            }
        }

        /// <summary>
        /// It is typically not necessary to call this method since it is automatically called when the value
        /// is changed or when T implements INotifyPropertyChanged or INotifyCollectionChanged. It is only
        /// necessary to call this method when using types that do not implement these interfaces, such as
        /// Dictionary&lt;TKey, TValue&gt;.
        /// </summary>
        public void Save()
        {
            // Update isolated storage
            _isolatedSettings.Save();

            // Run modified action
            if (_changeAction != null)
                _changeAction(_value);
        }

        protected void Value_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Save();
        }

        protected void Value_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Save();
        }
    }
}
