using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Sitecore.Data;
using Sitecore.Data.Items;

namespace Sitecore.BitsOnTheRun.Web.sitecore_modules.BotR
{
    public partial class ExampleUsage : System.Web.UI.Page
    {
        //replace with your player id from BotR
        protected const string PLAYER_ID = "HaGeB32A";
        protected const string ROOT_ID = "{FA872777-6817-4E0D-953E-71C2879F6BD4}";

        protected Item[] Videos;

        protected void Page_Load(object sender, EventArgs e)
        {
            Item botrRoot = Sitecore.Context.Database.GetItem(Sitecore.Data.ID.Parse(ROOT_ID));
            Videos = botrRoot.GetChildren().ToArray();
        }
    }
}