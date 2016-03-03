namespace Sitecore.Bootcamp.Processors
{
  using System.Collections.Generic;
  using System.IO;
  using System.Linq;
  using System.Xml;
  using Sitecore.Diagnostics.Base;
  using Sitecore.Diagnostics.ConfigBuilder;

  internal class MergeWebConfigIncludes : Processor
  {
    internal override void Process(ProcessorArgs args)
    {
      args.WriteLine("Merging web.config include files...");

      var webConfigPath = args.Server.MapPath("/web.config");
      Assert.IsTrue(File.Exists(webConfigPath), "The /web.config file does not exist");

      var result = this.Process(webConfigPath);
      if (result == null)
      {
        return;
      }

      result.Save(webConfigPath);
    }

    internal XmlDocument Process(string webConfigPath)
    {
      var rootPath = Path.GetDirectoryName(webConfigPath);
      Assert.IsNotNull(rootPath, "rootPath");

      var webConfigFolder = Path.Combine(rootPath, "Web_Config");
      if (!Directory.Exists(webConfigFolder))
      {
        return null;
      }

      var includeFolder = Path.Combine(webConfigFolder, "Include");
      if (!Directory.Exists(includeFolder))
      {
        return null;
      }

      var tempFolder = Path.Combine(rootPath, "temp/conf");
      if (!Directory.Exists(tempFolder))
      {
        Directory.CreateDirectory(tempFolder);
      }

      try
      {
        var tempWebConfigPath = Path.Combine(tempFolder, "web.config");

        // workaround to bypass configbuilder limitations
        var originalWebConfig = File.ReadAllText(webConfigPath);
        File.WriteAllText(tempWebConfigPath, originalWebConfig.Replace("configSource=", "config__Source__disabled="));

        var tempIncludeFolder = Path.Combine(tempFolder, "Web_Config/Include");
        if (!Directory.Exists(tempIncludeFolder))
        {
          Directory.CreateDirectory(tempIncludeFolder);
        }

        var includeFiles = GetIncludeFiles(includeFolder);
        foreach (var includeFile in includeFiles)
        {
          var tempIncludeFile = Path.Combine(tempIncludeFolder, includeFile.Substring(includeFolder.Length + 1));
          var dir = Path.GetDirectoryName(tempIncludeFile);
          if (!Directory.Exists(dir))
          {
            Directory.CreateDirectory(dir);
          }

          var xml = new XmlDocument();
          xml.Load(includeFile);
          var documentElement = xml.DocumentElement;
          Assert.IsNotNull(documentElement, "documentElement");

          var children = documentElement.ChildNodes.OfType<XmlNode>().ToArray();
          if (children.Length != 1 || children[0].LocalName != "configuration")
          {
            // workaround as patching engine ignores document element and merges starting from children
            var innerConfiguration = xml.CreateElement("configuration");
            foreach (var child in children)
            {
              documentElement.RemoveChild(child);
              innerConfiguration.AppendChild(child);
            }

            documentElement.AppendChild(innerConfiguration);
          }

          xml.Save(tempIncludeFile);
        }

        var result = new ConfigBuilderEngineEx().Build(tempWebConfigPath, true, false);
        
        // strip sitecore namespace from web.config
        foreach (var attr in result.DocumentElement.Attributes.OfType<XmlAttribute>().ToArray())
        {
          if (attr.NamespaceURI == "http://www.w3.org/2000/xmlns/" && attr.Value.Contains("sitecore.net"))
          {
            result.DocumentElement.RemoveAttribute(attr.LocalName, attr.NamespaceURI);
          }
        }

        foreach (var element in result.DocumentElement.ChildNodes.OfType<XmlElement>().ToArray())
        {
          if (element == null || element.Name == "sitecore")
          {
            continue;
          }

          StripSourceAttributes(element);
        }

        var output = new XmlDocument();
        output.LoadXml(result.OuterXml.Replace("config__Source__disabled=", "configSource="));

        if (Directory.Exists(webConfigFolder))
        {
          Directory.Delete(webConfigFolder, true);
        }

        if (output.OuterXml == originalWebConfig)
        {
          // the web.config is already okay
          return null;
        }

        return output;
      }
      finally
      {
        if (Directory.Exists(tempFolder))
        {
          Directory.Delete(tempFolder, true);
        } 
      }
    }

    private void StripSourceAttributes(XmlElement element)
    {
      foreach (var child in element.ChildNodes.OfType<XmlElement>().ToArray())
      {
        foreach (var attr in child.Attributes.OfType<XmlAttribute>().ToArray())
        {
          if (attr.NamespaceURI.Contains("sitecore.net"))
          {
            child.RemoveAttribute(attr.LocalName, attr.NamespaceURI);
          }
        }

        StripSourceAttributes(child);
      }
    }

    private static IEnumerable<string> GetIncludeFiles(string sourceInclude)
    {
      foreach (var file in Directory.GetFiles(sourceInclude, "*.config"))
      {
        yield return file;
      }

      foreach (var dir in Directory.GetDirectories(sourceInclude))
      {
        foreach (var file in GetIncludeFiles(dir))
        {
          yield return file;
        }
      }
    }

    internal class ConfigBuilderEngineEx : ConfigBuilderEngine
    {
      protected override IEnumerable<string> GetIncludeFoldersPaths()
      {
        yield return "/Web_Config/Include";
      }

      protected override XmlElement FindSitecoreNode(XmlDocument configuration)
      {
        return configuration.DocumentElement;
      }
    }
  }
}