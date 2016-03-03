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
        else
        {
          var sourceFile = new FileInfo(sourcePath);
          var targetFile = new FileInfo(targetPath);
          if (targetFile.Exists && targetFile.Length == sourceFile.Length)
          {
            // to speed up bootcamp we do not overwrite the assemblies with the same size
            continue;
          }
        }

        // to avoid re-deploying during next web deploy we copy instead of moving
        File.Copy(sourcePath, targetPath);
      }
    }
  }
}