<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Users.aspx.cs" Inherits="ModelBase.Users" MasterPageFile="~/Master.Master" StylesheetTheme="Default" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
        <asp:GridView ID="GridView1" runat="server" AllowPaging="True" 
            AllowSorting="True" AutoGenerateColumns="False" CellPadding="4" 
            DataKeyNames="UserID" DataSourceID="LinqDataSource1" ForeColor="#333333" 
            GridLines="None">
            <RowStyle BackColor="#F7F6F3" ForeColor="#333333" />
            <Columns>
                <asp:BoundField DataField="Login" HeaderText="Login" SortExpression="Login" />
                <asp:CheckBoxField DataField="IsAdmin" HeaderText="IsAdmin" 
                    SortExpression="IsAdmin" />
                <asp:CheckBoxField DataField="IsDeleted" HeaderText="IsDeleted" 
                    SortExpression="IsDeleted" />
            </Columns>
            <FooterStyle BackColor="#5D7B9D" Font-Bold="True" ForeColor="White" />
            <PagerStyle BackColor="#284775" ForeColor="White" HorizontalAlign="Center" />
            <SelectedRowStyle BackColor="#E2DED6" Font-Bold="True" ForeColor="#333333" />
            <HeaderStyle BackColor="#5D7B9D" Font-Bold="True" ForeColor="White" />
            <EditRowStyle BackColor="#999999" />
            <AlternatingRowStyle BackColor="White" ForeColor="#284775" />
        </asp:GridView>
        <asp:LinqDataSource ID="LinqDataSource1" runat="server" 
            ContextTypeName="ModelBase.DatabaseDataContext" EnableUpdate="True" 
            TableName="Users" OnUpdated="UsersUpdated" 
            OrderBy="IsDeleted, IsAdmin desc, Login">
        </asp:LinqDataSource>

</asp:Content>
