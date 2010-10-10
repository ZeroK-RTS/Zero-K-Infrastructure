<%@ Page Title="" Language="C#" MasterPageFile="~/Master.Master" AutoEventWireup="true" CodeBehind="UnitDetail.aspx.cs" Inherits="ModelBase.UnitDetailForm" StylesheetTheme="Default"%>
<%@ Register src="UcComments.ascx" tagname="UcComments" tagprefix="uc1" %>
<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <span style="background-color: #5D7B9D; color: White; font-weight: bold; width: 300px; display:block; text-align:center; padding:3px;">
    Game unit details</span>
    <table style="border: 1px black solid;">
        <tr>
            <td>
                Code:
            </td>
            <td>
                <asp:Image ID="imgUnit" runat="server" CssClass="thumbIcon" /><br />
                <asp:Label ID="lbCode" runat="server" Text="Label" Font-Bold="True"></asp:Label>
            </td>
        </tr>
        <tr>
            <td>
                Game:
            </td>
            <td>
                <asp:Label ID="lbGame" runat="server" Text="lbGame"></asp:Label>
            </td>
        </tr>

        <tr>
            <td>
                Name:
            </td>
            <td>
                <asp:Label ID="lbName" runat="server" Text="Label"></asp:Label>
            </td>
        </tr>
        <tr>
            <td>
                Description:
            </td>
            <td>
                <asp:Label ID="lbDescription" runat="server" Text="Label"></asp:Label>
            </td>
        </tr>

        <tr>
            <td>
                Note:
            </td>
            <td>
                <asp:TextBox ID="tbNote" runat="server" TextMode="MultiLine" Rows="4" Columns="70"></asp:TextBox>
            </td>
        </tr>
        <tr>
            <td>
                License:
            </td>
            <td>
                <asp:DropDownList ID="ddLicense" runat="server">
                    <asp:ListItem Value="">-- any --</asp:ListItem><asp:ListItem Value="0">Cavedog</asp:ListItem><asp:ListItem Value="1">Unknown</asp:ListItem><asp:ListItem Value="2">OK</asp:ListItem></asp:DropDownList>
            </td>
        </tr>

        <tr>
            <td>
                Model progress:
            </td>
            <td>
                <asp:TextBox ID="tbModelProg" runat="server"></asp:TextBox>           
             </td>
        </tr>
        <tr>
            <td>
                Texture progress:
            </td>
            <td>
                <asp:TextBox ID="tbTextureProg" runat="server"></asp:TextBox>           
             </td>
        </tr>
        <tr>
            <td>
                Script progress:
            </td>
            <td>
                <asp:TextBox ID="tbScriptProg" runat="server"></asp:TextBox>           
             </td>
        </tr>
        <tr>
            <td>
                Last changed:
            </td>
            <td>
                <asp:Label ID="lbLastChanged" runat="server"/>
             </td>
        </tr>
        

        
        <tr>
            <td>
                Submit changes:
            </td>
            <td>
                <asp:Button ID="btnSubmit" runat="server" Text="Submit" 
                    onclick="btnSubmit_Click" />
            </td>
        </tr>

        <tr>
            <td>
                Candidate models:
            </td>
            <td>
                <asp:DropDownList ID="ddCandidates" runat="server">
                </asp:DropDownList>
                <asp:Button ID="btnRemove"
                    runat="server" Text=">>Remove" onclick="btnRemove_Click" /><br />
                <asp:Literal ID="litCandidates" runat="server"></asp:Literal>
            </td>
        </tr>



        
    </table>

    <uc1:UcComments ID="UcComments1" runat="server"/>
</asp:Content>
