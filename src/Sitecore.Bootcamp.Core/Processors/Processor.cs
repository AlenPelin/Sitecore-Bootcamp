namespace Sitecore.Bootcamp.Core.Processors
{
  using Sitecore.Diagnostics.Base.Annotations;

  public abstract class Processor
  {
    internal abstract void Process([NotNull] ProcessorArgs args);
  }
}