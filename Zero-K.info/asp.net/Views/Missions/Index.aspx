<%@ Page Title="" Language="C#" Inherits="System.Web.Mvc.ViewPage<ZeroKWeb.Controllers.MissionsIndexData>"
	MasterPageFile="~/Views/Shared/Site.Master" %>

<%@ Import Namespace="ZkData" %>
<asp:Content runat="server" ID="Main" ContentPlaceHolderID="MainContent">
	<script type="text/javascript">
		var offset = <%=Model.FetchInitialCount%>;
		var enabled = true;
		function scrollEvent() {
			if (!enabled) return;
			var el = document.documentElement;
			if (el.scrollHeight - (document.documentElement.scrollTop + document.documentElement.clientHeight) < 50) {
				enabled = false;
				$.get('<%= Url.Action("TileList") %>' + '?offset=' + offset + '&search='+ $('#search').val()+ '&sp='+ $('#sp').val()+ '&coop='+ $('#coop').val()+ '&adversarial='+ $('#adversarial').val(), function (data) {
					$('#missions').append(data);
					offset = offset + <%=Model.FetchTileCount%>;
					if (data == '') enabled= false;
					else enabled = true;
				});
			}
		}

		window.onscroll = scrollEvent;
	</script>
	<%
		
		using (
		Ajax.BeginForm("TileList",
									 new AjaxOptions()
									 {
										 InsertionMode = InsertionMode.Replace,
										 UpdateTargetId = "missions",
										 OnBegin = string.Format("function(){{offset={0}; enabled=false;}}", Model.FetchInitialCount),
										 OnComplete = "function(){enabled= true;}",

									 }))
		{%>
		<%= Html.CheckBox("sp",true) %>SinglePlayer
		<%= Html.CheckBox("coop",true) %>Coop
		<%= Html.CheckBox("adversarial",true) %>Adversarial
	<%=Html.TextBox("search", Model.SearchString)%><input type="submit" id="submit" value="Search" />
	<%
		}%>
	<span>Design new missions with the <a href='http://code.google.com/p/zero-k/wiki/MissionEditorStartPage'>
		Zero-K Mission Editor</a> </span>
	<table width='100%'>
		<tr>
			<td>
				<div id='missions'>
					<%
						Html.RenderPartial("TileList", Model.LastUpdated);%>
				</div>
			</td>
			<td width="250" valign="top" align="left">
				<h3>
					Highest rated</h3>
				<ul>
					<%
						foreach (var mission in Model.MostRating.Take(15)) Response.Write(string.Format("<li><span title='$mission${1}'>{0}</span></li>", Html.ActionLink(mission.Name, "Detail", new { id = mission.MissionID }), mission.MissionID));%>
				</ul>
				<h3>
					Last comments</h3>
				<ul>
					<%
						foreach (var mission in Model.LastComments.Take(15)) Response.Write(string.Format("<li><span title='$mission${1}'>{0}</span></li>", Html.ActionLink(mission.Name, "Detail", new { id = mission.MissionID }), mission.MissionID));%>
				</ul>

				<h3>
					Most played</h3>
				<ul>
					<%
						foreach (var mission in Model.MostPlayed.Take(15)) Response.Write(string.Format("<li><span title='$mission${1}'>{0}</span></li>", Html.ActionLink(mission.Name, "Detail", new { id = mission.MissionID }), mission.MissionID));%>
				</ul>
			</td>
		</tr>
	</table>
</asp:Content>
