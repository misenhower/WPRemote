using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Input;
using Windows.Graphics.Display;
using Windows.Security.ExchangeActiveSyncProvisioning;

namespace Komodex.Common
{
    public static class DeviceInfo
    {
        private static readonly EasClientDeviceInformation _easInfo = new EasClientDeviceInformation();

        public static string FriendlyName { get { return _easInfo.FriendlyName; } }
        public static string SystemManufacturer { get { return _easInfo.SystemManufacturer; } }
        public static string SystemProductName { get { return _easInfo.SystemProductName; } }
        public static string SystemSku { get { return _easInfo.SystemSku; } }

        public static DeviceType Type
        {
            get
            {
                switch (_easInfo.OperatingSystem.ToLower())
                {
                    case "windows":
                        return DeviceType.Windows;
                    case "windowsphone":
                        return DeviceType.WindowsPhone;
                }

                return DeviceType.Unknown;
            }
        }

        public static bool HasTouch
        {
            get { return (new TouchCapabilities().TouchPresent > 0); }
        }
    }

    public enum DeviceType
    {
        Unknown,
        Windows,
        WindowsPhone,
    }
}
