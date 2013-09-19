using Komodex.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;

namespace Komodex.Remote.Marketplace
{
    public static class ArtistBackgroundImageManager
    {
        private static readonly Log _log = new Log("ArtistBackgroundImageManager");

        private const string Locale = "en-US"; // TODO: Load from environment variables?

        private static readonly TimeSpan CacheExpiryTime = TimeSpan.FromDays(14);

        private const string MarketplaceSearchURIFormat = "http://catalog.zune.net/v3.2/{0}/music/{1}/?chunksize=20&isActionable=true&q={2}";
        private const string MarketplaceArtistBackgroundURIFormat = "http://image.catalog.zune.net/v3.2/{0}/music/artist/{1}/primaryimage?height={2}&contenttype=image/jpeg&resize=true";
        private const string ArtistBackgroundImageCacheDirectory = "/ArtistBackgroundImageCache";

        private static readonly List<string> _processingArtistNames = new List<string>();
        private static readonly List<string> _processingArtistIDs = new List<string>();
        private static readonly Setting<List<CachedArtistID>> _artistIDCache = new Setting<List<CachedArtistID>>("MarketplaceArtistIDCache", new List<CachedArtistID>());
        private static readonly Dictionary<string, string> _artistIDs = new Dictionary<string, string>();

        static ArtistBackgroundImageManager()
        {
            // Load the cached artist IDs and remove out-of-date IDs
            var cachedArtistIDs = _artistIDCache.Value;
            DateTime cacheCutoff = DateTime.Now - CacheExpiryTime;
            for (int i = 0; i < cachedArtistIDs.Count; i++)
            {
                var cachedID = cachedArtistIDs[i];
                if (cachedID.Date <= cacheCutoff)
                {
                    _log.Trace("Removing out-of-date artist ID for artist '{0}', ID '{1}' (cache date {2})", cachedID.Name, cachedID.ID, cachedID.Date);
                    cachedArtistIDs.RemoveAt(i);
                    i--;
                    continue;
                }

                _log.Trace("Found cached ID for artist '{0}', ID '{1}' (cache date {2})", cachedID.Name, cachedID.ID, cachedID.Date);
                _artistIDs[cachedID.Name] = cachedID.ID;
            }

            // Save any changes
            _artistIDCache.Save();
        }

        #region Properties

        public static string CurrentArtistName { get; private set; }
        public static ImageSource CurrentArtistImageSource { get; private set; }

        #endregion

        #region Events

        public static event EventHandler<ArtistBackgroundImageSourceUpdatedEventArgs> CurrentArtistImageSourceUpdated;

        private static void SendImageSourceUpdated()
        {
            CurrentArtistImageSourceUpdated.RaiseOnUIThread(null, new ArtistBackgroundImageSourceUpdatedEventArgs(CurrentArtistImageSource));
        }

        #endregion

        public static async void SetArtistName(string artistName)
        {
            if (artistName == CurrentArtistName)
                return;

            CurrentArtistName = artistName;
            CurrentArtistImageSource = null;

            // If the artist name is null, the Now Playing page will be exiting soon anyway, so don't change the background.
            if (artistName == null)
                return;

            if (string.IsNullOrWhiteSpace(artistName))
            {
                SendImageSourceUpdated();
                return;
            }

            artistName = artistName.Trim();

            // Are we looking for this artist already?
            lock (_processingArtistNames)
            {
                if (_processingArtistNames.Contains(artistName))
                {
                    _log.Info("Already searching for artist '{0}', exiting...", artistName);
                    return;
                }
                _processingArtistNames.Add(artistName);
            }

            // Get the artist ID
            string artistID = await GetArtistID(artistName);

            lock (_processingArtistNames)
                _processingArtistNames.Remove(artistName);

            if (artistID == null)
            {
                _log.Warning("Could not determine ID for artist '{0}'.", artistName);
                if (artistName == CurrentArtistName)
                    SendImageSourceUpdated();
                return;
            }

            _log.Info("Artist '{0}' ID: '{1}'", artistName, artistID);

            // Are we downloading this artist's image already?
            lock (_processingArtistIDs)
            {
                if (_processingArtistIDs.Contains(artistID))
                {
                    _log.Info("Already downloading image for artist ID '{0}', exiting...", artistID);
                    return;
                }
                _processingArtistIDs.Add(artistID);
            }

            // Get the artist image
            ImageSource artistImageSource = await GetArtistImage(artistID);

            lock (_processingArtistIDs)
                _processingArtistIDs.Remove(artistID);

            // If this artist is still the current artist, set the image source
            if (artistName == CurrentArtistName)
            {
                CurrentArtistImageSource = artistImageSource;
                SendImageSourceUpdated();
            }
        }


