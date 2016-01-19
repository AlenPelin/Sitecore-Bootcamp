using System.IO;

namespace Sitecore.Bootcamp.Core.Processors
{
  using Sitecore.Diagnostics.Base;

  internal class SaveConnectionStringsConfig : Processor
  {
    internal override void Process(ProcessorArgs args)
    {
      Assert.ArgumentNotNull(args, "args");

      var connectionStringsPath = args.Server.MapPath("/App_Config/ConnectionStrings.config");
      var dir = Path.GetDirectoryName(connectionStringsPath);
      if (!Directory.Exists(dir))
      {
        Directory.CreateDirectory(dir);
      }

      args.ConnectionStringsConfig.Save(connectionStringsPath);
    }
  }
}