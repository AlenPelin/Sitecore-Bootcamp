using System;
using System.IO;

namespace Sitecore.Bootcamp.Core.Processors
{
  public class MoveAssembliesToBinFolder : Processor
  {
    internal override void Process(ProcessorArgs args)
    {
      var root = args.Server.MapPath("/");
      var bin = Path.Combine(root, "bin");
      if (!Directory.Exists(bin))
      {
        Directory.CreateDirectory(bin);
      }

      var assemblies = Directory.GetFiles(root, "*.dll", SearchOption.AllDirectories);
      foreach (var path in assemblies)
      {
        if (string.IsNullOrEmpty(path) || path.StartsWith(bin, StringComparison.OrdinalIgnoreCase))
        {
          continue;
        }

        var targetPath = Path.Combine(path, Path.Combine(bin, Path.GetFileName(path)));
        if (!File.Exists(targetPath))
        {
          File.Move(path, targetPath);
        }
      }
    }
  }
}