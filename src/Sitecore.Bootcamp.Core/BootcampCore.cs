using System.Collections.Generic;
using System.ComponentModel;

namespace Sitecore.Bootcamp.Core
{
  using System;
  using System.IO;
  using System.Threading;
  using System.Web.UI;
  using Sitecore.Bootcamp.Core.Processors;
  using Sitecore.Diagnostics.Base;
  using Sitecore.Diagnostics.Base.Annotations;

  public class BootcampCore
  {
    [NotNull]
    internal readonly Page Page;

    private static bool installationStarted;

    private static bool installationFinished;

    private readonly BootcampMode Mode;

    private readonly bool Noisy;

    [NotNull]
    private readonly List<string> Messages = new List<string>();

    internal BootcampCore([NotNull] Page page, BootcampMode mode, bool noisy)
    {
      Assert.ArgumentNotNull(page, "page");

      this.Page = page;
      this.Mode = mode;
      this.Noisy = noisy;
    }

    public static void Install([NotNull] Page page, BootcampMode mode, bool noisy)
    {
      Assert.ArgumentNotNull(page, "page");

      new BootcampCore(page, mode, noisy).Install();
    }

    public void Install()
    {
      this.Page.Server.ScriptTimeout = int.MaxValue;
      this.Page.Response.StatusCode = 503;

      var lockFilePath = this.Page.Server.MapPath("lock.txt");
      if (File.Exists(lockFilePath))
      {
        while (File.Exists(lockFilePath))
        {
          Thread.Sleep(1000);
        }

        this.Page.Response.Redirect("/");
      }

      File.Open(lockFilePath, FileMode.CreateNew)
        .Close();
      
      try
      {
        var app = this.Page.Application;
        Assert.IsNotNull(app, "app");

        // check kernel
        var kernel1 = this.Page.Server.MapPath("/bin/Sitecore.Kernel.dll");
        var kernel2 = this.Page.Server.MapPath("/App_Bin/Sitecore.Kernel.dll");
        if (!File.Exists(kernel1) && !File.Exists(kernel2))
        {
          throw new InvalidOperationException("No Sitecore.Kernel.dll file is detected in both /bin and /App_Bin folders");
        }

        // check license
        var license = this.Page.Server.MapPath("/App_Data/License.xml");
        if (!File.Exists(license))
        {
          throw new InvalidOperationException("No license.xml file is detected in /App_Data folder");
        }

        this.WriteLine("Installing Sitecore...");
        this.WriteLine("");
        this.WriteLine("IMPORTANT! The installation log only available in this request, all the rest requests will be waiting silently.");
        this.WriteLine("");

        Pipeline.Run(new ProcessorArgs(this, this.Mode));

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

    internal void WriteLine([NotNull] string message, bool bypassNoisy = false)
    {
      Assert.ArgumentNotNull(message, "message");

      if (!Noisy && !bypassNoisy)
      {
        return;
      }

      this.Page.Response.Write(message + "<br />");
      this.Page.Response.Flush();
    }
  }
}