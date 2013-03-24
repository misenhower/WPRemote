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
using Microsoft.Phone.Info;

namespace Komodex.WP7DACPRemote.Utilities
{
#if DEBUG
    public static class DeviceInfo
    {
        private static string _DeviceManufacturer = null;
        public static string DeviceManufacturer
        {
            get
            {
                if (_DeviceManufacturer == null)
                {
                    object propertyValue;

                    if (DeviceExtendedProperties.TryGetValue("DeviceManufacturer", out propertyValue))
                        _DeviceManufacturer = (string)propertyValue;

                    if (_DeviceManufacturer == null)
                        _DeviceManufacturer = string.Empty;
                }

                return _DeviceManufacturer;
            }
        }

        private static string _DeviceName = null;
        public static string DeviceName
        {
            get
            {
                if (_DeviceName == null)
                {
                    object propertyValue;

                    if (DeviceExtendedProperties.TryGetValue("DeviceName", out propertyValue))
                        _DeviceName = (string)propertyValue;

                    if (_DeviceName == null)
                        _DeviceName = string.Empty;
                }

                return _DeviceName;
            }
        }

        private static string _DeviceFirmwareVersion = null;
        public static string DeviceFirmwareVersion
        {
            get
            {
                if (_DeviceFirmwareVersion == null)
                {
                    object propertyValue;

                    if (DeviceExtendedProperties.TryGetValue("DeviceFirmwareVersion", out propertyValue))
                        _DeviceFirmwareVersion = (string)propertyValue;

                    if (_DeviceFirmwareVersion == null)
                        _DeviceFirmwareVersion = string.Empty;
                }

                return _DeviceFirmwareVersion;
            }
        }

        private static string _DeviceHardwareVersion = null;
        public static string DeviceHardwareVersion
        {
            get
            {
                if (_DeviceHardwareVersion == null)
                {
                    object propertyValue;

                    if (DeviceExtendedProperties.TryGetValue("DeviceHardwareVersion", out propertyValue))
                        _DeviceHardwareVersion = (string)propertyValue;

                    if (_DeviceHardwareVersion == null)
                        _DeviceHardwareVersion = string.Empty;
                }

                return _DeviceHardwareVersion;
            }
        }
    }
#endif
}
