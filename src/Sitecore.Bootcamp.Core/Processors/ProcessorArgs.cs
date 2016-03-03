namespace Sitecore.Bootcamp.Core.Processors
{
  using System.Collections.Generic;
  using System.Web;
  using System.Xml;
  using Sitecore.Diagnostics.Base;
  using Sitecore.Diagnostics.Base.Annotations;
  using Sitecore.Diagnostics.InformationService.Client.Model;

  internal class ProcessorArgs
  {
    [NotNull]
    internal readonly List<string> AddedConnectionStrings = new List<string>();

    [NotNull]
    internal readonly XmlDocument ConnectionStringsConfig = new XmlDocument();

    [NotNull]
    internal readonly HttpServerUtility Server;

    internal readonly BootcampMode Mode;

    [NotNull]
    private readonly BootcampCore BootcampCore;

    [CanBeNull]
    private IRelease ReleaseCache;

    internal ProcessorArgs([NotNull] BootcampCore bootcampCore, BootcampMode mode)
    {
      Assert.ArgumentNotNull(bootcampCore, "bootcampCore");

      this.BootcampCore = bootcampCore;
      this.Mode = mode;
      this.Server = bootcampCore.Page.Server;
    }

    [NotNull]
    internal IRelease Release
    {
      get
      {
        var releaseCache = ReleaseCache;
        if (releaseCache != null)
        {
          return releaseCache;
        }

        lock (this)
        {
          releaseCache = this.ReleaseCache;
          if (releaseCache != null)
          {
            return releaseCache;
          }

          releaseCache = ReleaseHelper.GetRelease(this);

          this.ReleaseCache = releaseCache;
        }

        return releaseCache;
      }
    }

    internal void WriteLine([NotNull] string message, bool bypassNoisy = false)
    {
      Assert.ArgumentNotNull(message, "message");

      this.BootcampCore.WriteLine(message, bypassNoisy);
    }
  }
}