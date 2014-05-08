using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Xml.Serialization;
using Windows.ApplicationModel;
using Windows.Storage;

namespace Komodex.Common
{
    public class Setting<T>
    {
        private static Log _log = new Log("Settings");

        protected string _keyName;
        protected T _value;
        protected Action<T> _changeAction;

        private readonly bool _shouldSerializeValue;
        private readonly XmlSerializer _xmlSerializer;

        // This is a list of types that do not need to be serialized before storing in application settings
        private static readonly List<Type> _baseDataTypes = new List<Type>()
        { 
            typeof(string),
            typeof(int),
            typeof(bool),
            typeof(DateTimeOffset),
            typeof(Guid),
        };

        private static ApplicationDataContainer _localSettings = ApplicationData.Current.LocalSettings;

        static Setting()
        {
            if (DesignMode.DesignModeEnabled)
                return;
        }

        public Setting(string keyName, T defaultValue = default(T), Action<T> changeAction = null)
        {
            _keyName = keyName;
            _changeAction = changeAction;


            // Determine whether this value type should be serialized to XML
            if (!_baseDataTypes.Contains(typeof(T)))
            {
                _shouldSerializeValue = true;
                _xmlSerializer = new XmlSerializer(typeof(T));
            }

            // Try to load the value from isolated storage
            bool valueLoaded = false;

            valueLoaded = _localSettings.Values.TryGetValue<T>(_keyName, out _value);

            // If we couldn't load the value directly, try to deserialize it
            if (!valueLoaded && _shouldSerializeValue)
            {
                string value;
                if (_localSettings.Values.TryGetValue<string>(_keyName, out value))
                    valueLoaded = _xmlSerializer.TryDeserialize<T>(value, out _value);
            }

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
                ((INotifyPropertyChanged)_value).PropertyChanged += Value_PropertyChanged;
            if (_value is INotifyCollectionChanged)
                ((INotifyCollectionChanged)_value).CollectionChanged += Value_CollectionChanged;
        }

        protected void DetatchValueEvents()
        {
            if (_value is INotifyPropertyChanged)
                ((INotifyPropertyChanged)_value).PropertyChanged -= Value_PropertyChanged;
            if (_value is INotifyCollectionChanged)
                ((INotifyCollectionChanged)_value).CollectionChanged -= Value_CollectionChanged;
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
            if (DesignMode.DesignModeEnabled)
                return;

            // Update isolated storage
            object value;
            if (_shouldSerializeValue)
                value = _xmlSerializer.SerializeToString(_value);
            else
                value = _value;


            _localSettings.Values[_keyName] = value;

            // Run modified action
            if (_changeAction != null)
                _changeAction(_value);

            _log.Debug("Wrote value for key '{0}'", _keyName);
            _log.Trace("Updated value: '{0}' => {1}", _keyName, value);
        }

        protected void Value_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            _log.Trace("Saving triggered by property '{0}' changing.", e.PropertyName);
            Save();
        }

        protected void Value_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            _log.Trace("Saving triggered by collection changing. Action: " + e.Action);
            Save();
        }
    }
}
