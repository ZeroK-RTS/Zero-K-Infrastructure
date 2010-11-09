<%@ Page Language="C#" Inherits="System.Web.Mvc.ViewPage<System.Linq.IQueryable<ZkData.Mission>>" %>

<%	
	foreach (var mission in Model)
	{
%>
<div id='<%=mission.MissionID%>' class='mission left' onclick='window.location="<%= Url.Action("Detail", new {id= mission.MissionID}) %>"' title="$mission$<%= mission.MissionID%>" >
	<h3 class="mission_title"><%=Html.Encode(mission.Name)%></h3>
	<span class="mission_credit">By <%=Html.Encode(mission.Account.Name)%></span>
	<img width='96' height='96' border='1' src='<%=Url.Content(string.Format("~/img/missions/{0}.png", mission.MissionID)) %>' class='left' />
<table>
	<tr>
	<td>Rating:</td>
	<td><%= Html.Stars(StarType.GreenStarSmall, mission.Rating) %></td>
	</tr>
	<tr>
	<td>Difficulty:</td>
	<td><%= Html.Stars(StarType.RedStarSmall, mission.Difficulty) %></td>
	</tr>
	<tr>
	<td colspan='2'><small><%= string.Join("<br/>",mission.GetPseudoTags()) %></small></td>
	</tr>
</table>
<span style="float:left;"><%= mission.TopScoreLine != null ? "Record:" + mission.TopScoreLine : "" %></span>
</div>
<%
	}
%>
