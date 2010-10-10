<%@ Page Language="C#" MasterPageFile="~/MainView.master" AutoEventWireup="true"
    CodeFile="faction.aspx.cs" Inherits="FactionPage" StylesheetTheme="default"  Title="PlanetWars faction"%>

<%@ Import Namespace="PlanetWarsShared" %>
<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="Server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="holderMap" runat="Server">
    <h2>
        <asp:Label ID="Label1" runat="server" Text="Label"></asp:Label>
    </h2>
    <asp:Literal ID="litPlayers" runat="server"></asp:Literal>
</asp:Content>
