using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sitecore.Data;
using Sitecore.Data.DataProviders;
using Sitecore.Data.IDTables;
using Sitecore.Collections;
using Sitecore.Caching;
using System.IO;
using System.Net;
using System.Xml;
using System.Xml.Linq;
using BotR.API;
using Sitecore.Globalization;
using Sitecore.Data.Items;

namespace Sitecore.BitsOnTheRun
{
    public class BotrDataProvider : DataProvider
    {
        public const string CACHE_NAME = "Sitecore.BitsOnTheRun.BotrDataProvider.VideoResponse";
        public const string CACHE_KEY_VIDEO_DATA = "/videos/list";
        public const string CACHE_KEY_CONVERSION_DATA = "/videos/conversions/list/";
        public const string CACHE_KEY_WIDTH = "WIDTH_";
        public const string CACHE_KEY_HEIGHT = "HEIGHT_";

        protected static Cache _cache = null;
        protected static Object _cacheLock = new Object();

        private const string STATUS_FIELD = "{C7265AB1-14F6-4582-B981-6382D9E7321E}";
        private const string TITLE_FIELD = "3F4B20E9-36E6-4D45-A423-C86567373F82";
        private const string DESCRIPTION_FIELD = "BA8341A1-FF30-47B8-AE6A-F4947E4113F0";
        private const string KEYWORDS_FIELD = "2FAFE7CB-2691-4800-8848-255EFA1D31AA";
        private const string ALT_FIELD = "65885C44-8FCD-4A7F-94F1-EE63703FE193";
        private const string WIDTH_FIELD = "22EAC599-F13B-4607-A89D-C091763A467D";
        private const string HEIGHT_FIELD = "DE2CA9E4-C117-4C8A-A139-1FF4B199D15A";
        private const string EXTENSION_FIELD = "C06867FE-9A43-4C7D-B739-48780492D06F";
        private const string MIME_TYPE_FIELD = "6F47A0A5-9C94-4B48-ABEB-42D38DEF6054";
        private const string BLOB_FIELD = "40E50ED9-BA07-4702-992E-A912738D32DC";
        private const string VIDEO_KEY_FIELD = "D5EB8F32-14D1-4AAB-BA76-A779C99B2873";
        private const string VIDEO_URL_FIELD = "{E014F1B9-C430-4643-A3A9-5B16282D4852}";
        private const string VIDEO_CUSTOM_URL_FIELD = "{7E5BAA82-DCAA-4676-B723-B2F6E5CD81E5}";
        private const string CREATED_FIELD = "{25BED78C-4957-4165-998A-CA1B52F67497}";
        private const string UPDATED_FIELD = "{D9CF14B1-FA16-4BA6-9288-E8A174D4D522}";
        private const string DISPLAY_NAME_FIELD = "{B5E02AD9-D56F-4C41-A065-A133DB87BDEB}";

        private const string BOTR_VIDEO_KEY = "video_key";
        private const string BOTR_TITLE = "title";
        private const string BOTR_TAGS = "tags";
        private const string BOTR_DESCRIPTION = "description";
        private const string BOTR_LINK = "link";
        private const string BOTR_CUSTOM = "custom";
        private const string BOTR_CUSTOM_LINK = "link";
        private const string BOTR_DATE = "date";
        private const string BOTR_STATUS = "status";
        private const string BOTR_WIDTH = "width";
        private const string BOTR_HEIGHT = "height";

        private const int THUMBNAIL_SIZE = 120;
        private const string EXTENSION = "jpg";
        private const string MIME_TYPE = "image/jpeg";
        private const string ID_TABLE_PREFIX = "Sitecore.BitsOnTheRun.BotrDataProvider";
        private const string TEMPLATE = "4D67E4CA-E2F2-4C25-89BB-B20E643E39F7";

        private string _parentItem = null;
        private string _cacheMaxSize = "5MB";
        private int _cacheExpirationMinutes = 15;
        private string _videoLinkHost = null;
        private string _vodeoLinkSite = null;
        private string _botrApiKey = null;
        private string _botrApiSecret = null;

        public BotrDataProvider(string parentItem, string botrApiKey, string botrApiSecret, string cacheMaxSize, string cacheExpirationMinutes, string videoLinkHost, string videoLinkSite) : base()
        {
            _parentItem = parentItem;
            if (cacheMaxSize != null)
            {
                _cacheMaxSize = cacheMaxSize;
            }
            if (cacheExpirationMinutes != null)
            {
                Int32.TryParse(cacheExpirationMinutes, out _cacheExpirationMinutes);
            }
            _videoLinkHost = videoLinkHost;
            _vodeoLinkSite = videoLinkSite;
            _botrApiKey = botrApiKey;
            _botrApiSecret = botrApiSecret;
        }

