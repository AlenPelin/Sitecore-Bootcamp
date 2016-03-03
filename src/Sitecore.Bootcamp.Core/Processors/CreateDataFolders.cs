namespace Sitecore.Bootcamp.Processors
{
  using System.IO;
  using Sitecore.Diagnostics.Base;

  internal class CreateDataFolders : Processor
  {
    internal override void Process(ProcessorArgs args)
    {
      Assert.ArgumentNotNull(args, "args");

      var appData = args.Server.MapPath("/App_Data");
      var appDataFolders = new[]
      {
        "debug", "diagnostics", "indexes", "logs", "packages", "serialization", "submit queue", "tools", "viewstate"
      };

      foreach (var name in appDataFolders)
      {
        var dir = Path.Combine(appData, name);
        if (!Directory.Exists(dir))
        {
          Directory.CreateDirectory(dir);
        }
      }
    }
  }
}