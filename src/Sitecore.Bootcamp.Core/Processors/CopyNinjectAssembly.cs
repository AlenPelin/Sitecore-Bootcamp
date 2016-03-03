namespace Sitecore.Bootcamp.Core.Processors
{
  using System.IO;
  using Sitecore.Diagnostics.Base;

  internal class CopyNinjectAssembly : Processor
  {
    internal override void Process(ProcessorArgs args)
    {
      Assert.ArgumentNotNull(args, "args");

      var sourceNinject = args.Server.MapPath("/bin/Ninject.dll");
      if (!File.Exists(sourceNinject))
      {
        return;
      }

      var targetNinject = args.Server.MapPath("/bin/Social/Ninject.dll");
      if (!File.Exists(targetNinject))
      {
        var dir = Path.GetDirectoryName(targetNinject);
        if (!Directory.Exists(dir))
        {
          Directory.CreateDirectory(dir);
        }

        File.Copy(sourceNinject, targetNinject);
      }
    }
  }
}