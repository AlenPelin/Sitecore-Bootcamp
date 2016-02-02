namespace Sitecore.Bootcamp.Core.Processors
{
  using System.Configuration;
  using Sitecore.Diagnostics.Base;

  internal class InstallMongoDatabases : InstallDatabaseProcessorBase
  {
    internal override void Process(ProcessorArgs args)
    {
      Assert.ArgumentNotNull(args, "args");
      var connectionStrings = args.ConnectionStringsConfig;

      var mongoConnectionStrings = new[]
      {
        "analytics",
        "tracking.live",
        "tracking.history",
        "tracking.contact",
      };
      foreach (var name in mongoConnectionStrings)
      {
        if (args.AddedConnectionStrings.Contains(name))
        {
          continue;
        }

        if (ConfigurationManager.ConnectionStrings[name] != null)
        {
          continue;
        }

        var addNode = connectionStrings.CreateElement("add");
        addNode.SetAttribute("name", name);
        addNode.SetAttribute("connectionString", string.Format("mongodb://localhost:27017/{0}_{1}", args.Server.MapPath("/").Replace("\\", "_").Replace(":", "").Replace(".", "_"), name.Replace("\\", "_").Replace(":", "").Replace(".", "_")));
        connectionStrings.DocumentElement.AppendChild(addNode);
        args.AddedConnectionStrings.Add(name);
      }
    }
  }
}