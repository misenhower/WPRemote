using Komodex.Common;
using Komodex.Remote.ServerManagement;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace Komodex.Remote.Settings
{
    public static class SettingsUpdater
    {
        private const string AppSettingsFilename = "__ApplicationSettings";

        private static readonly Log _log = new Log("Settings Updater");

        private static readonly IsolatedStorageSettings _settings = IsolatedStorageSettings.ApplicationSettings;

        private static readonly Dictionary<Guid, string> _discoveredServerIDs = new Dictionary<Guid, string>();
        private static Guid _selectedServerID;

        public static void CheckForUpdate()
        {
            try
            {
                // Determine whether application settings can be loaded
                if (_settings.Count > 0)
                    return;

                _log.Info("No app settings found. Looking for existing settings file...");

                string appSettings;

                // Attempt to load settings file
                using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if (!store.FileExists(AppSettingsFilename))
                        return;
                    using (var fileStream = new IsolatedStorageFileStream(AppSettingsFilename, FileMode.Open, store))
                    using (var reader = new StreamReader(fileStream))
                    {
                        appSettings = reader.ReadToEnd();
                    }
                }

                if (string.IsNullOrWhiteSpace(appSettings))
                    return;

                // The actual XML content starts on the second line of the file
                int start = appSettings.IndexOf("\n");
                if (start <= 0)
                    return;
                appSettings = appSettings.Substring(start);

                _log.Info("Existing settings found. Attempting update...");

                // Parse the XML...
                XDocument document = XDocument.Parse(appSettings);
                // Get item nodes
                var items = document.Descendants().Where(n => n.Name.LocalName == "KeyValueOfstringanyType");
                foreach (var item in items)
                {
                    // Get key
                    var keyNode = item.Descendants().Where(n => n.Name.LocalName == "Key").FirstOrDefault();
                    if (keyNode == null)
                        return;
                    string key = keyNode.Value;

                    // Get value
                    var valueNode = item.Descendants().Where(n => n.Name.LocalName == "Value").FirstOrDefault();
                    if (valueNode == null)
                        return;
                    string value = valueNode.Value;

                    // Process node
                    try
                    {
                        switch (key)
                        {
                            case "DACPServerList":
                                HandleServerList(valueNode);
                                break;

                            case "SelectedServerGuid":
                                _selectedServerID = Guid.Parse(value);
                                break;

                            case "SettingsRunUnderLock":
                                SaveValue("SettingsRunUnderLock", bool.Parse(value));
                                break;

                            case "SettingsArtistClickAction":
                                SaveValue("SettingsArtistClickAction", "<ArtistClickAction>" + value + "</ArtistClickAction>");
                                break;

                            case "SettingsExtendedErrorReporting":
                                SaveValue("SettingsExtendedErrorReporting", bool.Parse(value));
                                break;

                            case "FirstRunNotificationToBeSent":
                                SaveValue("FirstRunNotificationToBeSent", value);
                                break;

                            case "FirstRunCompleted":
                                if (value.ToLower() == "true")
                                    value = "1.0.0.1";
                                SaveValue("FirstRunCompleted", value);
                                break;

                            case "FirstRunTrialMode":
                                SaveValue("FirstRunTrialMode", bool.Parse(value));
                                break;

                            case "FirstRunDate":
                                SaveValue("FirstRunDate", DateTime.Parse(value));
                                break;
                        }
                    }
                    catch (Exception e)
                    {
                        _log.Error("Exception while processing key '{0}'", key);
                        _log.Debug("Exception details: " + e.ToString());
                    }
                }

                if (_selectedServerID != Guid.Empty && _discoveredServerIDs.ContainsKey(_selectedServerID))
                    SaveValue("SelectedServerID", _discoveredServerIDs[_selectedServerID]);

                _log.Info("Settings update complete.");
            }
            catch { }
        }

        private static void HandleServerList(XElement node)
        {
            ServerConnectionInfoCollection servers = new ServerConnectionInfoCollection();

            var serverNodes = node.Descendants().Where(n => n.Name.LocalName == "DACPServerInfo");
            foreach (var serverNode in serverNodes)
            {
                ServerConnectionInfo info = new ServerConnectionInfo();
                Guid id = Guid.Empty;

                var serverNodeDataNodes = serverNode.Descendants();
                foreach (var serverNodeDataNode in serverNodeDataNodes)
                {
                    var key = serverNodeDataNode.Name.LocalName;
                    var value = serverNodeDataNode.Value;

                    switch (key)
                    {
                        case "ID":
                            id = Guid.Parse(value);
                            break;

                        case "ServiceID":
                            info.ServiceID = value;
                            break;

                        case "HostName":
                            // Look for a specified port
                            int port = 3689;
                            string[] hostParts = value.Split(':');
                            if (hostParts.Length > 1)
                                port = int.Parse(hostParts[1]);
                            info.LastPort = port;
                            // Determine whether this is an IP address or a hostname
                            IPAddress ip;
                            if (IPAddress.TryParse(hostParts[0], out ip))
                                info.LastIPAddress = ip.ToString();
                            info.LastHostname = hostParts[0];
                            break;

                        case "LibraryName":
                            info.Name = value;
                            break;

                        case "PIN":
                            int pin = int.Parse(value);
                            info.PairingCode = string.Format("{0:0000}{0:0000}{0:0000}{0:0000}", pin);
                            break;
                    }
                }

                // Check to see whether we have all the necessary parts
                if (string.IsNullOrEmpty(info.ServiceID) || string.IsNullOrEmpty(info.LastHostname) || string.IsNullOrEmpty(info.PairingCode))
                    continue;

                // Store the old GUID for later
                _discoveredServerIDs[id] = info.ServiceID;

                // Make sure we don't have any duplicates
                var oldInfo = servers.FirstOrDefault(s => s.ServiceID == info.ServiceID);
                if (oldInfo != null)
                    servers.Remove(oldInfo);

                // Add the server to the list
                servers.Add(info);
            }

            if (servers.Count > 0)
            {
                XmlSerializer xml = new XmlSerializer(typeof(ServerConnectionInfoCollection));
                string serversXML = xml.SerializeToString(servers);
                SaveValue("PairedServerList", serversXML);
            }
        }


        private static void SaveValue(string key, object value)
        {
            _log.Info("Updating setting: '{0}' => '{1}'", key, value);
            _settings[key] = value;
            _settings.Save();
        }
    }
}
