using System.IO;

namespace Sitecore.Bootcamp.Core.Processors
{
  public class DeleteRoslynAssemblies : Processor
  {
    internal override void Process(ProcessorArgs args)
    {
      var roslyn = args.Server.MapPath("/bin/roslyn");
      if (Directory.Exists(roslyn))
      {
        Directory.Delete(roslyn, true);
      }
    }
  }
}