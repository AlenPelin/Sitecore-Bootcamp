# Sitecore Bootcamp

Sitecore Bootcamp is a project which designed to install Sitecore via NuGet with minimal artifact size possible 
(90% of the files are downloaded from Sitecore cloud), flavoured with extra features:
* run-time installation (with `503 status code`) - just like other CMSs do
* /Web_Config/Include folder support for web.config patching the same way as /App_Config/Include/*.config does

## Prerequisites (once per dev machine)

1. Run latest [QA version of SIM](http://dl.sitecore.net/updater/qa/sim)
2. Ensure that SIM has necessary Sitecore version to install
3. Click `Bundled Tools -> Generate NuGet` option

## Usage (for running on dev machine)

**1**. Create empty Visual Studio website solution  
**2**. Open NuGet Console  
**3**. `Install-Package Sitecore.Bootcamp`  
**4**. Change NuGet server (combobox) to `Sitecore NuGet`  
**5**. `Install-Package SC.Sitecore.Kernel`   
**6**. Find `Sitecore.Kernel` in Project References  
**7**. Modify its properties to set Copy Local = True  
**8**. Create `/App_Data` folder and put there the `license.xml` valid Sitecore license file  
**9**. Click F5 and choose running without debugging

## Usage (for running on remote servers)

**9**. Copy `C:\ProgramData\Sitecore\NuGet\[ver]\SC.[ver].nupkg` to `/App_Data` folder  
**10**. Click F5 and choose running without debugging

## Under the hood

The logic is pretty straigforward. Small artifact size (the set of files that needed to be deployed) is achieved
by simple idea: we need to remove all default Sitecore files that can be downloaded from Sitecore cloud. So the
artifact only contains: 
* `/bin/Sitecore.Kernel.dll` (to figure out Sitecore version to use), 
* `/bin/Sitecore.Bootcamp.Core.dll` (downloading and installation logic)
* `/Default.aspx` that calls `BootcampCore.Install()`
* (optional) Custom assemblies, static files, layouts, sublayouts, views etc.
* (optional) `/App_Config` folder with custom configuration files
* (optional) `/App_Config` folder with default configuration files (not recommended)
* (optional) `/App_Config/web.config` file that will be moved to `/web.config` when installation is complete
* (optional) `/Web_Config/Include/**.config` files that will change default `web.config` file

After deploying a website, there is no Sitecore `web.config` file that executes all Sitecore magic which allows Sitecore.Bootcamp to rule the world:
* download default config files and extract them to `/App_Config` folder
* download default static files and extract them to `/` folder
* download client static files and extract them to `/sitecore/shell` folder
* check if there is a `/App_Config/ConnectionStrings.config` file and create a list of missing databases
* download missing default databases, put them to `/App_Data/Databases` folder and modify `/App_Config/ConnectionStrings.config` using `AttachDBFileName` syntax
* if exists, move `/App_Config/web.config` file to `/web.config`
* if exists, transform `/web.config` with `/Web_Config/Include/*.config` files using the same patching logic that `/App_Config/Include/*.config` files utilize
* remove `Sitecore.Bootcamp` files and restart application pool
