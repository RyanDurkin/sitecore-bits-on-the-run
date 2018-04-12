<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="ExampleUsage.aspx.cs" Inherits="Sitecore.BitsOnTheRun.Web.sitecore_modules.BotR.ExampleUsage" %>


<%--
    This example is not meant to be production code, but rather just communicates the basics
    of how to output videos from your integrated BotR repository.
--%>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Example BotR Module Usage</title>
</head>
<body>
    <div>
        <%
            foreach (var video in Videos)
            {
                String thumbnail = Sitecore.Resources.Media.MediaManager.GetMediaUrl(video);
        %>
                <div>
                    <h3><%=video.Name%></h3>
                    <img src="<%=thumbnail%>" /><br />
                    <script type="text/javascript" src="http://content.bitsontherun.com/players/<%=video["Video Key"]%>-<%=PLAYER_ID%>.js"></script>
                </div>
        <%
            }
        %>
    </div>
</body>
</html>
