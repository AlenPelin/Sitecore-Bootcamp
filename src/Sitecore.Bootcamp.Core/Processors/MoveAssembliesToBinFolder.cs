namespace Sitecore.Bootcamp.Core.Processors
{
  using System;
  using System.IO;

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

      var appBin = Path.Combine(root, "App_Bin");
      if (!Directory.Exists(appBin))
      {
        return;
      }

      var files = Directory.GetFiles(appBin, "*", SearchOption.AllDirectories);
      foreach (var sourcePath in files)
      {
        if (sourcePath == null)
        {
          continue;
        }

        var virtualPath = sourcePath.Substring(appBin.Length).TrimStart("/\\".ToCharArray());
        var targetPath = Path.Combine(bin, virtualPath);
        var dir = Path.GetDirectoryName(targetPath);
        if (!Directory.Exists(dir))
        {
          Directory.CreateDirectory(dir);
        }

        File.Move(sourcePath, targetPath);
      }
    }
  }
}