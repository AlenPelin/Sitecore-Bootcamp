namespace Sitecore.Bootcamp
{
  using System;
  using System.Collections.Generic;
  using System.Diagnostics;
  using System.IO;
  using System.Linq;
  using System.Threading;
  using Ionic.Zip;
  using Sitecore.Diagnostics.Base;
  using Sitecore.Diagnostics.Base.Annotations;
  using Sitecore.Diagnostics.InformationService.Client;
  using Sitecore.Diagnostics.InformationService.Client.Model;

  public partial class Default : System.Web.UI.Page
  {
    [NotNull]
    private static readonly object SyncRoot = new object();

    [NotNull]
    private static readonly List<string> Messages = new List<string>();

    private static bool installationStarted;

    private static bool installationFinished;

    protected override void OnPreInit([CanBeNull] EventArgs e)
    {
      this.Server.ScriptTimeout = int.MaxValue;
      this.Response.StatusCode = 503;

      if (this.Request.QueryString["install"] == null)
      {
        base.OnPreInit(e);

        return;
      }

      this.Response.Write("<html><head><title>Installing Sitecore...</title><style>body { font-family: consolas, courier new; }</style></head><body>");

      if (!installationStarted)
      {
        lock (SyncRoot)
        {
          if (!installationStarted)
          {
            installationStarted = true;

            this.WriteLine("Downloading metadata...");

            var release = GetRelease();

            ZipFile client = null;

            var installShell = File.Exists(this.Server.MapPath("bin\\Sitecore.Bootcamp.Shell.dll"));
            if (installShell)
            {
              new Thread(() => { client = this.GetFileSetZip(release, "client"); }).Start();
            }

            using (var def = this.GetFileSetZip(release, "default"))
            {
              this.ExtractFileSet(def, "default");
            }

            if (installShell)
            {
              // ReSharper disable once LoopVariableIsNeverChangedInsideLoop
              while (client == null)
              {
                Thread.Sleep(1000);
              }

              this.ExtractFileSet(client, "client");
            }

            var webConfigToDeploy = this.Server.MapPath("/App_Config/web.config");
            if (File.Exists(webConfigToDeploy))
            {
              this.WriteLine("Moving /App_Config/web.config to /web.config");

              var webConfig = this.Server.MapPath("/web.config");
              if (File.Exists(webConfig))
              {
                File.Delete(webConfig);
              }

              File.Move(webConfigToDeploy, webConfig);
            }

            this.DeleteInstallFiles();

            installationFinished = true;

            this.Response.Write("Done.<script>document.location.href='/default.aspx#'</script></body></html>");
            this.Response.Close();

            base.OnPreInit(e);

            return;
          }
        }
      }

      var line = 0;
      while (!installationFinished)
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

        Thread.Sleep(1000);
      }
    }

    private void ExtractFileSet([NotNull] ZipFile zip, [NotNull] string type)
    {
      Assert.ArgumentNotNull(zip, "zip");
      Assert.ArgumentNotNull(type, "type");

      this.WriteLine("Extracting " + type + " files...");

      using (zip)
      {
        foreach (var entry in zip.SelectEntries("*"))
        {
          var fileName = entry.FileName;
          var website = "Website/";
          var virtualPath = fileName.Substring(fileName.IndexOf(website) + website.Length);

          var filePath = this.Server.MapPath(virtualPath);
          if (entry.IsDirectory)
          {
            if (!Directory.Exists(filePath))
            {
              Directory.CreateDirectory(filePath);
            }
          }
          else
          {
            if (File.Exists(filePath))
            {
              continue;
            }

            try
            {
              var directoryPath = Path.GetDirectoryName(filePath);
              if (!Directory.Exists(directoryPath))
              {
                Directory.CreateDirectory(directoryPath);
              }

              using (var file = File.OpenWrite(filePath))
              {
                entry.Extract(file);
              }
            }
            catch (Exception ex)
            {
              this.WriteLine("Failed.");
              this.WriteLine("- Exception: " + ex.GetType().FullName);
              this.WriteLine("- Message: " + ex.Message);
              this.WriteLine("- StackTrace: " + ex.StackTrace.Replace("\n", "<br />"));
            }
          }
        }
      }
    }

    [NotNull]
    private ZipFile GetFileSetZip([NotNull] IRelease release, [NotNull] string type)
    {
      Assert.ArgumentNotNull(release, "release");
      Assert.ArgumentNotNull(type, "type");

      this.WriteLine("Downloading " + type + " files...");

      var fileInfo = release.Defaults.Files[type].Download();

      var zipFilePath = fileInfo.FullName;
      this.WriteLine("Downloaded to " + zipFilePath);

      ZipFile zip;

      try
      {
        zip = new ZipFile(zipFilePath);
      }
      catch
      {
        this.WriteLine("Cached " + type + " files are corrupted. Re-downloading...");

        File.Delete(zipFilePath);

        fileInfo = release.Defaults.Files[type].Download();

        zipFilePath = fileInfo.FullName;
        this.WriteLine("Downloaded to " + zipFilePath);

        zip = new ZipFile(zipFilePath);
      }

      return zip;
    }

    private void DeleteInstallFiles()
    {
      File.Delete(this.Server.MapPath("Default.css"));
      File.Delete(this.Server.MapPath("Default.aspx"));
      File.Delete(this.Server.MapPath("favicon.ico"));
    }
    
    private void WriteLine([NotNull] string message, bool skipCache = false)
    {
      Assert.ArgumentNotNull(message, "message");

      if (!skipCache)
      {
        Messages.Add(message);
      }

      this.Response.Write(message);
      this.Response.Write("<br />");
      this.Response.Flush();
    }

    [NotNull]
    private IRelease GetRelease()
    {
      var kernelVersion = GetKernelVersion();
      var versionName = GetVersionName(kernelVersion);
      var version = GetVersion(versionName);
      var revisionName = GetRevisionName(kernelVersion);
      var revision = GetRevision(version, revisionName);

      return revision;
    }

    [NotNull]
    private static IVersion GetVersion([NotNull] string versionName)
    {
      Assert.ArgumentNotNull(versionName, "versionName");

      var client = new ServiceClient();
      var versions = client.GetVersions("Sitecore CMS").ToArray();

      IVersion version = null;
      while (version == null && versionName.Length > 1)
      {
        version = versions.FirstOrDefault(x => x.Name == versionName);
        versionName = versionName.Substring(0, versionName.Length - 2);
      }
      Assert.IsNotNull(version, "version");
      return version;
    }

    [NotNull]
    private static string GetVersionName([NotNull] string kernelVersion)
    {
      Assert.ArgumentNotNull(kernelVersion, "kernelVersion");

      return kernelVersion.Substring(0, kernelVersion.IndexOf(' '));
    }

    [NotNull]
    private string GetKernelVersion()
    {
      var kernelVersion = FileVersionInfo.GetVersionInfo(this.Server.MapPath("bin\\Sitecore.Kernel.dll")).ProductVersion;
      Assert.IsNotNullOrEmpty(kernelVersion, "kernelVersion");

      return kernelVersion;
    }

    [NotNull]
    private static IRelease GetRevision([NotNull] IVersion version, [NotNull] string revisionName)
    {
      Assert.ArgumentNotNull(version, "version");
      Assert.ArgumentNotNull(revisionName, "revisionName");

      var revision = version.Releases.FirstOrDefault(x => x.Revision == revisionName);
      Assert.IsNotNull(revision, "revision");

      return revision;
    }

    [NotNull]
    private static string GetRevisionName([NotNull] string kernelVersion)
    {
      Assert.ArgumentNotNull(kernelVersion, "kernelVersion");

      var revisionName = kernelVersion.Substring(kernelVersion.LastIndexOf(' ') + 1);
      Assert.IsNotNullOrEmpty(revisionName, "revisionName");

      return revisionName;
    }
  }
}