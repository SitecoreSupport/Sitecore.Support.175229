namespace Sitecore.Support.Pipelines.RenderLayout
{
  using Sitecore.Configuration;
  using Sitecore.Diagnostics;
  using Sitecore.Pipelines.RenderLayout;
  using Sitecore.Sites;
  using Sitecore.Text;
  using Sitecore.Web;

  public class SecurityCheck : Sitecore.Pipelines.RenderLayout.SecurityCheck
  {
    public override void Process([NotNull] RenderLayoutArgs args)
    {
      Assert.ArgumentNotNull(args, "args");

      Profiler.StartOperation("Check security access to page.");

      if (!HasAccess())
      {
        args.AbortPipeline();

        SiteContext site = Context.Site;

        string loginPage = GetLoginPage(site);

        if (loginPage.Length > 0)
        {
          Tracer.Info("Redirecting to login page \"" + loginPage + "\".");
          UrlString url = new UrlString(loginPage);
          if (Settings.Authentication.SaveRawUrl)
          {
            #region Changed code
            //use returnUrl parameter to ensure login page understand where to redirect
            url.Append("returnUrl", Context.RawUrl); // removed HttpUtility.UrlEncode to avoid double encoding
            #endregion
          }

          WebUtil.Redirect(url.ToString(), false);
        }
        else
        {
          Tracer.Info("Redirecting to error page as no login page was found.");

          WebUtil.RedirectToErrorPage("Login is required, but no valid login page has been specified for the site (" + Context.Site.Name + ").", false);
        }
      }

      Profiler.EndOperation();
    }
  }
}