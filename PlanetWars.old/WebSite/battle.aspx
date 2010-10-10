<%@ Page Title="PlanetWars battle details" Language="C#" MasterPageFile="~/MainView.master" AutoEventWireup="true" CodeFile="battle.aspx.cs" Inherits="battle" StylesheetTheme="default" %>

<asp:Content ID="Content2" ContentPlaceHolderID="holderMap" Runat="Server">
<h2><asp:Label ID="lbTitle" runat="server" Text="Label" EnableViewState="false"></asp:Label></h2>
Planet: <asp:Label ID="lbPlanet" runat="server" Text="Label" EnableViewState="false"></asp:Label><br />
Time: <asp:Label ID="lbTime" runat="server" Text="Label" EnableViewState="false"></asp:Label><br />
Attacker: <asp:Label ID="lbAttacker" runat="server" Text="Label" EnableViewState="false"></asp:Label><br />
Winner: <asp:Label ID="lbWinner" runat="server" Text="Label" EnableViewState="false"></asp:Label><br />
Length: <asp:Label ID="lbLength" runat="server" Text="Label" EnableViewState="false"></asp:Label><br />
<asp:Literal ID="litDetails" runat="server"></asp:Literal>
</asp:Content>


