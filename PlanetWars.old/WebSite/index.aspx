<%@ Page Language="C#" MasterPageFile="~/MainView.master" AutoEventWireup="true" CodeFile="index.aspx.cs" Inherits="IndexMapPage" StyleSheetTheme="default" Title="PlanetWars map" %>

<asp:Content ID="Content4" ContentPlaceHolderID="holderMap" Runat="Server">
    <div style="text-align:left; border:0;">
        <img src="galaxy/galaxy.jpg" style="position:static; text-align:left; z-index:-50; top:0px; left:0px;" width="<%= mapSizeX %>" height="<%= mapSizeY %>"/>
        <asp:Literal ID="litPlanets" runat="server" EnableViewState="false"></asp:Literal>
        <asp:Literal ID="litJs" runat="server" EnableViewState="false"></asp:Literal>
    </div>
</asp:Content>