        protected Cache Cache
        {
            get
            {
                lock (_cacheLock)
                {
                    if (_cache == null)
                    {
                        _cache = Cache.GetNamedInstance(CACHE_NAME, Sitecore.StringUtil.ParseSizeString(_cacheMaxSize));
                    }
                }
                return _cache;
            }
        }

        private XmlDocument VideoData
        {
            get
            {
                var data = Cache.GetEntry(CACHE_KEY_VIDEO_DATA, false);
                if (data != null)
                {
                    return data.Data as XmlDocument;
                }
                var response = CallApi("/videos/list", new Dictionary<string, string> { { "order_by", "title" }, { "result_limit", "0" } });
                Cache.Add(CACHE_KEY_VIDEO_DATA, response, response.OuterXml.Length, DateTime.UtcNow.AddMinutes(_cacheExpirationMinutes));
                return response;
            }
        }

        public override IDList GetChildIDs(ItemDefinition itemDefinition, CallContext context)
        {
            if (itemDefinition.ID == ID.Parse(_parentItem))
            {
                context.Abort();
                XmlNodeList videos = VideoData.SelectNodes("/response/videos/video");
                IDList videoIDs = new IDList();
                foreach (XmlNode video in videos)
                {
                    string videoKey = video.Attributes["key"].Value;
                    IDTableEntry mappedID = IDTable.GetID(ID_TABLE_PREFIX, videoKey);
                    if (mappedID == null)
                    {
                        mappedID = IDTable.GetNewID(ID_TABLE_PREFIX, videoKey, ID.Parse(_parentItem));
                    }
                    videoIDs.Add(mappedID.ID);
                }
                return videoIDs;
            }
            return base.GetChildIDs(itemDefinition, context);
        }

        public override ID GetParentID(ItemDefinition itemDefinition, CallContext context)
        {
            IDTableEntry[] idList = IDTable.GetKeys(ID_TABLE_PREFIX, itemDefinition.ID);
            if (idList.Length > 0)
            {
                context.Abort();
                return ID.Parse(_parentItem);
            }
            return base.GetParentID(itemDefinition, context);
        }

        public override ItemDefinition GetItemDefinition(ID itemId, CallContext context)
        {
            IDTableEntry[] idList = IDTable.GetKeys(ID_TABLE_PREFIX, itemId);
            if (idList.Length > 0)
            {
                IDTableEntry mappedID = idList[0];
                context.Abort();
                XmlNode video = VideoData.SelectSingleNode("/response/videos/video[@key='" + mappedID.Key + "']");
                if (video == null)
                {
                    IDTable.RemoveID(ID_TABLE_PREFIX, mappedID.ID);
                    return null;
                }
                ItemDefinition videoItem = new ItemDefinition(itemId, mappedID.Key, ID.Parse(TEMPLATE), ID.Null);
                return videoItem;
            }
            return base.GetItemDefinition(itemId, context);
        }

        public override VersionUriList GetItemVersions(ItemDefinition item, CallContext context)
        {
            IDTableEntry[] idList = IDTable.GetKeys(ID_TABLE_PREFIX, item.ID);
            if (idList.Length > 0)
            {
                context.Abort();
                VersionUriList versions = new VersionUriList();
                LanguageCollection languages = Sitecore.Data.Managers.LanguageManager.GetLanguages(this.Database);
                foreach (Language language in languages)
                {
                    versions.Add(language, new Sitecore.Data.Version(1));
                }
                return versions;
            }
            return null;
        }

