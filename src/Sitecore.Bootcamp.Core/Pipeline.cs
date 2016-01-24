namespace Sitecore.Bootcamp.Core
{
  using Sitecore.Bootcamp.Core.Processors;
  using Sitecore.Diagnostics.Base;
  using Sitecore.Diagnostics.Base.Annotations;

  internal static class Pipeline
  {
    [NotNull]
    private static readonly Processor[] Processors = 
    {
      new DownloadReleaseMetadata(),
      new ExtractProgramDataNuGetPackage(), 
      new ExtractBundledNuGetPackages(),
      new MoveAssembliesToBinFolder(), 
      new CopyNinjectAssembly(),
      new DeleteRoslynAssemblies(), 
      new SetDataFolder(), 
      new CreateDataFolders(),
      new LoadConnectionStringsConfig(),
      new InstallSqlDatabases(),
      new InstallMonogoDatabases(),
      new SaveConnectionStringsConfig(),
      new InstallDefaultFiles(),
      new InstallClientFiles(), 
      new MoveWebConfig(),
      new InstallConfigFiles(),
      new MergeWebConfigIncludes(),
      new SendRefreshBrowserCommand(),
      new DeleteBootcampFiles()
    };

    internal static void Run([NotNull] ProcessorArgs args)
    {
      Assert.ArgumentNotNull(args, "args");

      foreach (var processor in Processors)
      {
        if (processor == null)
        {
          continue;
        }

        processor.Process(args);
      }
    }
  }
}