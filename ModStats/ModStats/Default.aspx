<%@ Page Language="C#" AutoEventWireup="True" CodeBehind="Default.aspx.cs" Inherits="ModStats.Default" StylesheetTheme="Default" %>

<%@ Register Src="UnitSelector.ascx" TagName="UnitSelector" TagPrefix="uc1" %>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>


</head>
<body>
<script language="JavaScript" type="text/jscript">

    function adjustDivs() {

        var df = document.getElementById('<%=UpdateProgress.ClientID %>');
        df.style.position = 'absolute';
        df.style.left = (document.documentElement.scrollLeft + 25) + '%';
        df.style.top = (document.documentElement.scrollTop + 200) + 'px';
    } 
    window.onload = adjustDivs;
    window.onresize = adjustDivs;
    window.onscroll = adjustDivs;
</script>

    <form id="form1" runat="server">
    <asp:ScriptManager ID="ScriptManager1" runat="server">
    </asp:ScriptManager>
    
    <div>
        <h2>
            Games</h2>
        <p>
            Players min:
            <asp:TextBox ID="tbPlayersMin" runat="server">2</asp:TextBox>
            &nbsp; max:
            <asp:TextBox ID="tbPlayersMax" runat="server">16</asp:TextBox>
            &nbsp;&nbsp;&nbsp; teams min:
            <asp:TextBox ID="tbTeamsMin" runat="server">2</asp:TextBox>
            &nbsp;&nbsp; max:
            <asp:TextBox ID="tbTeamsMax" runat="server">16</asp:TextBox>
            <br />
            &nbsp;&nbsp;&nbsp; Version min:
            <asp:TextBox ID="tbVersionMin" runat="server">0</asp:TextBox>
            &nbsp;&nbsp; max:
            <asp:TextBox ID="tbVersionMax" runat="server">99999</asp:TextBox>
            <br />
            Mod:
            <asp:TextBox ID="tbModName" runat="server"></asp:TextBox>
&nbsp; map:
            <asp:TextBox ID="tbMapName" runat="server"></asp:TextBox>
