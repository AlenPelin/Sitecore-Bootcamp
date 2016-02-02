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

      var destFileName = args.Server.MapPath("/App_Data/Default.aspx.disabled");
      if (File.Exists(destFileName))
      {
        File.Delete(destFileName);
      }

      File.Move(args.Server.MapPath("Default.aspx"), destFileName);
    }
  }
}