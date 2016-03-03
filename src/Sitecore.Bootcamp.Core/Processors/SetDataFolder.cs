namespace Sitecore.Bootcamp.Core.Processors
{
  using System.IO;

  internal class SetDataFolder : Processor
  {
    internal override void Process(ProcessorArgs args)
    {
      var dataFolder = args.Server.MapPath("/App_Config/Include/zzz/DataFolder.config");
      if (File.Exists(dataFolder))
      {
        return;
      }

      var dir = Path.GetDirectoryName(dataFolder);
      if (!Directory.Exists(dir))
      {
        Directory.CreateDirectory(dir);
      }

      var dataFolderPath = "/App_Data";
      var contents = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration xmlns:set=""http://www.sitecore.net/xmlconfig/set/"">
  <sitecore>
    <sc.variable name=""dataFolder"" set:value=""{dataFolderPath}"" />
  </sitecore>
</configuration>";

      File.WriteAllText(dataFolder, contents);
    }
  }
}