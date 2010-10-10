<%@ Page Language="C#" MasterPageFile="~/MainView.master" AutoEventWireup="true"
    CodeFile="planet.aspx.cs" Inherits="PlanetPage" StylesheetTheme="default"  Title="PlanetWars planet" %>

<asp:Content ID="Content4" ContentPlaceHolderID="holderMap" runat="Server">
        <h1>
        <asp:Label ID="lbName" runat="server" Text="planet name"></asp:Label></h1>
        <span style="color:Aqua">Faction</span>: <asp:Label ID="lbFaction" runat="server" Text="Label"></asp:Label>
        <br />
        <span style="color:Aqua">Owner</span>: <asp:Label ID="lbOwner" runat="server" Text="Label"></asp:Label>
        <br />
        <asp:Button ID="btnSendFleet" runat="server" Text="Send blockade fleet" 
            Visible="false" onclick="btnSendFleet_Click" /><br />
        <asp:Literal ID="litEvents" runat="server"></asp:Literal>
        <br />
        <asp:Label ID="lbRename" runat="server" Text="Rename or terraform your planet:" Visible="False" />
        <asp:TextBox ID="tbName" runat="server" Visible="False"></asp:TextBox>
        <asp:DropDownList ID="ddMap" runat="server" AutoPostBack="true" Visible="false" OnSelectedIndexChanged="ddMap_SelectedIndexChanged">
        </asp:DropDownList>
        <asp:Button ID="btnSubmit" runat="server" Text="Confirm terraforming plan" Visible="false"
            OnClick="btnSubmit_Click" />
            <br />
        <span style='font-size:x-small'>You can view planet buildings on the map. Red = enemy, green = ally, blue = yours.</span><br />
        <span id="map">
        <asp:Image ID="imgMap" runat="server"/>
        </span>
        <asp:Literal ID="litPlanetStructures" runat="server"></asp:Literal>
        <br />
        <asp:Literal ID="litPlanetInfo" runat="server"></asp:Literal>
        <br />
        <asp:TextBox ID="tbDesciption" runat="server" TextMode="MultiLine" 
    Rows="2" Columns= "40" Visible="False"></asp:TextBox><asp:Button
            ID="btnSetDesciption" runat="server"  Text="Publish new description" onclick="btnSetDesciption_Click" 
    Visible="False" /><br />        
        <script type="text/javascript">

            function getPos(el) {
                for (var lx = 0, ly = 0; el != null; lx += el.offsetLeft, ly += el.offsetTop, el = el.offsetParent);
                return { x: lx, y: ly }
            }

            function doStuff() {
                if (tpos == null) return;                
                var mpos = getPos(document.getElementById("map"))
                if (mpos.y - firefoxFix > 100) mpos.y = mpos.y - firefoxFix;

                var i = 0;
                for (i = 0; i < tpos.length; i += 2) {
                    var el = document.getElementById("img" + (i / 2));
                    el.style.left = mpos.x + tpos[i] + "px";
                    el.style.top = mpos.y + tpos[i + 1] + "px";
                }
            }
            addLoadEvent(doStuff);
        </script>
        
        <asp:Literal ID="litPopupLinks" runat="server"></asp:Literal>
</asp:Content>
