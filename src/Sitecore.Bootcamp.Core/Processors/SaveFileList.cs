namespace Sitecore.Bootcamp.Processors
{
  using System.IO;
  using System.Linq;

  internal class SaveFileList : Processor
  {
    private static readonly string[] IgnoreDirectories = { "App_Data", "temp", "sitecore" };

    internal override void Process(ProcessorArgs args)
    {
      var filePath = args.Server.MapPath("/App_Data/bootcamp.lastrun.txt");
      if (File.Exists(filePath))
      {
        try
        {
          File.Delete(filePath);
        }
        catch
        {
          // we don't care if we cannot delete the file - the feature is optional
          return;
        }
      }

      using (var writter = new StreamWriter(File.Open(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None)))
      {
        foreach (var directory in new DirectoryInfo(args.Server.MapPath("/")).GetDirectories())
        {
          if (IgnoreDirectories.Contains(directory.Name))
          {
            continue;
          }

          foreach (var file in directory.GetFiles("*", SearchOption.AllDirectories))
          {
            writter.Write(file.FullName);
            writter.Write("|");
            writter.Write(file.Length);
            writter.Write("|");
            writter.Write(file.LastWriteTimeUtc);

            writter.WriteLine();
          }
        }
      }

      var clientFilePath = args.Server.MapPath("/App_Data/bootcamp.client.lastrun.txt");
      File.WriteAllText(clientFilePath, Directory.GetFiles(args.Server.MapPath("/"), "*", SearchOption.AllDirectories).Length.ToString());
    }
  }
}