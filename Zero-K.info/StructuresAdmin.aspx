<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="StructuresAdmin.aspx.cs" Inherits="ZeroKWeb.StructuresAdmin" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
      <asp:GridView ID="GridView1" runat="server" AllowSorting="True">
      </asp:GridView>
    <asp:TextBox runat="server" ID="tbData" TextMode="MultiLine" Rows="40" Columns="150"></asp:TextBox>
    <asp:Button runat="server" Text="Update" OnClick="btnUpdateClick" />
    </div>
    </form>
</body>
</html>
