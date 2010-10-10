<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="UcComments.ascx.cs" Inherits="ModelBase.UcComments" %>

<asp:Panel ID="Panel1" runat="server">
<asp:Label ID="lbTitle" runat="server"></asp:Label><br />
    <asp:Repeater ID="Repeater1" runat="server" DataSourceID="LinqDataSource1">
        <ItemTemplate>
            <asp:Label ID="Label1" runat="server" Text='<%# Eval("Time","[{0:g}]") %>'></asp:Label> &lt;<asp:Label runat="server" Text='<%# Eval("Name") %>'></asp:Label>&gt;&nbsp;<asp:Label runat="server" Text='<%# Eval("Text") %>'></asp:Label>
        </ItemTemplate>
        <SeparatorTemplate><br />
        </SeparatorTemplate>
    </asp:Repeater>
    <asp:LinqDataSource ID="LinqDataSource1" runat="server" 
        ContextTypeName="ModelBase.DatabaseDataContext" TableName="Comments" 
        onselecting="LinqDataSource1_Selecting">
    </asp:LinqDataSource>
    <br />
    <asp:TextBox ID="tbText" runat="server" Columns="60" Rows="5" 
        TextMode="MultiLine"></asp:TextBox><br />
        <asp:Button ID="btnSubmit" runat="server" Text="Add comment" 
        onclick="btnSubmit_Click" />
</asp:Panel>
