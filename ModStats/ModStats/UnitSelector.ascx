<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="UnitSelector.ascx.cs" Inherits="ModStats.UnitSelector" %>

<table>
<tr>
<td>Your selection:<br />
<asp:ListBox ID="lbSelection" runat="server" Rows="8" SelectionMode="Multiple"></asp:ListBox></td>
<td>
<asp:Button ID="btn" runat="server" Text="&lt;&lt;" onclick="btn_Click" /><br />
<asp:Button ID="btnRemove" runat="server" Text="&gt;&gt;" 
        onclick="btnRemove_Click" />

</td>
<td>All units:<br />
<asp:ListBox ID="lbSource" runat="server" DataSourceID="LinqDataSource1" 
        DataTextField="Key" DataValueField="Key" Rows="8" 
        SelectionMode="Multiple"></asp:ListBox>
</td>

</tr>
</table>
    <asp:LinqDataSource ID="LinqDataSource1" runat="server" 
        ContextTypeName="" TableName="" 
    onselecting="LinqDataSource1_Selecting" >
    </asp:LinqDataSource>
