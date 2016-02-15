<%@ Page Language="C#" AutoEventWireup="true" Async="true" %>

<%@ Import Namespace="Sitecore.Bootcamp.Core" %>
<%@ Import Namespace="Sitecore.Diagnostics.Base.Annotations" %>

<!-- 
  
  PURPOSE
    This file is an entry point of Sitecore Bootcamp project which installs necessary Sitecore files around deployed Sitecore-powered website.
  
  UNDER THE HOOD
    This file is only executed during the very first application request and deletes itself in the end of this first request which recycles application pool.
  
  IMPORTANT! 
    Even if for some reason this file still exists (is not removed, or re-deployed) it will not be used as Sitecore overrides default ASP.NET request pipeline.
  
  -->

<script runat="server">
  
  protected override void OnLoad([CanBeNull] EventArgs e)
  {
    BootcampCore.Install(this, BootcampMode.Everything, true);
  }
  
</script>