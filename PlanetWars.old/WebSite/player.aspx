<%@ Page Language="C#" MasterPageFile="~/MainView.master" AutoEventWireup="true"
    CodeFile="player.aspx.cs" Inherits="PlayerPage" StylesheetTheme="default"  Title="PlanetWars player"%>

<asp:Content ID="Content3" ContentPlaceHolderID="holderMap" runat="Server">
        <h1>
            <asp:Image ID="imgRank" runat="server" Width="60" /><asp:Label ID="lbRankName" runat="server" Text="Label"></asp:Label>
            &nbsp;&nbsp;&nbsp;&nbsp;
            <asp:Label ID="lbName" runat="server" Text="Label"></asp:Label>
        </h1>
        <span style="color:Aqua">Faction</span>: <asp:Label ID="lbFaction" runat="server" Text="Label"></asp:Label>
        <br />
        <span style="color:Aqua">Home planet</span>: <asp:Label ID="lbPlanet" runat="server" Text="Label"></asp:Label>
        <br />
        <span style="color:Aqua">Awards</span>: <asp:Literal ID="litAwards" runat="server" EnableViewState="false"></asp:Literal><br />
        <br />                
        <asp:Literal ID="litEvents" runat="server" EnableViewState="false"></asp:Literal><br />
        <asp:Literal ID="litBuildpower" runat="server" EnableViewState="false"></asp:Literal><br />
        <asp:Label ID="lbSendBp" runat="server" Text="Label" Visible="false">Send metal (50% loss): </asp:Label><asp:TextBox
            ID="tbBpAmmount" Visible="false" runat="server"></asp:TextBox>
<asp:Button ID="btnSendBp" Visible="false" runat="server"
                Text="Send aid" onclick="btnSendBp_Click" />
        <span style='font-size:x-small;'>
        You can unlock upgrades here.<br />
        Mobile units can be dropped on any planet (commander controls drop).
        Buildings can only be dropped to planets owned by you or your allies.
        </span>
        <asp:Literal ID="litUpgrades" runat="server" EnableViewState="false"></asp:Literal>
        <asp:Literal ID="litJs" runat="server" EnableViewState="false"></asp:Literal><br />
</asp:Content>
