using System;
using System.IO;

namespace Sitecore.Bootcamp.Core.Processors
{
  internal class DeleteLockFile : Processor
  {
    internal override void Process(ProcessorArgs args)
    {
      var filePath = args.Server.MapPath("/lock.txt");
      if (!File.Exists(filePath))
      {
        throw new InvalidOperationException("The installation lock.txt file cannot be found");
      }

      File.Delete(filePath);
    }
  }
}