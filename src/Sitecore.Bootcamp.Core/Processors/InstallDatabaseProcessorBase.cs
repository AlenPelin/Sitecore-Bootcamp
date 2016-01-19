namespace Sitecore.Bootcamp.Core.Processors
{
  using System.Xml;
  using Sitecore.Diagnostics.Base;
  using Sitecore.Diagnostics.Base.Annotations;

  internal abstract class InstallDatabaseProcessorBase : Processor
  {
    protected void AddConnectionString([NotNull] XmlDocument connectionStrings, [NotNull] string name, [NotNull] string connectionString)
    {
      Assert.ArgumentNotNull(connectionStrings, "connectionStrings");
      Assert.ArgumentNotNull(name, "name");
      Assert.ArgumentNotNull(connectionString, "connectionString");

      var addNode = connectionStrings.CreateElement("add");
      addNode.SetAttribute("name", name);
      addNode.SetAttribute("connectionString", connectionString);

      var documentElement = connectionStrings.DocumentElement;
      Assert.IsNotNull(documentElement, "documentElement is null");

      documentElement.AppendChild(addNode);
    }
  }
}