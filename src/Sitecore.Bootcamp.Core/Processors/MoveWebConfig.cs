namespace Sitecore.Bootcamp.Core.Processors
{
  using System.IO;
  using Sitecore.Diagnostics.Base;

  internal class MoveWebConfig : Processor
  {
    internal override void Process(ProcessorArgs args)
    {
      Assert.ArgumentNotNull(args, "args");

      var root = args.Server.MapPath("/");
      var targetFolder = Path.Combine(root, "Web_Config/Include");
      var webConfig = Path.Combine(root, "web.config");
      if (!File.Exists(webConfig))
      {
        return;
      }

      args.WriteLine("Moving /App_Config/web.config to /web.config");

      var targetWebConfig = Path.Combine(targetFolder, "!root_web_config.config");
      if(File.Exists(targetWebConfig))
      {
        File.Delete(targetWebConfig);
      }

      File.Move(webConfig, targetWebConfig);
    }
  }
}