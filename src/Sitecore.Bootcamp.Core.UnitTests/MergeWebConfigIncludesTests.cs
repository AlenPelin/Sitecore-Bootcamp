using System;
using System.IO;
using System.Threading;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sitecore.Bootcamp.Core.Processors;

namespace Sitecore.Bootcamp.Core.UnitTests
{
  [TestClass]
  public class MergeWebConfigIncludesTests
  {
    [TestMethod]
    public void ProcessTest()
    {
      var processor = new MergeWebConfigIncludes();
      var webConfig = new XmlDocument();
      webConfig.LoadXml("<configuration><connectionStrings configSource=\"App_Config\\Sitecore.config\" /><appSettings><add key=\"setting1\" value=\"value1\" /></appSettings></configuration>");
      var setting1 = webConfig.SelectSingleNode("/configuration/appSettings/add[@key='setting1']") as XmlElement;
      Assert.IsNotNull(setting1, "setting1 != null");
      Assert.AreEqual("value1", setting1.GetAttribute("value"));

      var tmp = Path.GetTempFileName() + ".dir";
      Directory.CreateDirectory(tmp);
      try
      {
        var path = Path.Combine(tmp, "web.config");
        webConfig.Save(path);

        var dir = Path.Combine(tmp, "web_config\\Include");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "1.config"), "<configuration xmlns:set=\"http://www.sitecore.net/xmlconfig/set/\"><appSettings><add key=\"setting1\" set:value=\"value2\" /></appSettings></configuration>");

        var result = processor.Process(path);
        Assert.AreEqual("<configuration><connectionStrings configSource=\"App_Config\\Sitecore.config\" /><appSettings><add key=\"setting1\" value=\"value2\" /></appSettings></configuration>", result.OuterXml);
      }
      finally
      {
        if (Directory.Exists(tmp))
        {
          Directory.CreateDirectory(tmp);
        }
      }
    }
  }
}
