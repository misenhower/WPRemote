using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Xml.Serialization;

namespace Komodex.Common
{
    public class Setting<T>
    {
        private static Log.LogInstance _log = new Log.LogInstance("Settings");

        protected string _keyName;
        protected T _value;
        protected Action<T> _changeAction;

#if NETFX_CORE
        private bool _shouldSerializeValue;
        private XmlSerializer _xmlSerializer;

        // This is a list of types that do not need to be serialized before storing in application settings
        private static readonly List<Type> _baseDataTypes = new List<Type>()
        { 
            typeof(string),
            typeof(int),
            typeof(bool),
            typeof(DateTimeOffset),
            typeof(Guid),
        };
#endif

#if WINDOWS_PHONE
        private static System.IO.IsolatedStorage.IsolatedStorageSettings _isolatedSettings = System.IO.IsolatedStorage.IsolatedStorageSettings.ApplicationSettings;
#else
        private static Windows.Storage.ApplicationDataContainer _localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
#endif

        public Setting(string keyName, T defaultValue = default(T), Action<T> changeAction = null)
        {
            _keyName = keyName;
            _changeAction = changeAction;

            bool valueLoaded = false;

#if WINDOWS_PHONE
            // Try to load the value from isolated storage, or use the default value
            // TryGetValue<T> will throw an exception if the value in isolated storage is of a different type
            try
            {
                valueLoaded = _isolatedSettings.TryGetValue<T>(_keyName, out _value);
            }
            catch { }
#else
            // Determine whether this value type should be serialized to XML
            if (!_baseDataTypes.Contains(typeof(T)))
            {
                _shouldSerializeValue = true;
                _xmlSerializer = new XmlSerializer(typeof(T));
            }

            // Try to load the value from isolated storage
            if (_shouldSerializeValue)
            {
                string value;
                if (_localSettings.Values.TryGetValue<string>(_keyName, out value))
                    valueLoaded = _xmlSerializer.TryDeserialize<T>(value, out _value);
            }
            else
                valueLoaded = _localSettings.Values.TryGetValue<T>(_keyName, out _value);
#endif

            if (valueLoaded)
            {
                AttachValueEvents();
                _log.Debug("Loaded {0} from isolated storage key '{1}'", typeof(T).Name, _keyName);
            }
            else
            {
                _log.Debug("Initializing default value for settings key '{1}' (type: {0})", typeof(T).Name, _keyName);
                Value = defaultValue; // This will save the default value to isolated storage as well            
            }

            // If an action was specified, run it on the initial value
            if (_changeAction != null)
                _changeAction(_value);
        }

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
#if WINDOWS_PHONE
            _isolatedSettings[_keyName] = _value;
            _isolatedSettings.Save();
#else
            if (_shouldSerializeValue)
                _localSettings.Values[_keyName] = _xmlSerializer.SerializeToString(_value);
            else
                _localSettings.Values[_keyName] = _value;
#endif

            // Run modified action
            if (_changeAction != null)
                _changeAction(_value);

            _log.Debug("Wrote value for key '{0}'", _keyName);
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
