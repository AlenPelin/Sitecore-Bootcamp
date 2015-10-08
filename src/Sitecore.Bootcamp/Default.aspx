<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="Sitecore.Bootcamp.Default" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    <div style="width: 800px; margin: 0 auto;">
      <asp:PlaceHolder runat="server" ID="panel" Visible="False">
        <h1>License</h1>
        Sitecore License file:<br />
        <asp:FileUpload runat="server" ID="license"/><br />

        <h1>SQL Server</h1>
        Data Source:<br /> <asp:TextBox runat="server" ID="dataSource" Text="(local)" Width="400px" /><br />
        Integrated Security:<br /> <asp:CheckBox runat="server" ID="integrated" Width="400px"/><br />
        User ID:<br /> <asp:TextBox runat="server" ID="userId" Text="sa" Width="400px" /><br />
        Password:<br /> <asp:TextBox runat="server" ID="password" Text="12345" TextMode="Password" Width="400px" /><br />

        <br />

        Core:<br /> <asp:TextBox runat="server" ID="coreName" Text="Sitecore_Core" Width="400px"/><br />
        Master:<br /> <asp:TextBox runat="server" ID="masterName" Text="Sitecore_Master" Width="400px"/><br />
        Web:<br /> <asp:TextBox runat="server" ID="webName" Text="Sitecore_Web" Width="400px"/><br />
        Reporting:<br /> <asp:TextBox runat="server" Text="Sitecore_Reporting" ID="reportingName" Width="400px"/><br />

        <h1>MongoDB</h1>

        Analytics: <br /><asp:TextBox runat="server" ID="analytics" Text="mongodb://localhost/analytics" Width="800px" /><br />
        Tracking Contract:<br /> <asp:TextBox runat="server" ID="tracking_contract" Text="mongodb://localhost/tracking_contact" Width="800px"/><br />
        Tracking Live: <br /><asp:TextBox runat="server" ID="tracking_live" Text="mongodb://localhost/tracking_live" Width="800px"/><br />
        Tracking History: <br /><asp:TextBox runat="server" ID="tracking_history" Text="mongodb://localhost/tracking_history" Width="800px"/><br />
        <br />
        <asp:Button runat="server" OnClick="DoInstall" Text="Install"/>

      </asp:PlaceHolder>
    
    </div>
    </form>
</body>
</html>
