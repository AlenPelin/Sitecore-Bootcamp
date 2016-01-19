namespace Sitecore.Bootcamp.Core.Processors
{
  using System.IO;
  using Sitecore.Diagnostics.Base;

  internal class MoveWebConfig : Processor
  {
    internal override void Process(ProcessorArgs args)
    {
      Assert.ArgumentNotNull(args, "args");

      var webConfigToDeploy = args.Server.MapPath("/App_Config/web.config");
      var webConfig = args.Server.MapPath("/web.config");
      if (!File.Exists(webConfigToDeploy))
      {
        return;
      }

      args.WriteLine("Moving /App_Config/web.config to /web.config");

      if (File.Exists(webConfig))
      {
        File.Delete(webConfig);
      }

      File.Move(webConfigToDeploy, webConfig);
    }
  }
}