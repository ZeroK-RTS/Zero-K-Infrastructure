<%@ Page Title="" Language="C#" MasterPageFile="~/Master.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="ModelBase.WebForm1" StylesheetTheme="Default" %>
<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">

    <a href="RssHandler.ashx"><img src="Img/rss.png" />RSS feed</a>
  
    <asp:ListView ID="ListView1" runat="server"  DataSourceID="LinqDataSource1">
        <ItemTemplate>
            <li style="background-color: #DCDCDC;color: #000000;">
                <asp:Label ID="TimeLabel" runat="server" Text='<%# Eval("Time") %>' />
                <asp:Label runat="server" Text='<%# Eval("FullText") %>' />
                
            </li>
        </ItemTemplate>
        <LayoutTemplate>
            <ul ID="itemPlaceholderContainer" runat="server" 
                style="font-family: Verdana, Arial, Helvetica, sans-serif;">
                <li ID="itemPlaceholder" runat="server" />
                </ul>
                <div style="text-align: center;background-color: #CCCCCC;font-family: Verdana, Arial, Helvetica, sans-serif;color: #000000;">
                    <asp:DataPager ID="DataPager1" runat="server">
                        <Fields>
                            <asp:NextPreviousPagerField ButtonType="Button" ShowFirstPageButton="True" 
                                ShowNextPageButton="False" ShowPreviousPageButton="False" />
                            <asp:NumericPagerField />
                            <asp:NextPreviousPagerField ButtonType="Button" ShowLastPageButton="True" 
                                ShowNextPageButton="False" ShowPreviousPageButton="False" />
                        </Fields>
                    </asp:DataPager>
                </div>
            </LayoutTemplate>
            <ItemSeparatorTemplate>
                <br />
            </ItemSeparatorTemplate>
    </asp:ListView>
        <asp:LinqDataSource ID="LinqDataSource1" runat="server" 
            ContextTypeName="ModelBase.DatabaseDataContext" 
            onselecting="LinqDataSource1_Selecting" TableName="Comments">
        </asp:LinqDataSource>
</asp:Content>
