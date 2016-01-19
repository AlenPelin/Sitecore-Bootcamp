namespace Sitecore.Bootcamp.Core
{
  using System;
  using System.Collections.Generic;
  using System.Data.SqlClient;
  using System.Diagnostics;
  using System.IO;
  using System.Linq;
  using System.Threading;
  using System.Web.UI;
  using System.Xml;
  using Ionic.Zip;
  using Sitecore.Diagnostics.Base;
  using Sitecore.Diagnostics.Base.Annotations;
  using Sitecore.Diagnostics.InformationService.Client;
  using Sitecore.Diagnostics.InformationService.Client.Model;
  using Sitecore.Diagnostics.InformationService.Client.Model.Defaults;

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

      // check license
      var license = this.Page.Server.MapPath("/App_Data/License.xml");
      if (!File.Exists(license))
      {
        this.WriteLine("No license.xml file is detected in /App_Data folder");
        return false;
      }
      
      if (!installationStarted)
      {
        lock (SyncRoot)
        {
          if (!installationStarted)
          {
            installationStarted = true;

            this.WriteLine("Downloading metadata...");

            var release = this.GetRelease();

            this.WriteLine("Extracting Sitecore assemblies...");

            // 1. Extract SC.*.nupkg
            var nugetPackages = Directory.GetFiles(this.Page.Server.MapPath("/"), "*.nupkg", SearchOption.AllDirectories);
            var programDataNuget = this.GetNuGet(release);
            if (!nugetPackages.Any())
            {
              if (File.Exists(programDataNuget))
              {
                nugetPackages = new[] { programDataNuget };
              }
            }

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

              if (package != programDataNuget)
              {
                File.Delete(package);
              }
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
              "debug", "diagnostics", "indexes", "logs", "packages", "serialization", "submit queue", "tools", "viewstate", "databases"
            };

            var connectionStrings = new List<string>();
            var connectionStringsPath = this.Page.Server.MapPath("/App_Config/ConnectionStrings.config");
            XmlDocument connectionStringsXml = new XmlDocument();
            if (File.Exists(connectionStringsPath))
            {
              connectionStringsXml.Load(connectionStringsPath);
              foreach (var addNode in connectionStringsXml.DocumentElement.ChildNodes.OfType<XmlElement>())
              {
                var name = addNode.GetAttribute("name");
                if (!string.IsNullOrEmpty(name))
                {
                  connectionStrings.Add(name);
                }
              }
            }
            else
            {
              connectionStringsXml.LoadXml("<connectionStrings />");
            }

            var defaultDatabases = release.Defaults.Databases;
            var sqlConnectionStrings = defaultDatabases.Keys.Select(x => x == "analytics" ? "reporting" : x).ToArray();

            var mongoConnectionStrings = new[]
            {
              "analytics",
              "tracking.live",
              "tracking.history",
              "tracking.contact",
            };

            var defaultFiles = release.Defaults.Files;
            foreach (var connectionStringName in sqlConnectionStrings)
            {
              var name = connectionStringName;

              if (name == null)
              {
                continue;
              }

              if (connectionStrings.Contains(name))
              {
                continue;
              }

              var db = GetDefaultDatabase(defaultDatabases, name);

              var filename = db.FileName;
              var filePath = this.Page.Server.MapPath(string.Format("/App_Data/Databases/{0}", filename));
              var dir = Path.GetDirectoryName(filePath);
              if (!Directory.Exists(dir))
              {
                Directory.CreateDirectory(dir);
              }

              if (name == "web")
              {
                continue;
              }

              if (!File.Exists(filePath))
              {
                this.WriteLine(string.Format("Downloading {0} database...", name));

                var tmp = Path.GetTempFileName() + ".dir";

                try
                {
                  var file = GetDefaultFile(defaultFiles, name);

                  var downloadedFile = file.Download().FullName;

                  this.WriteLine("Downloaded to " + downloadedFile);

                  this.WriteLine("Extracting " + name + " database files...");

                  using (var zip = new ZipFile(downloadedFile))
                  {
                    zip.ExtractAll(tmp);
                  }

                  var extractedFile = Directory.GetFiles(tmp, filename, SearchOption.AllDirectories).First();
                  File.Move(extractedFile, filePath);
                }
                finally
                {
                  if (Directory.Exists(tmp))
                  {
                    Directory.Delete(tmp, true);
                  }
                }
              }

              var cstr = new SqlConnectionStringBuilder
              {
                IntegratedSecurity = true,
                UserInstance = true,
                DataSource = ".\\SQLEXPRESS",
                AttachDBFilename = filePath
              };

              var addNode = connectionStringsXml.CreateElement("add");
              addNode.SetAttribute("name", name);
              addNode.SetAttribute("connectionString", cstr.ToString());
              connectionStringsXml.DocumentElement.AppendChild(addNode);
              connectionStrings.Add(name);
            }

            if (sqlConnectionStrings.Contains("web"))
            {
              var webDatabasePath = this.Page.Server.MapPath(string.Format("/App_Data/Databases/{0}", this.GetDefaultDatabase(defaultDatabases, "web").FileName));
              var masterDatabasePath = this.Page.Server.MapPath(string.Format("/App_Data/Databases/{0}", this.GetDefaultDatabase(defaultDatabases, "master").FileName));
              if (!File.Exists(webDatabasePath) && File.Exists(masterDatabasePath))
              {
                this.WriteLine("Copying master database as web...");

                File.Copy(masterDatabasePath, webDatabasePath);

                var cstr = new SqlConnectionStringBuilder
                {
                  IntegratedSecurity = true,
                  UserInstance = true,
                  DataSource = ".\\SQLEXPRESS",
                  AttachDBFilename = webDatabasePath
                };

                var addNode = connectionStringsXml.CreateElement("add");
                addNode.SetAttribute("name", "web");
                addNode.SetAttribute("connectionString", cstr.ToString());
                connectionStringsXml.DocumentElement.AppendChild(addNode);
                connectionStrings.Add("web");
              }
            }

            foreach (var name in mongoConnectionStrings)
            {
              if (connectionStrings.Contains(name))
              {
                continue;
              }
              
              var addNode = connectionStringsXml.CreateElement("add");
              addNode.SetAttribute("name", name);
              addNode.SetAttribute("connectionString", string.Format("mongodb://localhost:27017/{0}_{1}", this.Page.Server.MapPath("/").Replace("\\", "_").Replace(":", ""), name));
              connectionStringsXml.DocumentElement.AppendChild(addNode);
              connectionStrings.Add(name);
            }

            connectionStringsXml.Save(connectionStringsPath);

            foreach (var name in appDataFolders)
            {
              var dir = Path.Combine(appData, name);
              if (!Directory.Exists(dir))
              {
                Directory.CreateDirectory(dir);
              }
            }

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
            var webConfig = this.Page.Server.MapPath("/web.config");
            if (File.Exists(webConfigToDeploy))
            {
              this.WriteLine("Moving /App_Config/web.config to /web.config");

              if (File.Exists(webConfig))
              {
                File.Delete(webConfig);
              }

              File.Move(webConfigToDeploy, webConfig);
            }

            var mimeTypes = this.Page.Server.MapPath("/App_Config/MimeTypes.config");
            var globalAsax = this.Page.Server.MapPath("/global.asax");
            {
              var webConfigNeeded = !File.Exists(webConfig);
              var appConfigNeeded = !File.Exists(mimeTypes); // if there is no mimetyppes then no other app_config files
              var globalAsaxNeeded = !File.Exists(globalAsax);
              if (webConfigNeeded || appConfigNeeded || globalAsaxNeeded)
              {
                this.WriteLine("Extracting config files...");

                var filesUrl = release.Defaults.Configs.FilesUrl;
                var files = new ConfigFiles(filesUrl);
                using (var zip = new ZipFile(files.Download().FullName))
                {
                  foreach (var zipEntry in zip.Entries ?? new ZipEntry[0])
                  {
                    if (zipEntry == null)
                    {
                      continue;
                    }

                    var virtualPath = zipEntry.FileName ?? string.Empty;
                    if (webConfigNeeded && virtualPath.Equals("web.config", StringComparison.OrdinalIgnoreCase))
                    {
                      using (var stream = File.OpenWrite(webConfig))
                      {
                        zipEntry.Extract(stream);
                      }

                      continue;
                    }

                    if (globalAsaxNeeded && virtualPath.Equals("global.asax", StringComparison.OrdinalIgnoreCase))
                    {
                      using (var stream = File.OpenWrite(globalAsax))
                      {
                        zipEntry.Extract(stream);
                      }

                      continue;
                    }

                    if (appConfigNeeded && virtualPath.StartsWith("App_Config", StringComparison.OrdinalIgnoreCase))
                    {
                      var filePath = this.Page.Server.MapPath("/" + virtualPath.TrimStart('/'));
                      if (!File.Exists(filePath))
                      {
                        var dir = Path.GetDirectoryName(filePath);
                        if (!Directory.Exists(dir))
                        {
                          Directory.CreateDirectory(dir);
                        }

                        using (var stream = File.OpenWrite(filePath))
                        {
                          zipEntry.Extract(stream);
                        }
                      }
                    }
                  }
                }
              }
            }

            var dataFolder = this.Page.Server.MapPath("/App_Data/Include/zzz/DataFolder.config");
            if (!File.Exists(dataFolder))
            {
              var dir = Path.GetDirectoryName(dataFolder);
              if (!Directory.Exists(dir))
              {
                Directory.CreateDirectory(dir);
              }

              File.WriteAllText(dataFolder, @"<configuration xmlns:set=""http://www.sitecore.net/xmlconfig/set/""><sitecore><sc.variable name=""dataFolder"" set:value=""/App_Data"" /></sitecore></configuration>");
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

    [NotNull]
    private FileSet GetDefaultFile([NotNull] IDictionary<string, FileSet> defaultFiles, [NotNull] string name)
    {
      Assert.ArgumentNotNull(defaultFiles, "defaultFiles");
      Assert.ArgumentNotNull(name, "name");

      if (name == "reporting")
      {
        name = "analytics";
      }

      var key = string.Format("sitecore_{0}", name);
      Assert.IsTrue(defaultFiles.ContainsKey(key), "The defaultFiles does not contain {0} key", key);

      var file = defaultFiles[key];
      Assert.IsNotNull(file, "The defaultFiles[\"{0}\"] is null", key);

      return file;
    }

    [NotNull]
    private Database GetDefaultDatabase([NotNull] IDictionary<string, Database> defaultDatabases, [NotNull] string name)
    {
      Assert.ArgumentNotNull(defaultDatabases, "defaultDatabases");
      Assert.ArgumentNotNull(name, "name");

      if (name == "reporting")
      {
        name = "analytics";
      }

      Assert.IsTrue(defaultDatabases.ContainsKey(name), "The defaultDatabases doesn't contain {0} key", name);

      var db = defaultDatabases[name];
      Assert.IsNotNull(db, "The defaultDatabases[\"{0}\"] is null", name);

      return db;
    }

    private string GetNuGet(IRelease release)
    {
      var version = release.Version;
      if (version.Length == "x.x".Length)
      {
        version += ".0";
      }

      return Environment.ExpandEnvironmentVariables(string.Format("%PROGRAMDATA%\\Sitecore\\NuGet\\{0}.{1}\\SC.{0}.{1}.nupkg", version, release.Revision));
    }

    private class ConfigFiles : CacheableObject
    {
      [NotNull]
      private readonly string FilesUrl;

      public ConfigFiles(string filesUrl)
      {
        this.FilesUrl = filesUrl;
      }

      [NotNull]
      public FileInfo Download()
      {
        return new FileInfo(this.GetFilePath(this.FilesUrl));
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