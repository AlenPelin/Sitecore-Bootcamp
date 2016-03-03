namespace Sitecore.Bootcamp
{
  using Sitecore.Bootcamp.Processors;
  using Sitecore.Diagnostics.Base;
  using Sitecore.Diagnostics.Base.Annotations;

  internal static class Pipeline
  {
    [NotNull]
    private static readonly Processor[] Processors = 
    {
      // safe: abort pipeline if nothing changed
      new CheckForChanges(),

      // safe: delete bin/roslyn folder
      new DeleteRoslynAssemblies(), 

      // safe: set data folder to App_Data via App_Config/Include/zzz/DataFolder.config
      new SetDataFolder(), 

      // safe: create App_Data/packages, App_Data/logs etc.
      new CreateDataFolders(),

      // safe: load App_Config/ConnectionStrings.config into memory
      new LoadConnectionStringsConfig(),

      // safe: download missing Sitecore databases, put them to App_Data/Databases and add to ConnectionStrings.config via AttachDBFileName
      new InstallSqlDatabases(),

      // safe: add missing mongo connection strings to ConnectionStrings.config
      new InstallMongoDatabases(),

      // safe: save in-memory ConnectionStrings.config to file system
      new SaveConnectionStringsConfig(),

      // safe: download and extract Sitecore default files such as /areas/social, /layouts, /sitecore/services, /sitecore modules, /views etc.
      new InstallDefaultFiles(),

      // safe: download and extract Sitecore client files such as /sitecore/shell
      new InstallClientFiles(),

      // critical: extract /lib/*.dll from *.nupkg
      new ExtractBundledNuGetPackages(),

      // critical: extract /lib/*.dll from C:\ProgramData\Sitecore\NuGet\SC.[ver]\SC.[ver].nupkg
      new ExtractProgramDataNuGetPackage(), 

      // critical: move App_Bin/* to bin/*
      new MoveAssembliesToBinFolder(), 

      // critical: copy bin/Ninject.dll to bin/Social/Ninject.dll
      new CopyNinjectAssembly(),

      // critical: moving web.config to Web_Config/Include/!root_web_config.config
      new MoveWebConfig(),

      // critical: download and extract Sitecore defult config files such as global.asax, web.config and App_Config/**/*.config
      new InstallConfigFiles(),

      // critical: merge default web.config with Web_Config/Include/**/*.config files
      new MergeWebConfigIncludes(),

      // safe: save the list of files and metadata for CheckForChanges
      new SaveFileList(), 
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