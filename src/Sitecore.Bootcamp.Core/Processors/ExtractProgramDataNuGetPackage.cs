namespace Sitecore.Bootcamp.Core.Processors
{
  using System;
  using System.IO;
  using Sitecore.Diagnostics.Base;
  using Sitecore.Diagnostics.Base.Annotations;
  using Sitecore.Diagnostics.InformationService.Client.Model;

  internal class ExtractProgramDataNuGetPackage : ExtractNuGetPackageBase
  {
    internal override void Process(ProcessorArgs args)
    {
      Assert.ArgumentNotNull(args, "args");

      var release = args.Release;
      Assert.IsNotNull(release, "release");

      var programDataNuget = this.GetNuGet(release);
      if (File.Exists(programDataNuget))
      {
        this.ExtractNuGetPackage(args, programDataNuget);
      }
    }

    [NotNull]
    private string GetNuGet([NotNull] IRelease release)
    {
      Assert.ArgumentNotNull(release, "release");

      var version = release.Version;
      if (version.Length == "x.x".Length)
      {
        version += ".0";
      }

      return Environment.ExpandEnvironmentVariables(string.Format("%PROGRAMDATA%\\Sitecore\\NuGet\\{0}.{1}\\SC.{0}.{1}.nupkg", version, release.Revision));
    }
  }
}