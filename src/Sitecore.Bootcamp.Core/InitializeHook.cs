namespace Sitecore.Bootcamp
{
  using System.Web;
  using Sitecore.Events.Hooks;

  public class InitializeHook : IHook
  {
    public void Initialize()
    {
      new BootcampCore(HttpContext.Current, BootcampMode.Everything, false).Install();
    }
  }
}