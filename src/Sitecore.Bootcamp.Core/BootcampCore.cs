namespace Sitecore.Bootcamp
{
  using System;
  using System.Diagnostics;
  using System.IO;
  using System.Linq;
  using System.Reflection;
  using System.Threading;
  using System.Web;
  using System.Web.UI;
  using Sitecore.Bootcamp.Processors;
  using Sitecore.Diagnostics.Base;
  using Sitecore.Diagnostics.Base.Annotations;

  public class BootcampCore
  {
    private readonly BootcampMode Mode;

    private readonly bool Noisy;

    [NotNull]
    private readonly HttpServerUtility Server;

    [CanBeNull]
    private readonly HttpResponse Response;

    [NotNull]
    private readonly HttpApplicationState Application;

    internal BootcampCore([NotNull] Page page, BootcampMode mode, bool noisy) : this(page.Server, page.Application, page.Response, mode, noisy)
    {
      Assert.ArgumentNotNull(page, "page");
    }

    public BootcampCore([NotNull] HttpServerUtility server, [NotNull] HttpApplicationState application, [CanBeNull] HttpResponse response, BootcampMode mode, bool noisy)
    {
      Assert.ArgumentNotNull(server, "server");
      Assert.ArgumentNotNull(application, "application");

      this.Server = server;
      this.Application = application;
      this.Response = response;
      this.Mode = mode;
      this.Noisy = noisy;
    }

    public BootcampCore(HttpContext context, BootcampMode mode, bool noisy) : this(context.Server, context.Application, context.Response, mode, noisy)
    {
      Assert.ArgumentNotNull(context, "context");
    }

    public static void Install([NotNull] Page page, BootcampMode mode, bool noisy)
    {
      Assert.ArgumentNotNull(page, "page");

      new BootcampCore(page, mode, noisy).Install();
    }

    public void Install()
    {
      this.Server.ScriptTimeout = int.MaxValue;
      var response = this.Response;
      if (response != null)
        response.StatusCode = 503;

      var lockFilePath = this.Server.MapPath("lock.txt");
      if (File.Exists(lockFilePath))
      {
        while (File.Exists(lockFilePath))
        {
          Thread.Sleep(1000);
        }

        response?.Redirect("/");
      }

      try
      {
        File.Open(lockFilePath, FileMode.CreateNew)
        .Close();
      }
      catch (IOException)
      {
        while (File.Exists(lockFilePath))
        {
          Thread.Sleep(1000);
        }

        response?.Redirect("/");
      }

      try
      {
        var app = this.Application;
        Assert.IsNotNull(app, "app");

        var sitecoreVersion = GetSitecoreVersion();

        // check license
        var license = this.Server.MapPath("/App_Data/License.xml");
        if (!File.Exists(license))
        {
          throw new InvalidOperationException("No license.xml file is detected in /App_Data folder");
        }

        this.WriteLine("Installing Sitecore...");
        this.WriteLine("");
        this.WriteLine("IMPORTANT!");
        this.WriteLine("- Do not abort this request (do not close tab or browser, do not refresh the page) as installation happens in its working thread.");
        this.WriteLine("- The installation log only available in this request, all the rest requests will be waiting silently.");
        this.WriteLine("");

        Pipeline.Run(new ProcessorArgs(this, this.Server, this.Mode, sitecoreVersion));

        this.WriteLine("");
        this.WriteLine("Sitecore is starting now...<script>document.location.reload();</script></body></html>");
      }
      finally
      {
        if (File.Exists(lockFilePath))
        {
          File.Delete(lockFilePath);
        }
      }
    }

    private string GetSitecoreVersion()
    {
      // check kernel
      var kernel1 = this.Server.MapPath("/bin/Sitecore.Kernel.dll");
      var kernel2 = this.Server.MapPath("/App_Bin/Sitecore.Kernel.dll");
      if (!File.Exists(kernel1) && !File.Exists(kernel2))
      {
        throw new InvalidOperationException("No Sitecore.Kernel.dll file is detected in both /bin and /App_Bin folders.");
      }

      var kernelPath = kernel1;
      if (!File.Exists(kernelPath))
      {
        kernelPath = kernel2;

        Assert.IsTrue(File.Exists(kernelPath), "Cannot find Sitecore.Kernel.dll in both bin and App_Bin folders.");
      }

      var kernelVersion = FileVersionInfo.GetVersionInfo(kernelPath).ProductVersion;
      Assert.IsNotNullOrEmpty(kernelVersion, "kernelVersion");

      return kernelVersion;
    }

    internal void WriteLine([NotNull] string message, bool bypassNoisy = false)
    {
      Assert.ArgumentNotNull(message, "message");

      if (!Noisy && !bypassNoisy)
      {
        return;
      }

      var response = this.Response;
      if (response == null)
      {
        return;
      }

      response.Write(message + "<br />");
      response.Flush();
    }
  }
}