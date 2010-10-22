<%@ Page Title="" Language="C#" Inherits="System.Web.Mvc.ViewPage<ZeroKWeb.Controllers.MissionsIndexData>"
	MasterPageFile="~/Views/Shared/Site.Master" %>

<asp:Content runat="server" ID="Main" ContentPlaceHolderID="MainContent">
	<script type="text/javascript">
		var offset = <%=Model.FetchInitialCount%>;
		var enabled = true;
		function scrollEvent() {
			if (!enabled) return;
			var el = document.documentElement;
			if (el.scrollHeight - (document.documentElement.scrollTop + document.documentElement.clientHeight) < 50) {
				enabled = false;
				$('#loader').css("visibility","visible");
				$.get('<%= Url.Action("TileList") %>' + '?offset=' + offset + '&search='+ $('#search').val(), function (data) {
					$('#missions').append(data);
					offset = offset + <%=Model.FetchTileCount%>;
					$('#loader').css("visibility","collapse");
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
										 OnBegin = string.Format("function(){{offset={0}; enabled=true; $('#loader').css('visibility','visible');}}", Model.FetchInitialCount),
										 OnComplete = "function(){$('#loader').css('visibility','collapse');}"
									 }))
		{%>
	<%=Html.TextBox("search", Model.SearchString)%><input type="submit" id="submit" value="Search" />
	<%
		}%>
	<table>
		<tr>
			<td>
				<div id='missions'>
					<%
						Html.RenderPartial("TileList", Model.LastUpdated);%>
				</div>
			</td>
			<td width="250" valign="top">
				<h3>
					Most popular</h3>
				<ul>
					<%
						foreach (var mission in Model.MostPopular.Take(15)) Response.Write(string.Format("<li>{0}</li>", Html.ActionLink(mission.Name, "Detail", new { id = mission.MissionID })));%>
				</ul>
				<h3>
					Last comments</h3>
				<ul>
					<%
						foreach (var mission in Model.LastCommented.Take(15)) Response.Write(string.Format("<li>{0}</li>", Html.ActionLink(mission.Name, "Detail", new { id = mission.MissionID })));%>
				</ul>
			</td>
		</tr>
	</table>
	<span id="loader" style="visibility: collapse"><h2>Loading ... <img src="<%= Url.Content("~/img/loader.gif") %>"/></h2></span>
</asp:Content>
