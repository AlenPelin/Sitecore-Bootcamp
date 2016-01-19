﻿namespace Sitecore.Bootcamp.Core.Processors
{
  using Sitecore.Diagnostics.Base;

  internal class SendRefreshBrowserCommand : Processor
  {
    internal override void Process(ProcessorArgs args)
    {
      Assert.ArgumentNotNull(args, "args");

      args.WriteLine("<script>setTimeout(function(){ document.location.href=document.location.protocol + '//' + document.location.hostname;},5000);</script></body></html>", true);
    }
  }
}