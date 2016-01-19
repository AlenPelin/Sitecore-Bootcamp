namespace Sitecore.Bootcamp.Core.Processors
{
  using Sitecore.Diagnostics.Base;

  internal class SaveConnectionStringsConfig : Processor
  {
    internal override void Process(ProcessorArgs args)
    {
      Assert.ArgumentNotNull(args, "args");

      var connectionStringsPath = args.Server.MapPath("/App_Config/ConnectionStrings.config");
      args.ConnectionStringsConfig.Save(connectionStringsPath);
    }
  }
}