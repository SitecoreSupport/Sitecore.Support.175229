using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Layouts;
using Sitecore.Pipelines.HttpRequest;
using Sitecore.SecurityModel;
using Sitecore.Sites;
using Sitecore.Web;
using Version = Sitecore.Data.Version;

namespace Sitecore.Support.Pipelines.HttpRequest
{
  public class ExecuteRequest : Sitecore.Pipelines.HttpRequest.ExecuteRequest
  {
    #region Original Code
    private string GetNoAccessUrl(out bool loginPage)
    {
      SiteContext site = Context.Site;
      loginPage = false;
      if ((site == null) || (site.LoginPage.Length <= 0))
      {
        Tracer.Warning("Redirecting to \"No Access\" page as no login page was found.");
        return Settings.NoAccessUrl;
      }

      if (SiteManager.CanEnter(site.Name, Context.User))
      {
        Tracer.Info("Redirecting to login page \"" + site.LoginPage + "\".");
        loginPage = true;
        return site.LoginPage;
      }

      Tracer.Info("Redirecting to the 'No Access' page as the current user '" + Context.User.Name +
                  "' does not have sufficient rights to enter the '" + site.Name + "' site.");
      return Settings.NoAccessUrl;
    }

    private void HandleItemNotFound(HttpRequestArgs args)
    {
      string localPath = args.LocalPath;
      string name = Context.User.Name;
      bool flag = false;
      bool loginPage = false;
      string itemNotFoundUrl = Settings.ItemNotFoundUrl;
      if (args.PermissionDenied)
      {
        flag = true;
        itemNotFoundUrl = this.GetNoAccessUrl(out loginPage);
      }

      SiteContext site = Context.Site;
      string[] collection = new string[]
        {"item", localPath, "user", name, "site", (site != null) ? site.Name : string.Empty};
      List<string> list = new List<string>(collection);
      if (Settings.Authentication.SaveRawUrl)
      {
        #endregion

        #region Modified Code

        string[] strArray2;
        if (flag && loginPage)
        {
          strArray2 = new string[] {"returnUrl", HttpUtility.UrlEncode(Context.RawUrl)};
        }
        else
        {
          strArray2 = new string[] { "url", HttpUtility.UrlEncode(Context.RawUrl) };
        }

        #endregion

        #region Original Code

        list.AddRange(strArray2);
      }

      itemNotFoundUrl = WebUtil.AddQueryString(itemNotFoundUrl, list.ToArray());
      if (!flag)
      {
        this.RedirectOnItemNotFound(itemNotFoundUrl);
      }
      else
      {
        if (loginPage)
        {
          this.RedirectToLoginPage(itemNotFoundUrl);
        }

        this.RedirectOnNoAccess(itemNotFoundUrl);
      }
    }

    private void HandleSiteAccessDenied(SiteContext site, HttpRequestArgs args)
    {
      string[] parameters = new string[]
        {"item", args.LocalPath, "user", Context.GetUserName(), "site", site.Name, "right", "site:enter"};
      string url = WebUtil.AddQueryString(Settings.NoAccessUrl, parameters);
      this.RedirectOnSiteAccessDenied(url);
    }

    private void HandleLayoutNotFound(HttpRequestArgs args)
    {
      string itemPath = string.Empty;
      string name = string.Empty;
      string url = string.Empty;
      DeviceItem device = Context.Device;
      if (device != null)
      {
        name = device.Name;
      }

      Item item = Context.Item;
      if ((item != null) && (device != null))
      {
        itemPath = item.Visualization.GetLayoutID(device).ToString();
        if (itemPath.Length > 0)
        {
          Database database = Context.Database;
          Assert.IsNotNull(database, "No database on processor.");
          Item item3 = ItemManager.GetItem(itemPath, Language.Current, Version.Latest, database, SecurityCheck.Disable);
          if ((item3 != null) && !item3.Access.CanRead())
          {
            SiteContext site = Context.Site;
            string[] parameters = new string[] {"item"};
            string[] strArray2 = new string[] {"Layout: ", itemPath, " (item: ", args.LocalPath, ")"};
            parameters[1] = string.Concat(strArray2);
            parameters[2] = "user";
            parameters[3] = Context.GetUserName();
            parameters[4] = "site";
            parameters[5] = (site != null) ? site.Name : string.Empty;
            parameters[6] = "device";
            parameters[7] = name;
            url = WebUtil.AddQueryString(Settings.NoAccessUrl, parameters);
          }
        }
      }

      if (url.Length == 0)
      {
        string[] parameters = new string[] {"item", args.LocalPath, "layout", itemPath, "device", name};
        url = WebUtil.AddQueryString(Settings.LayoutNotFoundUrl, parameters);
      }

      this.RedirectOnLayoutNotFound(url);
    }

    public override void Process(HttpRequestArgs args)
    {
      Assert.ArgumentNotNull(args, "args");
      SiteContext site = Context.Site;
      if ((site != null) && !SiteManager.CanEnter(site.Name, Context.User))
      {
        this.HandleSiteAccessDenied(site, args);
      }
      else
      {
        PageContext page = Context.Page;
        Assert.IsNotNull(page, "No page context in processor.");
        string filePath = page.FilePath;
        if (filePath.Length <= 0)
        {
          if (Context.Item == null)
          {
            this.HandleItemNotFound(args);
          }
          else
          {
            this.HandleLayoutNotFound(args);
          }
        }
        else if (WebUtil.IsExternalUrl(filePath))
        {
          args.Context.Response.Redirect(filePath, true);
        }
        else if (string.Compare(filePath, HttpContext.Current.Request.Url.LocalPath,
                   StringComparison.InvariantCultureIgnoreCase) != 0)
        {
          args.Context.RewritePath(filePath, args.Context.Request.PathInfo, args.Url.QueryString, false);
        }
      }
    }

    #endregion
  }
}