        #region Artist IDs

        private static async Task<string> GetArtistID(string artistName)
        {
            // Look for artist ID in cache
            if (_artistIDs.ContainsKey(artistName))
                return _artistIDs[artistName];

            _log.Info("Looking up ID for artist '{0}'...", artistName);

            string normalizedArtistName = artistName.ToLowerInvariant();

            string uri = string.Format(MarketplaceSearchURIFormat, Locale, "artist", artistName);

            try
            {
                HttpClient client = new HttpClient();
                string response = await client.GetStringAsync(uri);

                XDocument document = XDocument.Parse(response);
                XNamespace xmlns = "http://www.w3.org/2005/Atom";

                var artists = from node in document.Descendants(xmlns + "entry")
                              select new
                              {
                                  ID = node.Element(xmlns + "id").Value.Replace("urn:uuid:", ""),
                                  Name = node.Element(xmlns + "title").Value.ToLowerInvariant(),
                              };

                // Try to locate an artist with a matching name
                string artistID = null;
                foreach (var artist in artists)
                {
                    if (artist.Name == normalizedArtistName)
                        artistID = artist.ID;
                }

                // Otherwise, try to return the first artist
                if (artistID == null)
                {
                    var firstArtist = artists.FirstOrDefault();
                    if (firstArtist != null)
                        artistID = firstArtist.ID;
                }

                // If we found an artist ID, store it to the cache
                if (artistID != null)
                {
                    _artistIDs[artistName] = artistID;
                    _artistIDCache.Value.Add(new CachedArtistID() { Name = artistName, ID = artistID, Date = DateTime.Now });
                    _artistIDCache.Save();
                }

                return artistID;
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region Artist Images

        private static async Task<ImageSource> GetArtistImage(string artistID)
        {
            using (var isolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (!isolatedStorage.DirectoryExists(ArtistBackgroundImageCacheDirectory))
                    isolatedStorage.CreateDirectory(ArtistBackgroundImageCacheDirectory);

                string filename = ArtistBackgroundImageCacheDirectory + "/" + artistID + ".jpg";

                if (!isolatedStorage.FileExists(filename))
                {
                    _log.Info("Downloading artist background image for ID '{0}'...", artistID);

                    HttpClient client = new HttpClient();
                    string uri = string.Format(MarketplaceArtistBackgroundURIFormat, Locale, artistID, 768);
                    try
                    {
                        Stream stream = await client.GetStreamAsync(uri);
                        using (var fileStream = isolatedStorage.OpenFile(filename, FileMode.OpenOrCreate))
                            await stream.CopyToAsync(fileStream);

                        _log.Info("Finished downloading background image for ID '{0}'", artistID);
                    }
                    catch (HttpRequestException)
                    {
                        _log.Error("Error downloading artist background image for ID '{0}'", artistID);
                        return null;
                    }
                }

                using (var fileStream = isolatedStorage.OpenFile(filename, FileMode.Open))
                {
                    try
                    {
                        BitmapImage image = new BitmapImage();
                        image.SetSource(fileStream);
                        return image;
                    }
                    catch { }
                }

                // If we get here, an error occurred while loading the image, which probably means we didn't receive a valid image as the response.
                _log.Warning("Invalid file for artist ID '{0}', deleting...", artistID);
                isolatedStorage.DeleteFile(filename);

                return null;
            }
        }

        #endregion

    }

    public class ArtistBackgroundImageSourceUpdatedEventArgs : EventArgs
    {
        public ArtistBackgroundImageSourceUpdatedEventArgs(ImageSource imageSource)
        {
            ImageSource = imageSource;
        }

        public ImageSource ImageSource { get; protected set; }
    }

    public class CachedArtistID
    {
        public string Name { get; set; }
        public string ID { get; set; }
        public DateTime Date { get; set; }
    }
}
