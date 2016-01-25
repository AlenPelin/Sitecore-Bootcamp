namespace Sitecore.Bootcamp.Core
{
  using System;
  using System.Collections.Generic;
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

    [NotNull]
    private static readonly List<string> Messages = new List<string>();

    private static bool installationStarted;

    private static bool installationFinished;

    private readonly BootcampMode Mode;

    private readonly bool Noisy;

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
      try
      {
        this.Process();
      }
      catch (Exception ex)
      {
        this.WriteLine("Error. " + ex.Message + "<br />Exception: " + ex.GetType().FullName + "<br />StackTrace: " + (ex.StackTrace ?? string.Empty).Replace(Environment.NewLine, "<br />"));
      }
    }

    internal void WriteLine([NotNull] string message, bool skipCache = false, bool bypassNoisy = false)
    {
      Assert.ArgumentNotNull(message, "message");

      if (!Noisy && !bypassNoisy)
      {
        return;
      }

      if (!skipCache)
      {
        Messages.Add(message);
      }

      this.Page.Response.Write(message);
      this.Page.Response.Write("<br />");
      this.Page.Response.Flush();
    }

    private void Process()
    {
      this.Page.Server.ScriptTimeout = int.MaxValue;
      this.Page.Response.StatusCode = 503;

      this.Page.Response.Write("<html><head><title>Installing Sitecore...</title><style>body { font-family: consolas, courier new; }</style></head><body>Installing Sitecore...<br />");
      this.Page.Response.Flush();

      // check license
      var license = this.Page.Server.MapPath("/App_Data/License.xml");
      if (!File.Exists(license))
      {
        this.WriteLine("No license.xml file is detected in /App_Data folder", false, true);
        return;
      }

      var app = this.Page.Application;
      Assert.IsNotNull(app, "app");

      if (app["bootcamp-started"] == null)
      {
        var start = false;
        var mutex = new Mutex(true, "bootcamp-mutex");        
        try
        {
          mutex.WaitOne(1000);
          if (app["bootcamp-started"] == null)
          {
            app.Add("bootcamp-started", new object());
            start = true;
          }
        }
        catch
        {
        }
        finally 
        {
          mutex.ReleaseMutex();
        }

        if (start)
        {
          ThreadStart action = delegate
          {
            Pipeline.Run(new ProcessorArgs(this, this.Mode));

            this.WriteLine("Initializing Sitecore...");

            app.Add("bootcamp-done", new object());
          };

          new Thread(action).Start();
        }
      }

      this.PrintConsole();
    }

    private void PrintConsole()
    {
      var app = this.Page.Application;
      Assert.IsNotNull(app, "app");

      var line = 0;
      while (app["bootcamp-done"] == null)
      {
        try
        {
          for (var i = line; i < Messages.Count; ++i)
          {
            this.WriteLine(Messages[i], true);
            line++;
          }
        }
        catch
        {
        }

        Thread.Sleep(500);
      }

      this.Page.Response.Close();
    }
  }
}