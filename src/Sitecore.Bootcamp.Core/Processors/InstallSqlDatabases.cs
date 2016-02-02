namespace Sitecore.Bootcamp.Core.Processors
{
  using System.Collections.Generic;
  using System.Configuration;
  using System.Data.SqlClient;
  using System.IO;
  using System.Linq;
  using Ionic.Zip;
  using Sitecore.Diagnostics.Base;
  using Sitecore.Diagnostics.Base.Annotations;
  using Sitecore.Diagnostics.InformationService.Client.Model.Defaults;

  internal class InstallSqlDatabases : InstallDatabaseProcessorBase
  {
    internal override void Process(ProcessorArgs args)
    {
      Assert.ArgumentNotNull(args, "args");

      var release = args.Release;
      Assert.IsNotNull(release, "release");

      var defaultFiles = release.Defaults.Files;
      var defaultDatabases = release.Defaults.Databases;
      var defaultDatabasesNames = defaultDatabases.Keys.Select(x => x == "analytics" ? "reporting" : x).ToArray();

      this.ProcessDatabases(args, defaultDatabasesNames, defaultDatabases, defaultFiles);

      this.ProcessMasterDatabase(args, defaultDatabasesNames, defaultDatabases);
    }

    private void ProcessMasterDatabase([NotNull] ProcessorArgs args, [NotNull] string[] defaultDatabasesNames, [NotNull] IDictionary<string, Database> defaultDatabases)
    {
      Assert.ArgumentNotNull(args, "args");
      Assert.ArgumentNotNull(defaultDatabasesNames, "defaultDatabasesNames");
      Assert.ArgumentNotNull(defaultDatabases, "defaultDatabases");

      var connectionStrings = args.ConnectionStringsConfig;

      if (!defaultDatabasesNames.Contains("web"))
      {
        return;
      }

      var webDatabasePath = args.Server.MapPath(string.Format("/App_Data/Databases/{0}", this.GetDefaultDatabase(defaultDatabases, "web").FileName));
      var masterDatabasePath = args.Server.MapPath(string.Format("/App_Data/Databases/{0}", this.GetDefaultDatabase(defaultDatabases, "master").FileName));
      if (File.Exists(webDatabasePath) || !File.Exists(masterDatabasePath))
      {
        return;
      }

      args.WriteLine("Copying master database as web...");

      File.Copy(masterDatabasePath, webDatabasePath);

      var cstr = new SqlConnectionStringBuilder
      {
        IntegratedSecurity = true,
        UserInstance = true,
        DataSource = ".\\SQLEXPRESS",
        AttachDBFilename = webDatabasePath
      };

      this.AddConnectionString(connectionStrings, "web", cstr.ToString());
    }

    private void ProcessDatabases([NotNull] ProcessorArgs args, [NotNull] string[] defaultDatabasesNames, [NotNull] IDictionary<string, Database> defaultDatabases, [NotNull] IDictionary<string, FileSet> defaultFiles)
    {
      Assert.ArgumentNotNull(args, "args");
      Assert.ArgumentNotNull(defaultDatabasesNames, "defaultDatabasesNames");
      Assert.ArgumentNotNull(defaultDatabases, "defaultDatabases");
      Assert.ArgumentNotNull(defaultFiles, "defaultFiles");

      var connectionStrings = args.ConnectionStringsConfig;

      foreach (var connectionStringName in defaultDatabasesNames)
      {
        var name = connectionStringName;
        if (name == null)
        {
          continue;
        }

        if (args.AddedConnectionStrings.Contains(name))
        {
          continue;
        }

        if (ConfigurationManager.ConnectionStrings[name] != null)
        {
          continue;
        }

        var defaultDatabase = this.GetDefaultDatabase(defaultDatabases, name);
        var fileName = defaultDatabase.FileName;
        var filePath = args.Server.MapPath(string.Format("/App_Data/Databases/{0}", fileName));
        var dir = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(dir))
        {
          Directory.CreateDirectory(dir);
        }

        // web database is processed separately
        if (name == "web")
        {
          continue;
        }

        if (!File.Exists(filePath))
        {
          args.WriteLine(string.Format("Downloading {0} database...", name));

          var tmp = Path.GetTempFileName() + ".dir";

          try
          {
            var file = this.GetDefaultFile(defaultFiles, name);

            var downloadedFile = file.Download().FullName;

            args.WriteLine("Downloaded to " + downloadedFile);

            args.WriteLine("Extracting " + name + " database files...");

            using (var zip = new ZipFile(downloadedFile))
            {
              zip.ExtractAll(tmp);
            }

            var extractedFile = Directory.GetFiles(tmp, fileName, SearchOption.AllDirectories).First();
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

        this.AddConnectionString(connectionStrings, name, cstr.ToString());
      }
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
  }
}