        public override Sitecore.Data.FieldList GetItemFields(ItemDefinition itemDefinition, VersionUri versionUri, CallContext context)
        {
            IDTableEntry[] idList = IDTable.GetKeys(ID_TABLE_PREFIX, itemDefinition.ID);
            if (idList.Length > 0)
            {
                IDTableEntry mappedID = idList[0];
                context.Abort();
                XmlNode video = VideoData.SelectSingleNode("/response/videos/video[@key='" + mappedID.Key + "']");
                FieldList list = new FieldList();
                list.Add(ID.Parse(DISPLAY_NAME_FIELD), video[BOTR_TITLE].InnerText);
                list.Add(ID.Parse(VIDEO_KEY_FIELD), mappedID.Key);
                list.Add(ID.Parse(STATUS_FIELD), video[BOTR_STATUS].InnerText);
                list.Add(ID.Parse(BLOB_FIELD), itemDefinition.ID.ToString());
                list.Add(ID.Parse(TITLE_FIELD), video[BOTR_TITLE].InnerText);
                list.Add(ID.Parse(DESCRIPTION_FIELD), video[BOTR_DESCRIPTION].InnerText);
                list.Add(ID.Parse(KEYWORDS_FIELD), video[BOTR_TAGS].InnerText);
                list.Add(ID.Parse(ALT_FIELD), video[BOTR_TITLE].InnerText);
                list.Add(ID.Parse(EXTENSION_FIELD), EXTENSION);
                list.Add(ID.Parse(MIME_TYPE_FIELD), MIME_TYPE);
                list.Add(ID.Parse(VIDEO_URL_FIELD), video[BOTR_LINK].InnerText);
                XmlNode custom = video[BOTR_CUSTOM];
                if (custom != null && custom[BOTR_CUSTOM_LINK] != null)
                {
                    list.Add(ID.Parse(VIDEO_CUSTOM_URL_FIELD), custom[BOTR_CUSTOM_LINK].InnerText);
                }

                double videoTimestamp = Double.Parse(video[BOTR_DATE].InnerText);
                string videoDate = Sitecore.DateUtil.ToIsoDate(UnixDateTime.FromUnixTimestamp(videoTimestamp));
                list.Add(ID.Parse(CREATED_FIELD), videoDate);
                list.Add(ID.Parse(UPDATED_FIELD), videoDate);

                //BotR puts width/height data with conversion data. just need the aspect ratio, so grab the first one in the doc
                XmlDocument conversionData = GetVideoConversionData(mappedID.Key);
                XmlNode videoConversion = conversionData.SelectSingleNode("/response/conversions/conversion");
                if (videoConversion != null)
                {
                    string heightStr = videoConversion[BOTR_HEIGHT].InnerText;
                    string widthStr = videoConversion[BOTR_WIDTH].InnerText;
                    double height = 0;
                    double width = 0;
                    Double.TryParse(heightStr, out height);
                    Double.TryParse(widthStr, out width);
                    if (height != 0 && width != 0)
                    {
                        int thumbHeight = (int)(THUMBNAIL_SIZE * (height / width));
                        list.Add(ID.Parse(WIDTH_FIELD), THUMBNAIL_SIZE.ToString());
                        list.Add(ID.Parse(HEIGHT_FIELD), thumbHeight.ToString());
                    }
                }
                return list;
            }
            return base.GetItemFields(itemDefinition, versionUri, context);
        }

        public override Stream GetBlobStream(Guid blobId, CallContext context)
        {
            IDTableEntry[] idList = IDTable.GetKeys(ID_TABLE_PREFIX, new ID(blobId));
            if (idList.Length > 0)
            {
                IDTableEntry mappedID = idList[0];
                context.Abort();
                return GetThumbnailStream(mappedID.Key);
            }
            return null;
        }

        public override bool BlobStreamExists(Guid blobId, CallContext context)
        {
            IDTableEntry[] idList = IDTable.GetKeys(ID_TABLE_PREFIX, new ID(blobId));
            if (idList.Length > 0)
            {
                context.Abort();
                return true;
            }
            return false;
        }

