namespace Sitecore.Bootcamp.Core.Processors
{
  using System.IO;
  using Ionic.Zip;
  using Sitecore.Diagnostics.Base;
  using Sitecore.Diagnostics.Base.Annotations;

  internal abstract class ExtractNuGetPackageBase : Processor
  {
    protected void ExtractNuGetPackage([NotNull] ProcessorArgs args, [NotNull] string package)
    {
      Assert.ArgumentNotNull(args, "args");
      Assert.ArgumentNotNull(package, "package");

      using (var zip = new ZipFile(package))
      {
        foreach (var zipEntry in zip.Entries ?? new ZipEntry[0])
        {
          if (zipEntry == null || zipEntry.FileName == null || !zipEntry.FileName.StartsWith("lib/"))
          {
            continue;
          }

          var filePath = args.Server.MapPath("/bin/" + zipEntry.FileName.Substring("lib/".Length));
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
    }
  }
}