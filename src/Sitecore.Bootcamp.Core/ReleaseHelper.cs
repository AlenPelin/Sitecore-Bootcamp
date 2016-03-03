namespace Sitecore.Bootcamp.Core
{
  using System.Diagnostics;
  using System.IO;
  using System.Linq;
  using Sitecore.Bootcamp.Core.Processors;
  using Sitecore.Diagnostics.Base;
  using Sitecore.Diagnostics.Base.Annotations;
  using Sitecore.Diagnostics.InformationService.Client;
  using Sitecore.Diagnostics.InformationService.Client.Model;

  internal static class ReleaseHelper
  {
    internal static IRelease GetRelease(ProcessorArgs args)
    {
      args.WriteLine("Downloading metadata...");

      var kernelVersion = GetKernelVersion(args);
      var versionName = GetVersionName(kernelVersion);
      var version = GetVersion(versionName);
      var releaseName = GetReleaseName(kernelVersion);
      var release = GetRelease(version, releaseName);

      return release;
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
    private static string GetKernelVersion([NotNull] ProcessorArgs args)
    {
      Assert.ArgumentNotNull(args, "args");

      var kernelPath = args.Server.MapPath("bin\\Sitecore.Kernel.dll");
      if (!File.Exists(kernelPath))
      {
        kernelPath = args.Server.MapPath("App_Bin\\Sitecore.Kernel.dll");

        Assert.IsTrue(File.Exists(kernelPath), "Cannot find Sitecore.Kernel.dll in both bin and App_Bin folders.");
      }

      var kernelVersion = FileVersionInfo.GetVersionInfo(kernelPath).ProductVersion;
      Assert.IsNotNullOrEmpty(kernelVersion, "kernelVersion");

      return kernelVersion;
    }

    [NotNull]
    private static IRelease GetRelease([NotNull] IVersion version, [NotNull] string revisionName)
    {
      Assert.ArgumentNotNull(version, "version");
      Assert.ArgumentNotNull(revisionName, "revisionName");

      var revision = version.Releases.FirstOrDefault(x => x.Revision == revisionName);
      Assert.IsNotNull(revision, "revision");

      return revision;
    }

    [NotNull]
    private static string GetReleaseName([NotNull] string kernelVersion)
    {
      Assert.ArgumentNotNull(kernelVersion, "kernelVersion");

      var revisionName = kernelVersion.Substring(kernelVersion.LastIndexOf(' ') + 1);
      Assert.IsNotNullOrEmpty(revisionName, "revisionName");

      return revisionName;
    }
  }
}