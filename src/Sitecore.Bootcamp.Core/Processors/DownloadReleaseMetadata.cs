namespace Sitecore.Bootcamp.Core.Processors
{
  using System.Diagnostics;
  using System.Linq;
  using Sitecore.Diagnostics.Base;
  using Sitecore.Diagnostics.Base.Annotations;
  using Sitecore.Diagnostics.InformationService.Client;
  using Sitecore.Diagnostics.InformationService.Client.Model;

  internal class DownloadReleaseMetadata : Processor
  {
    internal override void Process(ProcessorArgs args)
    {
      Assert.ArgumentNotNull(args, "args");

      args.WriteLine("Downloading metadata...");

      var kernelVersion = this.GetKernelVersion(args);
      var versionName = this.GetVersionName(kernelVersion);
      var version = this.GetVersion(versionName);
      var releaseName = this.GetReleaseName(kernelVersion);
      var release = this.GetRelease(version, releaseName);

      args.Release = release;
    }

    [NotNull]
    private IVersion GetVersion([NotNull] string versionName)
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
    private string GetVersionName([NotNull] string kernelVersion)
    {
      Assert.ArgumentNotNull(kernelVersion, "kernelVersion");

      return kernelVersion.Substring(0, kernelVersion.IndexOf(' '));
    }

    [NotNull]
    private string GetKernelVersion([NotNull] ProcessorArgs args)
    {
      Assert.ArgumentNotNull(args, "args");

      var kernelVersion = FileVersionInfo.GetVersionInfo(args.Server.MapPath("bin\\Sitecore.Kernel.dll")).ProductVersion;
      Assert.IsNotNullOrEmpty(kernelVersion, "kernelVersion");

      return kernelVersion;
    }

    [NotNull]
    private IRelease GetRelease([NotNull] IVersion version, [NotNull] string revisionName)
    {
      Assert.ArgumentNotNull(version, "version");
      Assert.ArgumentNotNull(revisionName, "revisionName");

      var revision = version.Releases.FirstOrDefault(x => x.Revision == revisionName);
      Assert.IsNotNull(revision, "revision");

      return revision;
    }

    [NotNull]
    private string GetReleaseName([NotNull] string kernelVersion)
    {
      Assert.ArgumentNotNull(kernelVersion, "kernelVersion");

      var revisionName = kernelVersion.Substring(kernelVersion.LastIndexOf(' ') + 1);
      Assert.IsNotNullOrEmpty(revisionName, "revisionName");

      return revisionName;
    }
  }
}