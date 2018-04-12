using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using Sitecore.Data.Items;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Caching;

namespace Sitecore.BitsOnTheRun
{
    public class DataRefreshCommand : Command
    {
        public override void Execute(CommandContext context)
        {
            XmlNode botrItemID = Sitecore.Configuration.Factory.GetConfigNode("/sitecore/dataProviders/botr/param[@desc='parentItem']");
            if (botrItemID != null)
            {
                ItemCache itemCache = Sitecore.Context.ContentDatabase.Caches.ItemCache;
                DataCache dataCache = Sitecore.Context.ContentDatabase.Caches.DataCache;
                Item botrItem = Sitecore.Context.ContentDatabase.GetItem(botrItemID.InnerText);
                if (botrItem != null)
                {
                    foreach (Item video in botrItem.GetChildren())
                    {
                        itemCache.RemoveItem(video.ID);
                        dataCache.RemoveItemInformation(video.ID);
                    }
                    itemCache.RemoveItem(botrItem.ID);
                    dataCache.RemoveItemInformation(botrItem.ID);

                    var cache = CacheManager.FindCacheByName(BotrDataProvider.CACHE_NAME);
                    if (cache != null)
                    {
                        cache.Clear();
                    }
                    Sitecore.Context.ClientPage.SendMessage(this, string.Format("item:refresh(id={0})", botrItem.ID));
                    Sitecore.Context.ClientPage.SendMessage(this, string.Format("item:refreshchildren(id={0})", botrItem.ID));
                    Sitecore.Context.ClientPage.SendMessage(this, string.Format("item:load(id={0})", botrItem.ID));
                }
            }
        }
    }
}
