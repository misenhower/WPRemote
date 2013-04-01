using Komodex.Common;
using Komodex.Remote.DACPServerInfoManagement;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Komodex.Remote.Settings
{
    public static class SettingsUpdater
    {
        private const string AppSettingsFilename = "__ApplicationSettings";

        private static readonly Log _log = new Log("Settings Updater");

        private static readonly IsolatedStorageSettings _settings = IsolatedStorageSettings.ApplicationSettings;

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
                                SaveValue("SelectedServerGuid", Guid.Parse(value));
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
                    catch
                    {
                        _log.Error("Exception while processing key '{0}'", key);
                    }
                }

                _log.Info("Settings update complete.");
            }
            catch { }
        }

        private static void HandleServerList(XElement node)
        {
            // TODO: Will need to make sure this outputs a string of serialized server data rather than saving the object directly
            ObservableCollection<DACPServerInfo> servers = new ObservableCollection<DACPServerInfo>();

            var serverNodes = node.Descendants().Where(n => n.Name.LocalName == "DACPServerInfo");
            foreach (var serverNode in serverNodes)
            {
                DACPServerInfo serverInfo = new DACPServerInfo();
                var serverNodeDataNodes = serverNode.Descendants();
                foreach (var serverNodeDataNode in serverNodeDataNodes)
                {
                    var key = serverNodeDataNode.Name.LocalName;
                    var value = serverNodeDataNode.Value;

                    switch (key)
                    {
                        case "ID":
                            serverInfo.ID = Guid.Parse(value);
                            break;

                        case "ServiceID":
                            serverInfo.ServiceID = value;
                            break;

                        case "HostName":
                            serverInfo.HostName = value;
                            break;

                        case "LibraryName":
                            serverInfo.LibraryName=value;
                            break;

                        case "PIN":
                            serverInfo.PIN = int.Parse(value);
                            break;
                    }
                }

                servers.Add(serverInfo);
            }

            SaveValue("DACPServerList", servers);
        }


        private static void SaveValue(string key, object value)
        {
            _log.Info("Updating setting: '{0}' => '{1}'", key, value);
            _settings[key] = value;
            _settings.Save();
        }
    }
}
