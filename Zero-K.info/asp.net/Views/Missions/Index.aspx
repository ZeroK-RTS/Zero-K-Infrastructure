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
				$.get('<%= Url.Action("TileList") %>' + '?offset=' + offset + '&search='+ $('#search').val(), function (data) {
					$('#missions').append(data);
					offset = offset + <%=Model.FetchTileCount%>;
					if (data == '') enabled= false;
					else enabled = true;
				});
			}
		}

		window.onscroll = scrollEvent;
	</script>
	<%= Request["zk_login"] %>

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
			<td width="250" valign="top" align="left">
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

</asp:Content>
