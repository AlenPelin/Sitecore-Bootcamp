<%@ Page Language="C#" AutoEventWireup="true" Async="true" %>

<%@ Import Namespace="Sitecore.Bootcamp.Core" %>
<%@ Import Namespace="Sitecore.Diagnostics.Base.Annotations" %>

<script runat="server">
  
  protected override void OnLoad([CanBeNull] EventArgs e)
  {
    BootcampCore.Install(this, BootcampMode.Everything, true);
  }


</script>