<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="ResourceDetail.aspx.cs" Inherits="PlasmaServer.ResourceDetail" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
    <h2><asp:Label runat="server" ID="lbName"></asp:Label></h2>
		<asp:Literal runat="server" ID="lbDetails"/>
    
    Depends on:<br />
    <asp:Literal runat="server" ID="litLinks"></asp:Literal><br /><br />
		
    Spring hashes:<br />
    <asp:Literal runat="server" ID="litHashes"></asp:Literal><br /><br />
    
    Possible files:<br />
			<asp:GridView ID="GridView1" runat="server" AllowPaging="True" 
				AllowSorting="True" AutoGenerateColumns="False" CellPadding="4" 
				DataKeyNames="ResourceID,Md5" DataSourceID="lqContentFiles" ForeColor="#333333" 
				GridLines="None">
				<RowStyle BackColor="#EFF3FB" />
				<Columns>
					<asp:CommandField ShowDeleteButton="True" />
					<asp:BoundField DataField="FileName" HeaderText="FileName" 
						SortExpression="FileName" />
					<asp:HyperLinkField DataNavigateUrlFields="TorrentFileName" 
						DataNavigateUrlFormatString="~/Resources/{0}" HeaderText="Torrent" 
						Text="torrent" />
					<asp:BoundField DataField="Md5" HeaderText="Md5" ReadOnly="True" 
						SortExpression="Md5" />
					<asp:BoundField DataField="Length" HeaderText="Length" 
						SortExpression="Length" />
					<asp:BoundField DataField="LinkText" HeaderText="Links" SortExpression="Links"  HtmlEncode="false"/>
					<asp:BoundField DataField="LinkCount" HeaderText="LinkCount" 
						SortExpression="LinkCount" />
				</Columns>
				<FooterStyle BackColor="#507CD1" Font-Bold="True" ForeColor="White" />
				<PagerStyle BackColor="#2461BF" ForeColor="White" HorizontalAlign="Center" />
				<SelectedRowStyle BackColor="#D1DDF1" Font-Bold="True" ForeColor="#333333" />
				<HeaderStyle BackColor="#507CD1" Font-Bold="True" ForeColor="White" />
				<EditRowStyle BackColor="#2461BF" />
				<AlternatingRowStyle BackColor="White" />
			</asp:GridView>
    
    	<asp:LinqDataSource ID="lqContentFiles" runat="server" 
				ContextTypeName="ZkData.ZkDataContext" EnableDelete="True" 
				onselecting="lqContentFiles_Selecting" TableName="ResourceContentFiles" 
				ondeleting="lqContentFiles_Deleting">
			</asp:LinqDataSource>
    
    </div>
    		<asp:Literal runat="server" ID="litBasics"></asp:Literal>
    </form>
</body>
</html>