&nbsp; player:
            <asp:TextBox ID="tbPlayer" runat="server"></asp:TextBox>
        </p>
                <asp:Button ID="btnFilterGames" runat="server" OnClick="btnFilterGames_Click" Text="Filter" />
                <asp:GridView ID="GridView1" runat="server" AllowPaging="True" AllowSorting="True"
                    CellPadding="4" DataSourceID="LinqDataSourceGames" ForeColor="#333333" 
                    GridLines="None" AutoGenerateColumns="False" 
                    onrowcommand="GridView1_RowCommand">
                    <RowStyle BackColor="#E3EAEB" />
                    <Columns>
                        <asp:TemplateField ShowHeader="False">
                            <ItemTemplate>
                                <asp:LinkButton ID="LinkButton1" runat="server" CausesValidation="false" 
                                    CommandName="filter" Text="Filter" CommandArgument='<%# Eval("GameID") %>'></asp:LinkButton>
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:BoundField DataField="GameID" HeaderText="ID" SortExpression="GameID" />
                        <asp:BoundField DataField="Created" HeaderText="Time" 
                            SortExpression="Created" />
                        <asp:BoundField DataField="Mod" HeaderText="Mod" SortExpression="Mod" />
                        <asp:BoundField DataField="Version" HeaderText="Version" 
                            SortExpression="Version" />
                        <asp:BoundField DataField="Map" HeaderText="Map" SortExpression="Map" />
                        <asp:BoundField DataField="Players" HeaderText="Players" 
                            SortExpression="Players" />
                        <asp:BoundField DataField="Teams" HeaderText="Teams" SortExpression="Teams" />
                        <asp:BoundField DataField="PlayerList" HeaderText="Player list" 
                            SortExpression="PlayerList" />
                    </Columns>
                    <FooterStyle BackColor="#1C5E55" Font-Bold="True" ForeColor="White" />
                    <PagerStyle BackColor="#666666" ForeColor="White" HorizontalAlign="Center" />
                    <SelectedRowStyle BackColor="#C5BBAF" Font-Bold="True" ForeColor="#333333" />
                    <HeaderStyle BackColor="#1C5E55" Font-Bold="True" ForeColor="White" />
                    <EditRowStyle BackColor="#7C6F57" />
                    <AlternatingRowStyle BackColor="White" />
                </asp:GridView>
        <asp:LinqDataSource ID="LinqDataSourceGames" runat="server" ContextTypeName="" OnSelecting="LinqDataSourceGames_Selecting"
            TableName="">
        </asp:LinqDataSource>
                <h2>
                    Unit stats for
                    <asp:Label ID="Label1" runat="server"></asp:Label>
                    &nbsp;game(s)</h2>
                <uc1:UnitSelector ID="unitSelector" runat="server" />
                <asp:Button ID="btnFilterUnits" runat="server" OnClick="btnFilterUnits_Click" Style="height: 26px"
                    Text="Filter" />
                <asp:GridView ID="gridUnits" runat="server" AllowPaging="True" AllowSorting="True"
                    CellPadding="4" DataSourceID="LinqDataSourceUnits" ForeColor="#333333" GridLines="None"
                    AutoGenerateColumns="False" onrowcommand="gridUnits_RowCommand" >
                    <RowStyle BackColor="#E3EAEB" />
                    <FooterStyle BackColor="#1C5E55" Font-Bold="True" ForeColor="White" />
                    <PagerStyle BackColor="#666666" ForeColor="White" HorizontalAlign="Center" />
                    <SelectedRowStyle BackColor="#C5BBAF" Font-Bold="True" ForeColor="#333333" />
                    <HeaderStyle BackColor="#1C5E55" Font-Bold="True" ForeColor="White" />
                    <EditRowStyle BackColor="#7C6F57" />
                    <AlternatingRowStyle BackColor="White" />
                    <Columns>
                        <asp:TemplateField ShowHeader="False">
                            <ItemTemplate>
                                <asp:LinkButton runat="server" CausesValidation="false" 
                            CommandArgument='<%#Eval("Name") %>' CommandName="victims" Text="Victims"></asp:LinkButton> 
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="Name" SortExpression="Name">
                            <ItemTemplate>
                                <a href='http://trac.caspring.org/browser/trunk/mods/ca/units/<%# Eval("Name") %>.lua' target="_blank" >
                                <asp:Image runat="server" AlternateText='<%# Eval("Name") %>' 
                                    ImageUrl='<%# Eval("Name", "~/unitpics/{0}.png") %>' />
                                    </a>
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:BoundField DataField="Cost" HeaderText="Cost" SortExpression="Cost" />
                        <asp:BoundField DataField="Health" HeaderText="HP" SortExpression="Health" />
                        <asp:BoundField DataField="Created" HeaderText="Created / game" SortExpression="Created"
                            DataFormatString="{0:0.00}" />
                        <asp:BoundField DataField="Destroyed" HeaderText="Destroyed / game" SortExpression="Destroyed"
                            DataFormatString="{0:0.00}" />
                        <asp:BoundField DataField="DamageEffectivity" HeaderText="Damage ratio" SortExpression="DamageEffectivity"
                            DataFormatString="{0:0.00}" />
                        <asp:BoundField DataField="DamageCostEffectivity" HeaderText="Cost damaged / lost"
                            SortExpression="DamageCostEffectivity" DataFormatString="{0:0.00}" />
                            
                          <asp:BoundField DataField="DamageDonePerCost" HeaderText="Cost damaged / investment"
                            SortExpression="DamageDonePerCost" DataFormatString="{0:0.00}" />

                        <asp:BoundField DataField="CostParalyzedPerLost" HeaderText="Cost paralyzed / lost" 
                            SortExpression="CostParalyzedPerLost" DataFormatString="{0:0.00}" />
                        <asp:BoundField DataField="ParalyzeDonePerCost" HeaderText="Cost paralyzed / investment" 
                            SortExpression="ParalyzeDonePerCost" DataFormatString="{0:0.00}" />
                        <asp:BoundField DataField="SpendingPercentage" HeaderText="% of total spending" SortExpression="SpendingPercentage"
                            DataFormatString="{0:0.00}" />
                        <asp:BoundField DataField="DamageDonePercentage" HeaderText="% of total damage" SortExpression="DamageDonePercentage"
                            DataFormatString="{0:0.00}" />
                            <asp:BoundField DataField="GamesUsedPercentage" HeaderText="% of games used" SortExpression="GamesUsedPercentage"
                            DataFormatString="{0:0.00}" />
                            
                            
                    </Columns>
                </asp:GridView>
                
                <asp:Panel ID="Panel1" runat="server" Visible="false">
            <table><tr>
                       
            <td valign="top">Top victims:
            <asp:hiddenfield runat="server" ID="victimsUnitKey"/>
            <asp:LinqDataSource ID="LinqDataSourceVictims" runat="server" ContextTypeName="" OnSelecting="LinqDataSourceVictims_Selecting"
            TableName="">
        </asp:LinqDataSource>

            <asp:GridView ID="gridVictims" runat="server" BackColor="White" AllowSorting="True" AllowPaging="True" PageSize="15"
                    BorderColor="#DEDFDE" BorderStyle="None" BorderWidth="1px" CellPadding="4" 
                    ForeColor="Black" GridLines="Vertical" AutoGenerateColumns="False" datasourceid="LinqDataSourceVictims">
                <RowStyle BackColor="#F7F7DE" />
                <Columns>
                        <asp:TemplateField HeaderText="Victim" SortExpression="Name">
                            <ItemTemplate>
                                <a href='http://trac.caspring.org/browser/trunk/mods/ca/units/<%# Eval("Name") %>.lua' target="_blank" >
                                <asp:Image ID="Image1" runat="server" AlternateText='<%# Eval("Name") %>' 
                                    ImageUrl='<%# Eval("Name", "~/unitpics/{0}.png") %>' />
                                    </a>
                            </ItemTemplate>
                        </asp:TemplateField>
                    <asp:BoundField DataField="Damage" HeaderText="Damage" SortExpression="Damage" 
                        DataFormatString="{0:0.}" />
                        <asp:BoundField DataField="Ratio" HeaderText="Cost damaged / lost" SortExpression="Ratio" 
                                        DataFormatString="{0:0.00}" />
                                        
                    <asp:BoundField DataField="CostDamaged" HeaderText="Cost damaged" SortExpression="CostDamaged" 
                        DataFormatString="{0:0.}" />
                                        
                </Columns>
                <FooterStyle BackColor="#CCCC99" />
                <PagerStyle BackColor="#F7F7DE" ForeColor="Black" HorizontalAlign="Right" />
                <SelectedRowStyle BackColor="#CE5D5A" Font-Bold="True" ForeColor="White" />
                <HeaderStyle BackColor="#6B696B" Font-Bold="True" ForeColor="White" />
                <AlternatingRowStyle BackColor="White" />
            </asp:GridView>
            </td>
            <td valign="top">Top killers:
            <asp:LinqDataSource ID="LinqDataSourceKillers" runat="server" ContextTypeName="" OnSelecting="LinqDataSourceKillers_Selecting"
            TableName="">
        </asp:LinqDataSource>
            <asp:GridView ID="gridKillers" runat="server" BackColor="White"  AllowSorting="True" Allowpaging="True" PageSize="15"
                    BorderColor="#DEDFDE" BorderStyle="None" BorderWidth="1px" CellPadding="4" 
                    ForeColor="Black" GridLines="Vertical" AutoGenerateColumns="False" datasourceid="LinqDataSourceKillers">
                <RowStyle BackColor="#F7F7DE" />
                <FooterStyle BackColor="#CCCC99" />
                <PagerStyle BackColor="#F7F7DE" ForeColor="Black" HorizontalAlign="Right" />
                <SelectedRowStyle BackColor="#CE5D5A" Font-Bold="True" ForeColor="White" />
                <HeaderStyle BackColor="#6B696B" Font-Bold="True" ForeColor="White" />
                <AlternatingRowStyle BackColor="White" />
                                <Columns>
                        <asp:TemplateField HeaderText="Killer" SortExpression="Name">
                            <ItemTemplate>
                                <a href='http://trac.caspring.org/browser/trunk/mods/ca/units/<%# Eval("Name") %>.lua' target="_blank" >
                                <asp:Image ID="Image2" runat="server" AlternateText='<%# Eval("Name") %>' 
                                    ImageUrl='<%# Eval("Name", "~/unitpics/{0}.png") %>' />
                                    </a>
                            </ItemTemplate>
                        </asp:TemplateField>
                    <asp:BoundField DataField="Damage" HeaderText="Damage" SortExpression="Damage" 
                                        DataFormatString="{0:0.}" />
                                        <asp:BoundField DataField="Ratio" HeaderText="Cost damaged / lost" SortExpression="Ratio" 
                                        DataFormatString="{0:0.00}" />
                </Columns>

            </asp:GridView>
            </td>
            </tr>
            </table>
            &nbsp;&nbsp;&nbsp;<asp:Button ID="btnHideKillers" runat="server" Text="Hide" 
                        onclick="btnHideKillers_Click" />
            </asp:Panel>
                
        <asp:LinqDataSource ID="LinqDataSourceUnits" runat="server" ContextTypeName="" OnSelecting="LinqDataSourceUnits_Selecting">
        </asp:LinqDataSource>
        <h2>
            Unit vs unit matrix</h2>
                <uc1:UnitSelector ID="UnitSelector2" runat="server" />
                <asp:Button ID="btnMatrix" runat="server" OnClick="btnMatrix_Click" Text="Filter" />
                <asp:GridView ID="gridMatrix" runat="server" AllowPaging="True" AllowSorting="True"
                    AutoGenerateColumns="False" CellPadding="4" DataSourceID="MatrixDataSource" ForeColor="#333333"
                    GridLines="None">
                    <RowStyle BackColor="#E3EAEB" />
                    <Columns>
                        <asp:TemplateField HeaderText="Name" SortExpression="Name">
                            <ItemTemplate>
                                <a href='http://trac.caspring.org/browser/trunk/mods/ca/units/<%# Eval("Name") %>.lua' target="_blank" >
                                <asp:Image ID="Image3" runat="server" AlternateText='<%# Eval("Name") %>' 
                                    ImageUrl='<%# Eval("Name", "~/unitpics/{0}.png") %>' />
                                    </a>
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="Name2" SortExpression="Name2">
                            <ItemTemplate>
                                <a href='http://trac.caspring.org/browser/trunk/mods/ca/units/<%# Eval("Name2") %>.lua' target="_blank" >
                                <asp:Image ID="Image4" runat="server" AlternateText='<%# Eval("Name2") %>' 
                                    ImageUrl='<%# Eval("Name2", "~/unitpics/{0}.png") %>' />
                                    </a>
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:BoundField DataField="DamageEffectivity" HeaderText="Damage ratio" 
                            SortExpression="DamageEffectivity" DataFormatString="{0:0.00}" />
                        <asp:BoundField DataField="DamageCostEffectivity" HeaderText="Cost damaged / lost" SortExpression="DamageCostEffectivity"
                            DataFormatString="{0:0.00}" />
                        <asp:BoundField DataField="ParalyzeCostEffectivity" HeaderText="Cost paralyzed / lost" SortExpression="ParalyzeCostEffectivity"
                            DataFormatString="{0:0.00}" />
                        <asp:BoundField DataField="DamageDoneTotal" HeaderText="Damage done"
                            SortExpression="DamageDoneTotal" DataFormatString="{0:0.}" />

                    </Columns>
                    <FooterStyle BackColor="#1C5E55" Font-Bold="True" ForeColor="White" />
                    <PagerStyle BackColor="#666666" ForeColor="White" HorizontalAlign="Center" />
                    <SelectedRowStyle BackColor="#C5BBAF" Font-Bold="True" ForeColor="#333333" />
                    <HeaderStyle BackColor="#1C5E55" Font-Bold="True" ForeColor="White" />
                    <EditRowStyle BackColor="#7C6F57" />
                    <AlternatingRowStyle BackColor="White" />
                </asp:GridView>
        <asp:LinqDataSource ID="MatrixDataSource" runat="server" ContextTypeName="" TableName=""
            OnSelecting="MatrixDataSource_Selecting">
        </asp:LinqDataSource>
    </div>

			<asp:UpdateProgress runat="server" id="UpdateProgress">
				<ProgressTemplate>
<table style="border: 1px solid #000000; "  width="300" height="100" id="updateProgressTable"  cellspacing="0" cellpadding="0">
 <tr>
  <td align="center" bgcolor="#1C5E55"><b>
  <font face="Verdana" color="#FFFFFF">Please wait...</font></b></td>
 </tr>
 <tr>
  <td align="center" bgcolor="#FFFFFF">
  <table border="0" id="table2" cellspacing="4" cellpadding="3">
   <tr>
    <td><img id="Img1" src="~/Img/Loader.gif" runat="server"/> .. scratching my head ..</td>
   </tr>
  </table>
  </td>
 </tr>
</table>				</ProgressTemplate>
			</asp:UpdateProgress>  
 

    </form>
</body>
</html>
