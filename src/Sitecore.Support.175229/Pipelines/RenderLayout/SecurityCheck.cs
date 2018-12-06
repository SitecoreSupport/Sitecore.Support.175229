using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sitecore.Configuration;
using Sitecore.Diagnostics;
using Sitecore.Pipelines.RenderLayout;
using Sitecore.Sites;
using Sitecore.Text;
using Sitecore.Web;

namespace Sitecore.Support.Pipelines.RenderLayout
{
  public class SecurityCheck : Sitecore.Pipelines.RenderLayout.SecurityCheck
  {
    public override void Process(RenderLayoutArgs args)
    {
      Assert.ArgumentNotNull(args, "args");
      Profiler.StartOperation("Check security access to page.");
      if (!this.HasAccess())
      {
        args.AbortPipeline();
        SiteContext site = Context.Site;
        string loginPage = this.GetLoginPage(site);
        if (loginPage.Length <= 0)
        {
          Tracer.Info("Redirecting to error page as no login page was found.");
          WebUtil.RedirectToErrorPage(
            "Login is required, but no valid login page has been specified for the site (" + Context.Site.Name + ").",
            false);
        }
        else
        {
          Tracer.Info("Redirecting to login page \"" + loginPage + "\".");
          UrlString str2 = new UrlString(loginPage);
          if (Settings.Authentication.SaveRawUrl)
          {
            str2.Append("url", HttpUtility.UrlEncode(Context.RawUrl));
          }

          WebUtil.Redirect(str2.ToString(), false);
        }
      }

      Profiler.EndOperation();
    }
  }
}