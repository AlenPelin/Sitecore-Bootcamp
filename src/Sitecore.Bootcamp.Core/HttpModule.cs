namespace Sitecore.Bootcamp
{
  using System.Web;

  public class HttpModule : IHttpModule
  {
    public void Init(HttpApplication context)
    {
      context.BeginRequest += (sender, args) => new BootcampCore(context.Server, context.Application, null, BootcampMode.Everything, false).Install();
    }
    
    public void Dispose()
    {
    }
  }
}