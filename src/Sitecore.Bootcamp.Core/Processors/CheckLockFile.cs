using System;
using System.IO;

namespace Sitecore.Bootcamp.Core.Processors
{
  internal class CheckLockFile : Processor
  {
    internal override void Process(ProcessorArgs args)
    {
      var filePath = args.Server.MapPath("/lock.txt");
      if (!File.Exists(filePath))
      {
        throw new InvalidOperationException("The installation has already been started at " + File.ReadAllText(filePath) + " UTC");
      }
    }
  }
}