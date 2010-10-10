<%@ Page Title="" Language="C#" MasterPageFile="~/Master.Master" AutoEventWireup="true"
    CodeBehind="ModelDetail.aspx.cs" Inherits="ModelBase.ModelDetailForm" StylesheetTheme="Default" %>

<%@ Register Src="UcComments.ascx" TagName="UcComments" TagPrefix="uc1" %>
<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <span style="background-color: #1C5E55; color: White; font-weight: bold; width: 300px; display:block; text-align:center; padding:3px;">
     Model details</span>
    <table style="border: 1px black solid;">
        <tr>
            <td>
                Name:
            </td>
            <td>
                <asp:Image ID="imgUnit" runat="server" CssClass="thumbIcon" /><br />
                <asp:Label ID="lbName" runat="server" Text="Label" Font-Bold="True"></asp:Label>
            </td>
        </tr>
        <tr>
            <td>
                Author:
            </td>
            <td>
                <asp:Label ID="lbAuthor" runat="server" Text="Label"></asp:Label>
            </td>
        </tr>
        <tr>
            <td>
                Description:
            </td>
            <td>
                <asp:TextBox ID="tbDescription" runat="server" TextMode="MultiLine" Rows="4" Columns="70"></asp:TextBox>
            </td>
        </tr>
        <tr>
            <td>
                Tags:
            </td>
            <td>
                <asp:Label ID="lbTags" runat="server" Text="Label"></asp:Label><br />
                <asp:ListBox ID="ddTags" runat="server" SelectionMode="Multiple"></asp:ListBox>
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
                License:
            </td>
            <td>
                <asp:DropDownList ID="ddLicense" runat="server" AutoPostBack="True" 
                    onselectedindexchanged="ddLicense_SelectedIndexChanged">
                </asp:DropDownList>&nbsp;&nbsp;<asp:HyperLink ID="hlLicense" runat="server">link</asp:HyperLink>
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
                Sources:
            </td>
            <td>
                <asp:HyperLink ID="linkSources" runat="server" Font-Bold="True" Target="_blank" Font-Size="Large">HyperLink</asp:HyperLink><br />
                <asp:Label ID="lbModified" runat="server" Text="Label"></asp:Label>
            </td>
        </tr>
        
        <tr>
            <td>
                Candidate for:
            </td>
            <td>
                Game:<asp:DropDownList ID="ddGames" runat="server"/> code:<asp:TextBox ID="tbCandCode" runat="server" Width="80"></asp:TextBox>
                <asp:Button ID="btnAdd"
                    runat="server" Text="Add >> " onclick="btnAdd_Click" />
                <asp:DropDownList ID="ddCandidates" runat="server">
                </asp:DropDownList>
                <asp:Button ID="btnRemove"
                    runat="server" Text=">>Remove" onclick="btnRemove_Click" /><br />
                <asp:Literal ID="litCandidates" runat="server"></asp:Literal>
            </td>
        </tr>



        
    </table>
       <asp:Literal ID="Literal1" runat="server"></asp:Literal>
    <uc1:UcComments ID="UcComments1" runat="server" />
</asp:Content>
