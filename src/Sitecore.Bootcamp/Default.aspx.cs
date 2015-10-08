using System;

namespace Sitecore.Bootcamp
{
  using System.Collections.Generic;
  using System.Data.SqlClient;

  public partial class Default : System.Web.UI.Page
  {
    protected void Page_Load(object sender, EventArgs e)
    {
      panel.Visible = !IsPostBack;
    }

    protected void DoInstall(object sender, EventArgs e)
    {
      var connectionStrings = new Dictionary<string, string>();
      var cstr = new SqlConnectionStringBuilder
      {
        DataSource = dataSource.Text,
        IntegratedSecurity = integrated.Checked,
        UserID = userId.Text,
        Password = password.Text
      };

      cstr.InitialCatalog = coreName.Text;
      connectionStrings.Add("core", cstr.ToString());

      cstr.InitialCatalog = masterName.Text;
      connectionStrings.Add("master", cstr.ToString());

      cstr.InitialCatalog = webName.Text;
      connectionStrings.Add("web", cstr.ToString());

      cstr.InitialCatalog = reportingName.Text;
      connectionStrings.Add("reporting", cstr.ToString());

      connectionStrings.Add("analytics", analytics.Text);
      connectionStrings.Add("tracking.contract", tracking_contract.Text);
      connectionStrings.Add("tracking.live", tracking_live.Text);
      connectionStrings.Add("tracking.history", tracking_history.Text);

      Installer.Install(this.Context, connectionStrings);
    }
  }
}