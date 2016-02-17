namespace Sitecore.Bootcamp.Core.Processors
{
  using System.Configuration;
  using System.IO;
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

        var db = Path.Combine(args.Server.MapPath("/").TrimEnd('\\'), name);
        db = db.Replace("\\\\", "\\").Replace("\\", "_").Replace(":", "").Replace(".", "_");
        db = db.Length > 64 ? db.Substring(db.Length - 64) : db; // mongo db limit is 64 chars for db name
        addNode.SetAttribute("connectionString", string.Format("mongodb://localhost:27017/{0}", db));
        connectionStrings.DocumentElement.AppendChild(addNode);
        args.AddedConnectionStrings.Add(name);
      }
    }
  }
}