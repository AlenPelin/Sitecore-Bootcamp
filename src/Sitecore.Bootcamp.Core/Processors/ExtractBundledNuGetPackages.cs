namespace Sitecore.Bootcamp.Core.Processors
{
  using System.IO;
  using Sitecore.Diagnostics.Base;

  internal class ExtractBundledNuGetPackages : ExtractNuGetPackageBase
  {
    internal override void Process(ProcessorArgs args)
    {
      Assert.ArgumentNotNull(args, "args");

      args.WriteLine("Extracting assemblies bundled in *.nupkg files...");

      var nugetPackages = Directory.GetFiles(args.Server.MapPath("/"), "*.nupkg", SearchOption.AllDirectories);
      
      foreach (var package in nugetPackages)
      {
        this.ExtractNuGetPackage(args, package);

        File.Delete(package);
      }
    }
  }
}