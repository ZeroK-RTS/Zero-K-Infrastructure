<%@ Page Title="" Language="C#" MasterPageFile="~/Master.Master" AutoEventWireup="true"
    CodeBehind="Models.aspx.cs" Inherits="ModelBase.ModelsForm" StylesheetTheme="Default" %>
<%@ Import Namespace="ModelBase"%>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <ul>
        <li>Upload your models using subversion - <a href="SubversionHowTo.aspx">instruction here</a></li>
        <li>When they appear on this list - should within 10 minutes, or you can click this:
            <asp:Button ID="btnRefresh" 
        runat="server" Text="reload models" onclick="btnRefresh_Click" Height="20px" /> , 
            click on them and update information about them on this site.</li>
        <li>To display content of whole repository go to <a href='http://springrts.com/websvn/listing.php?repname=ModelBase&path=%2F&sc=0' target="_blank">http://springrts.com/websvn/</a></li>
    </ul>
<asp:Panel runat="server" DefaultButton="btnSearch">
    Author:
    <asp:DropDownList ID="ddAuthor" runat="server" AppendDataBoundItems="true" DataSourceID="authorsDataSource"
        DataTextField="Login" DataValueField="UserID">
        <asp:ListItem Text="---" Value="0"></asp:ListItem>
    </asp:DropDownList>
    &nbsp;Name: 
    <asp:TextBox ID="tbName" runat="server"></asp:TextBox>&nbsp;
    Description: 
    <asp:TextBox ID="tbDescription" runat="server"></asp:TextBox>&nbsp;
    &nbsp;&nbsp;Tags:<asp:ListBox ID="ddTags" runat="server" DataSourceID="tagsDataSource"
        DataTextField="Name" DataValueField="TagID" SelectionMode="Multiple"></asp:ListBox>
    &nbsp; Results on page:
    <asp:TextBox ID="tbResults" runat="server" Width="30">20</asp:TextBox>&nbsp;
    <asp:Button ID="btnSearch" runat="server" Text="Search" OnClick="btnSearch_Click" />

    <asp:LinqDataSource ID="authorsDataSource" runat="server" ContextTypeName="ModelBase.DatabaseDataContext"
        OrderBy="Login" Select="new (Models, Login, UserID)" TableName="Users" Where="Models.Count > 0">
    </asp:LinqDataSource>
    <br />
    &nbsp;<br />
    <asp:LinqDataSource ID="tagsDataSource" runat="server" ContextTypeName="ModelBase.DatabaseDataContext"
        OrderBy="Name" Select="new (Name, TagID)" TableName="Tags">
    </asp:LinqDataSource>
    <asp:GridView ID="GridView1" runat="server" AutoGenerateColumns="False" CellPadding="4"
        DataSourceID="gridDataSource" ForeColor="#333333" GridLines="None" AllowPaging="True"
        AllowSorting="True" PageSize="20">
        <RowStyle BackColor="#E3EAEB" />
        <Columns>
            <asp:TemplateField HeaderText="Name" SortExpression="Name">
                <ItemTemplate>
                    <a href='ModelDetail.aspx?ModelID=<%# Eval("ModelID") %>'>
                        <img src='<%# Eval("IconUrl")%>' alt='<%# Eval("Name") %>'
                            class="thumbIcon" />
                        <br />
                        <b><%# Eval("Name") %></b>
                    </a>
                </ItemTemplate>
            </asp:TemplateField>
            <asp:TemplateField HeaderText="Author" SortExpression="UserName">
                <ItemTemplate>
                    <asp:Label ID="Label1" runat="server" Text='<%#Eval("UserName") %>'></asp:Label>
                </ItemTemplate>
            </asp:TemplateField>
            <asp:TemplateField HeaderText="Tags">
                <ItemTemplate>
                    <%# Eval("TagString") %>
                </ItemTemplate>
            </asp:TemplateField>

            <asp:BoundField DataField="Description" HeaderText="Description" ReadOnly="True"
                SortExpression="Description" />
                
                <asp:TemplateField HeaderText="Model" SortExpression="ModelProgress">
                    <ItemTemplate>
                        <asp:Label ID="Label2" runat="server" Text='<%# Bind("ModelProgress") %>' Width="50"
                            BackColor='<%# Global.GenColor((int)Eval("ModelProgress")) %>'></asp:Label></ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="Texture" SortExpression="TextureProgress">
                    <ItemTemplate>
                        <asp:Label ID="Label3" runat="server" Text='<%# Bind("TextureProgress") %>' Width="50"
                            BackColor='<%# Global.GenColor((int)Eval("TextureProgress")) %>'></asp:Label></ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="Script" SortExpression="ScriptProgress">
                    <ItemTemplate>
                        <asp:Label ID="Label1" runat="server" Text='<%# Bind("ScriptProgress") %>' Width="25"
                            BackColor='<%# Global.GenColor((int)Eval("ScriptProgress")) %>'></asp:Label></ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="Overal" SortExpression="OverallProgress">
                    <ItemTemplate>
                        <asp:Label ID="Label5" runat="server" Text='<%# Bind("OverallProgress") %>' Width="50"
                            BackColor='<%# Global.GenColor((int)Eval("OverallProgress")) %>'></asp:Label></ItemTemplate>
                </asp:TemplateField>
                
                
                
            <asp:BoundField DataField="Modified" HeaderText="Modified" ReadOnly="True" SortExpression="Modified" />
        </Columns>
        <FooterStyle BackColor="#1C5E55" Font-Bold="True" ForeColor="White" />
        <PagerStyle BackColor="#666666" ForeColor="White" HorizontalAlign="Center" />
        <SelectedRowStyle BackColor="#C5BBAF" Font-Bold="True" ForeColor="#333333" />
        <HeaderStyle BackColor="#1C5E55" Font-Bold="True" ForeColor="White" />
        <EditRowStyle BackColor="#7C6F57" />
        <AlternatingRowStyle BackColor="White" />
    </asp:GridView>
    <asp:LinqDataSource ID="gridDataSource" runat="server" ContextTypeName="ModelBase.DatabaseDataContext" onselecting="gridDataSource_Selecting">
    </asp:LinqDataSource>
        </asp:Panel>
</asp:Content>
