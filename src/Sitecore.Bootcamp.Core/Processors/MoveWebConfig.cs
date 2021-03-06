﻿namespace Sitecore.Bootcamp.Processors
{
  using System.IO;
  using System.Linq;
  using System.Xml;
  using Sitecore.Diagnostics.Base;

  internal class MoveWebConfig : Processor
  {
    internal override void Process(ProcessorArgs args)
    {
      Assert.ArgumentNotNull(args, "args");

      var root = args.Server.MapPath("/");
      var webConfigPath = Path.Combine(root, "web.config");
      if (!File.Exists(webConfigPath))
      {
        return;
      }

      var webConfig = new XmlDocument();
      webConfig.Load(webConfigPath);
      if (webConfig.SelectSingleNode("/configuration/sitecore") != null)
      {
        // to speed up bootcamp we consider web.config read-to-use if it contains <sitecore> element
        return;
      }

      var httpModule = webConfig.SelectNodes("/configuration/system.web/httpModules/add")
        .OfType<XmlElement>()
        .FirstOrDefault(x => x.GetAttribute("type").Contains(typeof(HttpModule).FullName));
      if (httpModule != null)
      {
        httpModule.ParentNode.RemoveChild(httpModule);
      }

      args.WriteLine("Moving /web.config to /Web_Config/Include/!root_web_config.config");
      var targetFolder = Path.Combine(root, "Web_Config/Include");
      var targetWebConfig = Path.Combine(targetFolder, "!root_web_config.config");
      if(File.Exists(targetWebConfig))
      {
        File.Delete(targetWebConfig);
      }

      webConfig.Save(targetWebConfig);
      File.Delete(webConfigPath);
    }
  }
}