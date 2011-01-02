<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="ErrorReports.aspx.cs" Inherits="ZeroKWeb.ErrorReports" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
    
        <asp:GridView ID="GridView1" runat="server" AllowSorting="True" 
            AutoGenerateColumns="False" CellPadding="4" DataKeyNames="ExceptionLogID" 
            DataSourceID="LinqDataSource" ForeColor="#333333" GridLines="None">
            <AlternatingRowStyle BackColor="White" />
            <Columns>
                <asp:BoundField DataField="ExceptionLogID" HeaderText="ExceptionLogID" 
                    InsertVisible="False" ReadOnly="True" SortExpression="ExceptionLogID" />
                <asp:BoundField DataField="Exception" HeaderText="Exception" 
                    SortExpression="Exception" />
                <asp:BoundField DataField="ExtraData" HeaderText="ExtraData" 
                    SortExpression="ExtraData" />
                <asp:BoundField DataField="RemoteIP" HeaderText="RemoteIP" 
                    SortExpression="RemoteIP" />
                <asp:BoundField DataField="PlayerName" HeaderText="PlayerName" 
                    SortExpression="PlayerName" />
                <asp:BoundField DataField="Time" HeaderText="Time" SortExpression="Time" />
            </Columns>
            <EditRowStyle BackColor="#7C6F57" />
            <FooterStyle BackColor="#1C5E55" Font-Bold="True" ForeColor="White" />
            <HeaderStyle BackColor="#1C5E55" Font-Bold="True" ForeColor="White" />
            <PagerStyle BackColor="#666666" ForeColor="White" HorizontalAlign="Center" />
            <RowStyle BackColor="#E3EAEB" />
            <SelectedRowStyle BackColor="#C5BBAF" Font-Bold="True" ForeColor="#333333" />
            <SortedAscendingCellStyle BackColor="#F8FAFA" />
            <SortedAscendingHeaderStyle BackColor="#246B61" />
            <SortedDescendingCellStyle BackColor="#D4DFE1" />
            <SortedDescendingHeaderStyle BackColor="#15524A" />
        </asp:GridView>
        <asp:LinqDataSource ID="LinqDataSource" runat="server" 
            ContextTypeName="ZkData.ZkDataContext" EntityTypeName="" 
            TableName="ExceptionLogs">
        </asp:LinqDataSource>
    
    </div>
    </form>
</body>
</html>
