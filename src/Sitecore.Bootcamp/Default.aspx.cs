﻿namespace Sitecore.Bootcamp
{
  using System;
  using Sitecore.Bootcamp.Core;

  public partial class Default : System.Web.UI.Page
  {
    private readonly BootcampCore BootcampCore;

    public Default()
    {
      this.BootcampCore = new BootcampCore(this, "client");
    }

    protected override void OnPreInit(EventArgs e)
    {
      // ReSharper disable once PossibleNullReferenceException
      if (this.BootcampCore.Process())
      {
        base.OnPreInit(e);
      }
    }
  }
}