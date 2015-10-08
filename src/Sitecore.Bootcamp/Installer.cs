namespace Sitecore.Bootcamp
{
  using System;
  using System.Collections.Generic;
  using System.IO;
  using System.Linq;
  using System.Reflection;
  using System.Web;
  using System.Xml;

  using Ionic.Zip;

  public static class Installer
  {
    public static void Install(HttpContext context, Dictionary<string, string> connectionStrings)
    {
      var response = context.Response;
      response.Write("Sitecore installation is in progress... <br /><br />");
      response.Flush();

      var websiteFolderPath = context.Server.MapPath("/");
      var websiteDirectory = new DirectoryInfo(websiteFolderPath);
      
      // removing Default.aspx installer file
      var defaultFilePath = Path.Combine(websiteFolderPath, "Default.aspx");

      File.Delete(defaultFilePath);

      response.Write("<br/> Done!");
      response.Flush();

      var rootDirectory = websiteDirectory.Parent;
      var websiteOldPath = Path.Combine(rootDirectory.FullName, "WebsiteOld");

      // recreating WebsiteOld 
      RecreateFolder(new DirectoryInfo(websiteOldPath));

      // moving solution files
      Move(response, websiteFolderPath, websiteOldPath, 1);
      
      var bootcampName = "Sitecore.Bootcamp";
      var bootcampAssembly = Assembly.Load(bootcampName);
      Unpack(response, bootcampAssembly, bootcampName, websiteDirectory, 50);

      try
      {
        var shellName = "Sitecore.Bootcamp.Shell";
        var shellAssembly = Assembly.Load(shellName);
        Unpack(response, shellAssembly, shellName, websiteDirectory, 100);
      }
      catch (FileNotFoundException)
      {
      }
      
      // override new files with the files there were existing before installation started
      Move(response, websiteOldPath, websiteFolderPath, 1);

      var xml = new XmlDocument();
      var cstrFilePath = Path.Combine(websiteDirectory.FullName, "App_Config\\ConnectionStrings.config");
      xml.Load(cstrFilePath);
      foreach (var connectionString in connectionStrings)
      {
        var name = connectionString.Key;
        var add = xml.DocumentElement.ChildNodes.OfType<XmlElement>().FirstOrDefault(x => x.GetAttribute("name") == name);
        add?.SetAttribute("connectionString", connectionString.Value);
      }
      xml.Save(cstrFilePath);

      Directory.Delete(websiteOldPath, true);

      // install license into existing files
      InstallLicense(context.Request.Files[0], websiteFolderPath);

      response.Write("]<br /><br />Done!");
      response.Flush();

      response.Write("<script type='text/javascript'>window.location.href+='?done=true';</script>");
      response.Flush();
      response.Close();
    }

    private static void InstallLicense(HttpPostedFile license, string websiteFolderPath)
    {
      if (license != null)
      {
        var newAppData = Path.Combine(websiteFolderPath, "App_Data");
        Directory.CreateDirectory(newAppData);
        var licensePath = Path.Combine(newAppData, "license.xml");
        if (File.Exists(licensePath))
        {
          File.Delete(licensePath);
        }

        license.SaveAs(licensePath);
      }
    }

    private static void RecreateFolder(DirectoryInfo directory)
    {
      if (directory.Exists)
      {
        directory.Delete(true);
      }

      directory.Create();
    }

    private static void Move(HttpResponse response, string directory, string destDirName, int N)
    {
      var i = 0;
      foreach (var file in Directory.GetFiles(directory, "*", SearchOption.AllDirectories))
      {
        var newFile = Path.Combine(destDirName, file.Substring(directory.Length + 1));
        var dir = Path.GetDirectoryName(newFile);
        if (!Directory.Exists(dir))
        {
          Directory.CreateDirectory(dir);
        }
        else if (File.Exists(newFile))
        {
          File.Delete(newFile);
        }
        
        File.Move(file, newFile);

        if (++i == N)
        {
          response.Write("X");
          response.Flush();

          i = 0;
        }
      }
    }

    private static void Unpack(HttpResponse response, Assembly assembly, string assemblyName, DirectoryInfo outputDirectory, int N = 1)
    {
      using (Stream input = assembly.GetManifestResourceStream(assemblyName + ".Files.zip"))
      {
        if (input == null)
        {
          throw new InvalidOperationException("The Files.zip resource is not found in " + assemblyName);
        }

        var tmp = Path.GetTempFileName();
        try
        {
          using (var output = File.OpenWrite(tmp))
          {
            response.Write("X");
            response.Flush();

            CopyStream(input, output);
          }

          using (var zip = new ZipFile(tmp))
          {
            var i = 0;
            foreach (var zipEntry in zip.Entries)
            {
              zipEntry.Extract(outputDirectory.FullName);
              if (N == 1 || ++i == N)
              {
                response.Write("X");
                response.Flush();

                i = 0;
              }
            }
          }
        }
        finally
        {
          if (File.Exists(tmp))
          {
            File.Delete(tmp);
          }
        }
      }
    }

    public static void CopyStream(Stream input, Stream output)
    {
      // Insert null checking here for production
      byte[] buffer = new byte[8192];

      int bytesRead;
      while ((bytesRead = input.Read(buffer, 0, buffer.Length)) > 0)
      {
        output.Write(buffer, 0, bytesRead);
      }
    }
  }
}