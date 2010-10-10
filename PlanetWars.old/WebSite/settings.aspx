<%@ Page Language="C#" MasterPageFile="~/MainView.master" AutoEventWireup="true" CodeFile="settings.aspx.cs" Inherits="SettingsPage" StylesheetTheme="default"  Title="PlanetWars settings"%>

<asp:Content ID="Content2" ContentPlaceHolderID="holderMap" Runat="Server">
    <h3>Notification settings</h3>
    <table>
    <tr>
    <td>Battle phase:</td><td><asp:CheckBox ID="cbPreparing" runat="server" Text="Battle preparing" />
    <asp:CheckBox ID="cbStarted" runat="server" Text="Battle started" />
    <asp:CheckBox ID="cbEnded" runat="server" Text="Battle ended" /></td>
    </tr>
    
    <tr><td>For planets: </td><td><asp:DropDownList ID="drPlanet" runat="server">
        <asp:ListItem Selected="True">All</asp:ListItem>
        <asp:ListItem>My</asp:ListItem>
    </asp:DropDownList></td></tr>
    
    <tr><td>When: </td><td><asp:CheckBox ID="cbAttacking" runat="server" Text="Attacking" /><asp:CheckBox ID="cbDefending" runat="server" Text="Defending" /></td></tr>
    <tr><td colspan="2" align="center"><asp:Button ID="btnNotification" runat="server" Text="Set notifications" 
        onclick="btnNotification_Click" /></td></tr>
    </table>
    
    <h3>Customization</h3>
    <table>
    <tr><td>Change your title:</td><td><asp:TextBox ID="tbTitle" runat="server"></asp:TextBox></td></tr>
    <tr><td colspan="2" align="center"><asp:Button ID="btnSetTitle" runat="server" Text="Set title" 
        onclick="btnSetTitle_Click" /></td></tr>
    <tr><td>Timezone: </td><td><asp:DropDownList ID="drTimeZone" runat="server">
    </asp:DropDownList></td>
    </tr>
    <tr><td colspan="2" align="center"><asp:Button ID="btnSetTimeZone" runat="server" Text="Set timezone" 
        onclick="btnSetTimeZone_Click" /></td></tr>
    </table>
   

    <h3>Password</h3>
    <table>
    <tr>
    <td>Current password: </td><td><asp:TextBox ID="tbCurPass" runat="server" TextMode="Password"></asp:TextBox></td>
    </tr>
    <tr><td>New password: </td><td><asp:TextBox ID="tbNewPass1" runat="server" TextMode="Password"></asp:TextBox></td></tr>
    <tr><td>New password (retype): </td><td><asp:TextBox ID="tbNewPass2" runat="server" TextMode="Password"></asp:TextBox></td></tr>
    <tr><td colspan="2" align="center"><asp:Button ID="tbSetPassword" runat="server" 
            Text="Change password" onclick="tbSetPassword_Click" /></td></tr>
    </table>
</asp:Content>

