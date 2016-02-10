# Sitecore Bootcamp

Sitecore Bootcamp is a project which designed to install Sitecore via NuGet with minimal artifact size possible 
(90% of the files are downloaded from Sitecore cloud), flavoured with extra features:
* run-time installation (with `503 status code`) - just like other CMSs do
* /Web_Config/Include folder support for web.config patching the same way as /App_Config/Include/*.config does
 

Check [Wiki](https://github.com/Sitecore/Sitecore-Bootcamp/wiki) section for details.

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

**9**. `Install-Package SC-AsFiles`     
**10**. Deploy to the remote server
**11**. Open the remote server URL in browser

## Under the hood

Check [Wiki](https://github.com/Sitecore/Sitecore-Bootcamp/wiki) section for details.
