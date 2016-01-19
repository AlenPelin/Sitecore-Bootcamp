namespace Sitecore.Bootcamp.Core.Processors
{
  using System.IO;
  using Sitecore.Diagnostics.Base;

  internal class DeleteBootcampFiles : Processor
  {
    internal override void Process(ProcessorArgs args)
    {
      Assert.ArgumentNotNull(args, "args");

      args.WriteLine("Deleting installer files...");

      File.Delete(args.Server.MapPath("bin\\Sitecore.Bootcamp.Core.dll"));

      var shell = args.Server.MapPath("bin\\Sitecore.Bootcamp.dll");
      if (File.Exists(shell))
      {
        File.Delete(shell);
      }

      File.Delete(args.Server.MapPath("Default.aspx"));
    }
  }
}