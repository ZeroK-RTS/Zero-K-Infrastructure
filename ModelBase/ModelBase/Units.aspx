<%@ Page Title="" Language="C#" MasterPageFile="~/Master.Master" AutoEventWireup="true" CodeBehind="Units.aspx.cs" Inherits="ModelBase.WebForm2" StylesheetTheme="Default"%>
<%@ Import Namespace="ModelBase"%>
<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    
    Game:  <asp:DropDownList ID="ddGame" runat="server" 
        DataSourceID="gamesDataSource" DataTextField="Shortcut" 
        DataValueField="GameID" AutoPostBack="true" onselectedindexchanged="ddGame_SelectedIndexChanged">
    </asp:DropDownList>

    <asp:LinqDataSource ID="gamesDataSource" runat="server" 
        ContextTypeName="ModelBase.DatabaseDataContext" OrderBy="Shortcut, Name" 
        Select='new (GameID, Name, Shortcut+ " - " + Name as Shortcut )' TableName="Games">
    </asp:LinqDataSource>

    <asp:Panel runat="server" DefaultButton="btnSearch">
    <h3> <asp:Label ID="lbTotal" runat="server" Text=""></asp:Label><br /></h3>
    <asp:Panel ID="Panel1" runat="server" DefaultButton="btnSearch">
    Code: 
    <asp:TextBox ID="tbCode" runat="server" Width="50"></asp:TextBox>&nbsp;
    Name: 
    <asp:TextBox ID="tbName" runat="server"></asp:TextBox>&nbsp;
    Description: 
    <asp:TextBox ID="tbDescription" runat="server"></asp:TextBox>&nbsp;
    Built by (code): 
    <asp:TextBox ID="tbParent" runat="server" Width="50"></asp:TextBox>&nbsp;
    License: 
                <asp:DropDownList ID="ddLicense" runat="server">
                    <asp:ListItem Value="">-- any --</asp:ListItem><asp:ListItem Value="0">Cavedog</asp:ListItem><asp:ListItem Value="1">Unknown</asp:ListItem><asp:ListItem Value="2">OK</asp:ListItem></asp:DropDownList>&nbsp;
                  
    Results on page: 
    <asp:TextBox ID="tbResults" runat="server" Width="30">20</asp:TextBox>&nbsp;                  
                  <asp:Button ID="btnSearch" runat="server" Text="Search" 
        onclick="Button1_Click"/>
    <br/>
    <br/>
    </asp:Panel>    
    
    <asp:GridView ID="GridView1" runat="server" 
    AllowSorting="True" AutoGenerateColumns="False" CellPadding="4" 
    DataKeyNames="UnitID" DataSourceID="LinqDataSource1" ForeColor="#333333" 
    GridLines="None" PageSize="20" AllowPaging="True" 
         >
    <RowStyle BackColor="#F7F6F3" ForeColor="#333333" />
    <Columns>
        <asp:TemplateField HeaderText="Code" SortExpression="Code">
                         <ItemTemplate>
                              <a href='UnitDetail.aspx?UnitID=<%# Eval("UnitID") %>'  >
                                <asp:Image ID="Image1" runat="server" AlternateText='<%# Eval("Code") %>' 
                                    ImageUrl='<%# Eval("Code", "~/unitpics/{0}.png") %>' CssClass="thumbIcon" /></br>
                                    <b>
                                 <asp:Label runat="server" Text='<%# Eval("Code") %>' ></asp:Label></b>
                                </a>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:BoundField DataField="Name" HeaderText="Name" SortExpression="Name" ReadOnly="true" />
                <asp:BoundField DataField="CandidateCount" HeaderText="Candidates" SortExpression="CandidateCount"
                    ReadOnly="true" />
                <asp:TemplateField HeaderText="License" SortExpression="LicenseType">
                    <ItemTemplate>
                        <asp:Label ID="Label4" runat="server" Text='<%# ((LicenseType)(int)Eval("LicenseType")).ToString() %>'
                            Width="125" BackColor='<%# Global.GenColor((int)Eval("LicenseType"), 2) %>'></asp:Label></ItemTemplate>
                </asp:TemplateField>
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
                <asp:BoundField DataField="CurrentStatus" HeaderText="Note" SortExpression="CurrentStatus" />
                <asp:TemplateField HeaderText="Changed" SortExpression="LastChanged">
                    <ItemTemplate>
                        <asp:Label ID="Label6" runat="server" Text='<%# Eval("LastChanged") %>'></asp:Label>
                    </ItemTemplate>
                </asp:TemplateField>
            </Columns>
            <FooterStyle BackColor="#5D7B9D" Font-Bold="True" ForeColor="White" />
            <PagerStyle BackColor="#284775" ForeColor="White" HorizontalAlign="Center" />
            <SelectedRowStyle BackColor="#E2DED6" Font-Bold="True" ForeColor="#333333" />
            <HeaderStyle BackColor="#5D7B9D" Font-Bold="True" ForeColor="White" />
            <EditRowStyle BackColor="#999999" />
            <AlternatingRowStyle BackColor="White" ForeColor="#284775" />
        </asp:GridView>
        <asp:LinqDataSource ID="LinqDataSource1" runat="server" ContextTypeName="ModelBase.DatabaseDataContext"
            EnableUpdate="True" TableName="Units" OnSelecting="LinqDataSource1_Selecting"
>
        </asp:LinqDataSource>
    </asp:Panel>
</asp:Content>
