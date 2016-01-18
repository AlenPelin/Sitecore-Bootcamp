namespace Sitecore.Bootcamp.Core
{
  using System;
  using Sitecore.Diagnostics.Base.Annotations;

  public partial class Default : System.Web.UI.Page
  {
    [NotNull]
    private readonly BootcampCore BootcampCore;

    public Default()
    {
      this.BootcampCore = new BootcampCore(this);
    }

    protected override void OnPreInit([CanBeNull] EventArgs e)
    {
      if (this.BootcampCore.Process())
      {
        base.OnPreInit(e);
      }
    }
  }
}