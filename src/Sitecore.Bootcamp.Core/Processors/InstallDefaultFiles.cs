namespace Sitecore.Bootcamp.Processors
{
  using Sitecore.Diagnostics.Base;

  internal class InstallDefaultFiles : InstallFilesBase
  {
    internal override void Process(ProcessorArgs args)
    {
      Assert.ArgumentNotNull(args, "args");

      using (var zip = this.GetFileSetZip(args, "default"))
      {
        this.ExtractFileSet(args, zip, "default");
      }
    }
  }
}