namespace Sitecore.Bootcamp.Core.Processors
{
  using System.IO;
  using System.Linq;
  using System.Xml;
  using Sitecore.Diagnostics.Base;

  internal class LoadConnectionStringsConfig : Processor
  {
    internal override void Process(ProcessorArgs args)
    {
      Assert.ArgumentNotNull(args, "args");

      var addedConnectionStrings = args.AddedConnectionStrings;
      var connectionStringsPath = args.Server.MapPath("/App_Config/ConnectionStrings.config");

      var connectionStringsConfig = args.ConnectionStringsConfig;
      if (File.Exists(connectionStringsPath))
      {
        connectionStringsConfig.Load(connectionStringsPath);

        var documentElement = connectionStringsConfig.DocumentElement;
        Assert.IsNotNull(documentElement, "documentElement is null");

        foreach (var addNode in documentElement.ChildNodes.OfType<XmlElement>())
        {
          if (addNode == null)
          {
            continue;
          }

          var name = addNode.GetAttribute("name");
          if (!string.IsNullOrEmpty(name))
          {
            addedConnectionStrings.Add(name);
          }
        }
      }
      else
      {
        connectionStringsConfig.LoadXml("<connectionStrings />");
      }
    }
  }
}