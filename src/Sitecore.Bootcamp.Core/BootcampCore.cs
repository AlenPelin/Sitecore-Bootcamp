namespace Sitecore.Bootcamp.Core
{
  using System;
  using System.Collections.Generic;
  using System.Diagnostics;
  using System.IO;
  using System.Linq;
  using System.Reflection;
  using System.Threading;
  using System.Web.UI;
  using Ionic.Zip;
  using Sitecore.Diagnostics.Base;
  using Sitecore.Diagnostics.Base.Annotations;
  using Sitecore.Diagnostics.InformationService.Client;
  using Sitecore.Diagnostics.InformationService.Client.Model;

  public class BootcampCore
  {
    [NotNull]
    private static readonly object SyncRoot = new object();

    [NotNull]
    private static readonly List<string> Messages = new List<string>();

    private static bool installationStarted;

    private static bool installationFinished;

    [NotNull]
    private readonly Page Page;

    [NotNull]
    private readonly string[] Packages;

    public BootcampCore([NotNull] Page page, [NotNull] params string[] packages)
    {
      Assert.ArgumentNotNull(page, "page");
      Assert.ArgumentNotNull(packages, "packages");

      this.Page = page;
      this.Packages = packages;
    }

    public bool Process()
    {
      try
      {
        return this.ProcessInner();
      }
      catch (Exception ex)
      {
        this.WriteLine("Error happened. " + ex.Message + "<br />Exception: " + ex.GetType().FullName + "<br />StackTrace: " + ex.StackTrace.Replace(Environment.NewLine, "<br />"));

        return true;
      }
    }

    private bool ProcessInner()
    {
      this.Page.Server.ScriptTimeout = int.MaxValue;
      this.Page.Response.StatusCode = 503;

      if (this.Page.Request.QueryString["install"] == null)
      {
        return true;
      }

      this.Page.Response.Write("<html><head><title>Installing Sitecore...</title><style>body { font-family: consolas, courier new; }</style></head><body>");

      if (!installationStarted)
      {
        lock (SyncRoot)
        {
          if (!installationStarted)
          {
            installationStarted = true;

            this.WriteLine("Extracting Sitecore assemblies...");

            // 1. Extract SC.*.nupkg
            var nugetPackages = Directory.GetFiles(this.Page.Server.MapPath("/"), "*.nupkg", SearchOption.AllDirectories);
            foreach (var package in nugetPackages)
            {
              using (var zip = new ZipFile(package))
              {
                foreach (var zipEntry in zip.Entries)
                {
                  if (zipEntry == null || zipEntry.FileName == null || !zipEntry.FileName.StartsWith("lib/"))
                  {
                    continue;
                  }

                  var filePath = this.Page.Server.MapPath("/bin/" + zipEntry.FileName.Substring("lib/".Length));
                  if (File.Exists(filePath))
                  {
                    continue;
                  }

                  var dir = Path.GetDirectoryName(filePath);
                  if (!Directory.Exists(dir))
                  {
                    Directory.CreateDirectory(dir);
                  }

                  using (var file = File.OpenWrite(filePath))
                  {
                    zipEntry.Extract(file);
                  }
                }
              }

              File.Delete(package);
            }

            // 2. Copy Ninject.dll
            var sourceNinject = this.Page.MapPath("/bin/Ninject.dll");
            if (File.Exists(sourceNinject))
            {
              var targetNinject = this.Page.MapPath("/bin/Social/Ninject.dll");
              if (!File.Exists(targetNinject))
              {
                var dir = Path.GetDirectoryName(targetNinject);
                if (!Directory.Exists(dir))
                {
                  Directory.CreateDirectory(dir);
                }

                File.Copy(sourceNinject, targetNinject);
              }
            }

            // 3. Prepare App_Data
            var appData = this.Page.Server.MapPath("/App_Data");
            var appDataFolders = new[]
            {
              "debug", "diagnostics", "indexes", "logs", "packages", "serialization", "submit queue", "tools", "viewstate"
            };

            foreach (var name in appDataFolders)
            {
              var dir = Path.Combine(appData, name);
              if (!Directory.Exists(dir))
              {
                Directory.CreateDirectory(dir);
              }
            }

            this.WriteLine("Downloading metadata...");

            var release = this.GetRelease();

            // 4. Download and extract default files
            using (var def = this.GetFileSetZip(release, "default"))
            {
              this.ExtractFileSet(def, "default");
            }

            // 5. Download and extract client files
            foreach (var package in this.Packages)
            {
              using (var zip = this.GetFileSetZip(release, package))
              {
                this.ExtractFileSet(zip, package);
              }
            }

            // 6. Replace web.config
            var webConfigToDeploy = this.Page.Server.MapPath("/App_Config/web.config");
            if (File.Exists(webConfigToDeploy))
            {
              this.WriteLine("Moving /App_Config/web.config to /web.config");

              var webConfig = this.Page.Server.MapPath("/web.config");
              if (File.Exists(webConfig))
              {
                File.Delete(webConfig);
              }

              File.Move(webConfigToDeploy, webConfig);
            }

            this.Page.Response.Write("<script>setTimeout(function(){ document.location.href=document.location.protocol + '//' + document.location.hostname;},5000);</script></body></html>");

            // 7. Deleting install files
            this.DeleteInstallFiles();

            installationFinished = true;
            this.Page.Response.Close();

            return true;
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

      return false;
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
          var pos = fileName.IndexOf(website);
          if (pos < 0)
          {
            continue;
          }

          var virtualPath = fileName.Substring(pos + website.Length);
          var filePath = this.Page.Server.MapPath(virtualPath);
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
      this.WriteLine("Deleting installer files...");

      File.Delete(this.Page.Server.MapPath("bin\\Sitecore.Bootcamp.Core.dll"));

      var shell = this.Page.Server.MapPath("bin\\Sitecore.Bootcamp.dll");
      if (File.Exists(shell))
      {
        File.Delete(shell);
      }

      File.Delete(this.Page.Server.MapPath("Default.aspx"));
    }

    private void WriteLine([NotNull] string message, bool skipCache = false)
    {
      Assert.ArgumentNotNull(message, "message");

      if (!skipCache)
      {
        Messages.Add(message);
      }

      this.Page.Response.Write(message);
      this.Page.Response.Write("<br />");
      this.Page.Response.Flush();
    }

    [NotNull]
    private IRelease GetRelease()
    {
      var kernelVersion = this.GetKernelVersion();
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
      var kernelVersion = FileVersionInfo.GetVersionInfo(this.Page.Server.MapPath("bin\\Sitecore.Kernel.dll")).ProductVersion;
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