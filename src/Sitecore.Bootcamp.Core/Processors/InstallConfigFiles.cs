namespace Sitecore.Bootcamp.Core.Processors
{
  using System;
  using System.IO;
  using Ionic.Zip;
  using Sitecore.Diagnostics.Base;
  using Sitecore.Diagnostics.Base.Annotations;
  using Sitecore.Diagnostics.InformationService.Client;

  internal class InstallConfigFiles : Processor
  {
    internal override void Process(ProcessorArgs args)
    {
      Assert.ArgumentNotNull(args, "args");

      var webConfig = args.Server.MapPath("/web.config");
      var mimeTypes = args.Server.MapPath("/App_Config/MimeTypes.config");
      var globalAsax = args.Server.MapPath("/global.asax");
      {
        var webConfigNeeded = !File.Exists(webConfig);
        var appConfigNeeded = !File.Exists(mimeTypes); // if there is no MimeTypes then no other app_config files
        var globalAsaxNeeded = !File.Exists(globalAsax);

        if (!webConfigNeeded && !appConfigNeeded && !globalAsaxNeeded)
        {
          return;
        }

        args.WriteLine("Extracting config files...");

        var release = args.Release;
        Assert.IsNotNull(release, "release");

        var confiFilesUrl = release.Defaults.Configs.FilesUrl;
        var configFilesZip = new ConfigFiles(confiFilesUrl).Download();
        using (var zip = new ZipFile(configFilesZip.FullName))
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
              var filePath = args.Server.MapPath("/" + virtualPath.TrimStart('/'));
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

    private class ConfigFiles : CacheableObject
    {
      [NotNull]
      private readonly string FilesUrl;

      internal ConfigFiles([NotNull] string filesUrl)
      {
        Assert.ArgumentNotNull(filesUrl, "filesUrl");

        this.FilesUrl = filesUrl;
      }

      [NotNull]
      internal FileInfo Download()
      {
        return new FileInfo(this.GetFilePath(this.FilesUrl));
      }
    }
  }
}