        public override bool SaveItem(ItemDefinition itemDefinition, Sitecore.Data.Items.ItemChanges changes, CallContext context)
        {
            IDTableEntry[] idList = IDTable.GetKeys(ID_TABLE_PREFIX, itemDefinition.ID);
            if (idList.Length > 0 && changes.HasFieldsChanged)
            {
                IDTableEntry mappedID = idList[0];
                context.Abort();

                 var values = new Dictionary<string, string>();
                 values.Add(BOTR_VIDEO_KEY, mappedID.Key);

                FieldChangeList fieldChanges = changes.FieldChanges;
                lock (fieldChanges.SyncRoot)
                {
                    foreach (FieldChange change in fieldChanges)
                    {
                        if (change.FieldID == ID.Parse(DISPLAY_NAME_FIELD) || change.FieldID == ID.Parse(TITLE_FIELD) || change.FieldID == ID.Parse(ALT_FIELD))
                        {
                            values[BOTR_TITLE] = change.Value;
                        }
                        else if (change.FieldID == ID.Parse(DESCRIPTION_FIELD))
                        {
                            values[BOTR_DESCRIPTION] = change.Value;
                        }
                        else if (change.FieldID == ID.Parse(KEYWORDS_FIELD))
                        {
                            values[BOTR_TAGS] = change.Value;
                        }
                        else if (change.FieldID == ID.Parse(VIDEO_CUSTOM_URL_FIELD))
                        {
                            Sitecore.Links.UrlOptions options = Sitecore.Links.UrlOptions.DefaultOptions;
                            options.Site = Sitecore.Sites.SiteContextFactory.GetSiteContext(_vodeoLinkSite);
                            options.AlwaysIncludeServerUrl = false;
                            values[BOTR_CUSTOM + "." + BOTR_CUSTOM_LINK] = change.Value;
                            if (change.RemoveField || string.IsNullOrEmpty(change.Value))
                            {
                                values[BOTR_LINK] = string.Empty;
                            }
                            else
                            {
                                values[BOTR_LINK] = "http://" +
                                                    Sitecore.StringUtil.RemovePostfix('/', this._videoLinkHost) +
                                                    Sitecore.Links.LinkManager.GetItemUrl(context.DataManager.Database.GetItem(ID.Parse(change.Value)), options);
                            }
                        }
                        else if (change.FieldID == ID.Parse(CREATED_FIELD))
                        {
                            values[BOTR_DATE] = Sitecore.DateUtil.IsoDateToDateTime(change.Value).ToUnixTimestamp().ToString();
                        }
                    }

                    XmlDocument response = CallApi("/videos/update", values);
                    XmlNode status = response.SelectSingleNode("/response/status");
                    if (status != null && status.InnerText.ToLower() == "ok")
                    {
                        Cache.Clear();
                        return true;
                    }
                }
                return false;
            }
            return false;
        }

        public override bool DeleteItem(ItemDefinition itemDefinition, CallContext context)
        {
            IDTableEntry[] idList = IDTable.GetKeys(ID_TABLE_PREFIX, itemDefinition.ID);
            if (idList.Length > 0)
            {
                IDTableEntry mappedID = idList[0];
                context.Abort();
                XmlDocument response = CallApi("/videos/delete", new Dictionary<string, string> { { BOTR_VIDEO_KEY, mappedID.Key } });
                XmlNode status = response.SelectSingleNode("/response/status");
                if (status != null && status.InnerText.ToLower() == "ok")
                {
                    Cache.Clear();
                    return true;
                }
                return false;
            }
            return false;
        }

        protected XmlDocument CallApi(string callString, Dictionary<string,string> values)
        {
            BotRAPI api = new BotRAPI(_botrApiKey, _botrApiSecret);
            string returnString;
            if (values == null)
            {
                returnString = api.Call(callString);
            }
            else
            {
                System.Collections.Specialized.NameValueCollection nameValues = new System.Collections.Specialized.NameValueCollection();
                foreach (var value in values)
                {
                    nameValues.Add(value.Key, value.Value);
                }
                returnString = api.Call(callString, nameValues);
            }
            if (string.IsNullOrEmpty(returnString))
            {
                throw new Exception("Bits on the Run returned empty result. Check API key configuration.");
            }
            XmlDocument result = new XmlDocument();
            result.LoadXml(returnString);
            return result;
        }

        protected Stream GetThumbnailStream(string videoKey)
        {
            string imageUrl = "http://content.bitsontherun.com/thumbs/" + videoKey + "-" + THUMBNAIL_SIZE + ".jpg";
            WebClient webClient = new WebClient();
            byte[] buffer = new byte[1024];
            int count = 0;
            using (BufferedStream imageStream = new BufferedStream(webClient.OpenRead(imageUrl)))
            {
                MemoryStream memoryStream = new MemoryStream();
                while ((count = imageStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    memoryStream.Write(buffer, 0, buffer.Length);
                }
                return memoryStream;
            }
        }

        protected XmlDocument GetVideoConversionData(string videoKey)
        {
            var data = Cache.GetEntry(CACHE_KEY_CONVERSION_DATA + videoKey, false);
            if (data != null)
            {
                return data.Data as XmlDocument;
            }
            var response = CallApi("/videos/conversions/list", new Dictionary<string, string> { {BOTR_VIDEO_KEY, videoKey} });
            Cache.Add(CACHE_KEY_CONVERSION_DATA + videoKey, response, response.OuterXml.Length, DateTime.UtcNow.AddMinutes(_cacheExpirationMinutes));
            return response;
        }

    }
}
