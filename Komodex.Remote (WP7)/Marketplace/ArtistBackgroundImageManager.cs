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

        private const string MarketplaceSearchURIFormat = "http://catalog.zune.net/v3.2/{0}/music/{1}/?chunksize=20&isActionable=true&q={2}";
        private const string MarketplaceArtistBackgroundURIFormat = "http://image.catalog.zune.net/v3.2/{0}/music/artist/{1}/primaryimage?height={2}&contenttype=image/jpeg&resize=true";
        private const string ArtistBackgroundImageCacheDirectory = "/ArtistBackgroundImageCache";

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

            if (artistName == null)
                return;

            if (string.IsNullOrWhiteSpace(artistName))
            {
                SendImageSourceUpdated();
                return;
            }

            artistName = artistName.Trim();

            // Get the artist ID
            string artistID = await GetArtistID(artistName);

            if (artistID == null)
            {
                _log.Warning("Could not determine ID for artist '{0}'.", artistName);
                if (artistName == CurrentArtistName)
                    SendImageSourceUpdated();
                return;
            }

            _log.Info("Artist '{0}' ID: '{1}'", artistName, artistID);

            // Get the artist image
            ImageSource artistImageSource = await GetArtistImage(artistID);

            if (artistName == CurrentArtistName)
            {
                CurrentArtistImageSource = artistImageSource;
                SendImageSourceUpdated();
            }
        }


        #region Artist IDs

        private static async Task<string> GetArtistID(string artistName)
        {
            // TODO: Look for artist ID in cache

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
                foreach (var artist in artists)
                {
                    if (artist.Name == normalizedArtistName)
                        return artist.ID;
                }

                // Otherwise, try to return the first artist
                var firstArtist = artists.FirstOrDefault();
                if (firstArtist != null)
                    return firstArtist.ID;

                return null;
            }
            catch (Exception e)
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
                    }
                    catch (HttpRequestException)
                    {
                        _log.Error("Error downloading artist background image for ID '{0}'", artistID);
                        return null;
                    }
                }

                using (var fileStream = isolatedStorage.OpenFile(filename, FileMode.Open))
                {
                    BitmapImage image = new BitmapImage();
                    image.SetSource(fileStream);
                    return image;
                }
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
}
