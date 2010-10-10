<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="MissionEditorServer.Default" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
    
        <asp:GridView ID="GridView1" runat="server" AutoGenerateColumns="False" 
            CellPadding="4" DataKeyNames="MissionID" DataSourceID="LinqDataSource1" 
            ForeColor="#333333" GridLines="None" onrowcommand="GridView1_RowCommand" 
            AllowPaging="True" AllowSorting="True">
            <RowStyle BackColor="#E3EAEB" />
            <Columns>
                <asp:TemplateField ShowHeader="False">
                    <ItemTemplate>
                        <asp:LinkButton ID="LinkButton1" runat="server" CausesValidation="false" 
                            CommandArgument='<%#Eval("MissionID") %>' CommandName="download" Text='<%# "Download (" + Eval("DownloadCount") + ")" %>'></asp:LinkButton>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField ShowHeader="False">
                    <ItemTemplate>
                        <asp:LinkButton ID="LinkButton2" runat="server" CausesValidation="false" 
                            CommandName="comments" CommandArgument='<%# Eval("MissionID") %>' Text='<%# "Comments (" + Eval("CommentCount") + ")" %>'></asp:LinkButton>
                    </ItemTemplate>
                </asp:TemplateField>
                
                <asp:TemplateField ShowHeader="False">
                    <ItemTemplate>
                        <asp:LinkButton ID="LinkButton3" runat="server" CausesValidation="false" 
                            CommandName="top10" CommandArgument='<%# Eval("MissionID") %>' Text="Top score"></asp:LinkButton>
                    </ItemTemplate>
                </asp:TemplateField>
                
                <asp:BoundField DataField="Name" HeaderText="Name" ReadOnly="True" 
                    SortExpression="Name" />
                <asp:BoundField DataField="Rating" HeaderText="Rating" ReadOnly="True" 
                    SortExpression="Rating" DataFormatString="{0:0.0}" />
                <asp:BoundField DataField="Author" HeaderText="Author" ReadOnly="True" 
                    SortExpression="Author" />
                <asp:BoundField DataField="Description" HeaderText="Description" 
                    ReadOnly="True" SortExpression="Description" />
                <asp:BoundField DataField="TopScoreLine" HeaderText="Top player" 
                    ReadOnly="True" SortExpression="TopScoreLine" />
                    
                <asp:BoundField DataField="Mod" HeaderText="Mod" ReadOnly="True" 
                    SortExpression="Mod" />
                <asp:BoundField DataField="Map" HeaderText="Map" ReadOnly="True" 
                    SortExpression="Map" />
                <asp:BoundField DataField="DownloadCount" HeaderText="DownloadCount" 
                    ReadOnly="True" SortExpression="DownloadCount" />
                <asp:BoundField DataField="CreatedTime" HeaderText="CreatedTime" 
                    SortExpression="CreatedTime" />
                <asp:BoundField DataField="ModifiedTime" HeaderText="ModifiedTime" 
                    SortExpression="ModifiedTime" />
                <asp:BoundField DataField="CommentCount" HeaderText="CommentCount" 
                    SortExpression="CommentCount" />
                <asp:BoundField DataField="LastCommentTime" HeaderText="LastCommentTime" 
                    SortExpression="LastCommentTime" />
                    
            </Columns>
            <FooterStyle BackColor="#1C5E55" Font-Bold="True" ForeColor="White" />
            <PagerStyle BackColor="#666666" ForeColor="White" HorizontalAlign="Center" />
            <SelectedRowStyle BackColor="#C5BBAF" Font-Bold="True" ForeColor="#333333" />
            <HeaderStyle BackColor="#1C5E55" Font-Bold="True" ForeColor="White" />
            <EditRowStyle BackColor="#7C6F57" />
            <AlternatingRowStyle BackColor="White" />
        </asp:GridView>
        <asp:LinqDataSource ID="LinqDataSource1" runat="server" OnSelecting="LinqDataSource1_Selecting">
        </asp:LinqDataSource>
    
    </div>
    <br />
    <asp:Label ID="Label1" runat="server"></asp:Label>
    </form>
</body>
</html>
