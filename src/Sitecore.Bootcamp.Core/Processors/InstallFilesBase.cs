namespace Sitecore.Bootcamp.Processors
{
  using System;
  using System.IO;
  using Ionic.Zip;
  using Sitecore.Diagnostics.Base;
  using Sitecore.Diagnostics.Base.Annotations;

  internal abstract class InstallFilesBase : Processor
  {
    [NotNull]
    protected ZipFile GetFileSetZip([NotNull] ProcessorArgs args, [NotNull] string type)
    {
      Assert.ArgumentNotNull(args, "args");
      Assert.ArgumentNotNull(type, "type");

      var release = args.Release;

      Assert.IsNotNull(release, "release");

      Assert.ArgumentNotNull(release, "release");

      args.WriteLine("Downloading " + type + " files...");

      var fileInfo = release.Defaults.Files[type].Download();

      var zipFilePath = fileInfo.FullName;
      args.WriteLine("Downloaded to " + zipFilePath);

      ZipFile zip;

      try
      {
        zip = new ZipFile(zipFilePath);
      }
      catch
      {
        args.WriteLine("Cached " + type + " files are corrupted. Re-downloading...");

        File.Delete(zipFilePath);

        fileInfo = release.Defaults.Files[type].Download();

        zipFilePath = fileInfo.FullName;
        args.WriteLine("Downloaded to " + zipFilePath);

        zip = new ZipFile(zipFilePath);
      }

      return zip;
    }

    protected void ExtractFileSet([NotNull] ProcessorArgs args, [NotNull] ZipFile zip, [NotNull] string type)
    {
      Assert.ArgumentNotNull(args, "args");
      Assert.ArgumentNotNull(zip, "zip");
      Assert.ArgumentNotNull(type, "type");

      args.WriteLine("Extracting " + type + " files...");

      using (zip)
      {
        foreach (var entry in zip.SelectEntries("*") ?? new ZipEntry[0])
        {
          if (entry == null)
          {
            continue;
          }

          var fileName = entry.FileName;
          var prefix = "Website/";
          var pos = fileName.IndexOf(prefix);
          if (pos < 0)
          {
            continue;
          }

          var virtualPath = fileName.Substring(pos + prefix.Length);
          var filePath = args.Server.MapPath(virtualPath);
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
              args.WriteLine("Failed.");
              args.WriteLine("- Exception: " + ex.GetType().FullName);
              args.WriteLine("- Message: " + ex.Message);
              args.WriteLine("- StackTrace: " + ex.StackTrace.Replace("\n", "<br />"));
            }
          }
        }
      }
    }
  }
}