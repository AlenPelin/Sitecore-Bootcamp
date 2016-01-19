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

    internal ProcessorArgs([NotNull] BootcampCore bootcampCore, BootcampMode mode)
    {
      Assert.ArgumentNotNull(bootcampCore, "bootcampCore");

      this.BootcampCore = bootcampCore;
      this.Mode = mode;
      this.Server = bootcampCore.Page.Server;
    }

    [CanBeNull]
    internal IRelease Release { get; set; }

    internal void WriteLine([NotNull] string message, bool bypassNoisy = false)
    {
      Assert.ArgumentNotNull(message, "message");

      this.BootcampCore.WriteLine(message, false, bypassNoisy);
    }
  }
}