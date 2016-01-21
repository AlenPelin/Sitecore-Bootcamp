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

      File.Move(args.Server.MapPath("Default.aspx"), args.Server.MapPath("/App_Data/Default.aspx.disabled"));
    }
  }
}