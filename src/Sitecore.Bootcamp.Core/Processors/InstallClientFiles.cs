namespace Sitecore.Bootcamp.Core.Processors
{
  using Sitecore.Diagnostics.Base;

  internal class InstallClientFiles : InstallFilesBase
  {
    internal override void Process(ProcessorArgs args)
    {
      Assert.ArgumentNotNull(args, "args");

      if (args.Mode != BootcampMode.Everything)
      {
        return;
      }

      using (var zip = this.GetFileSetZip(args, "client"))
      {
        this.ExtractFileSet(args, zip, "client");
      }
    }
  }
}