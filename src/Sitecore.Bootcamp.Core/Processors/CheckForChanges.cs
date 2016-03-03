namespace Sitecore.Bootcamp.Processors
{
  using System;
  using System.Data;
  using System.IO;
  using System.Linq;
  using System.Threading;

  internal class CheckForChanges : Processor
  {
    private static readonly string[] IgnoreDirectories = { "App_Data", "temp", "sitecore" };

    internal override void Process(ProcessorArgs args)
    {
      var filePath = args.Server.MapPath("/App_Data/bootcamp.lastrun.txt");
      if (!File.Exists(filePath))
      {
        return;
      }

      try
      {
        var oldFiles =
          File.ReadAllLines(filePath)
            .Select(x => x.Split('|'))
            .Select(x => new { FilePath = x[0], FileSize = x[1], LastModifiedUtc = x[2] })
            .GetEnumerator();

        foreach (var directory in new DirectoryInfo(args.Server.MapPath("/")).GetDirectories())
        {
          if (IgnoreDirectories.Contains(directory.Name))
          {
            continue;
          }

          foreach (var file in directory.GetFiles("*", SearchOption.AllDirectories))
          {
            if (oldFiles.MoveNext())
            {
              return;
            }

            if (!file.FullName.Equals(oldFiles.Current))
            {
              return;
            }
          }
        }
      }
      catch (Exception ex)
      {
        args.WriteLine("Error happened during checking for changes: " + ex.GetType() + ", Message: " + ex.Message + ", StackTrace: " + ex.StackTrace);
        try
        {
          File.Delete(filePath);
        }
        catch
        {
          // we don't care if we cannot delete file          
        }
      }

      var fileClientPath = args.Server.MapPath("/App_Data/bootcamp.client.lastrun.txt");
      if (!File.Exists(fileClientPath))
      {
        if (args.Mode == BootcampMode.NoClient)
        {
          throw new AbortPipelineException();
        }

        return;
      }

      try
      {
        if (int.Parse(File.ReadAllText(fileClientPath)) == Directory.GetFiles(args.Server.MapPath("/sitecore")).Length)
        {
          
        }
      }
      catch (Exception ex)
      {
        args.WriteLine("Error happened during checking client for changes: " + ex.GetType() + ", Message: " + ex.Message + ", StackTrace: " + ex.StackTrace);
        try
        {
          File.Delete(fileClientPath);
        }
        catch
        {
          // we don't care if we cannot delete file          
        }

        return;
      }

      throw new AbortPipelineException();
    }
  }
}