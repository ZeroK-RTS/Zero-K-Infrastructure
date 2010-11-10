<%@ Page Language="C#" Inherits="System.Web.Mvc.ViewPage<System.Linq.IQueryable<ZkData.Mission>>" %>

<%	
	foreach (var mission in Model)
	{
%>
<div id='<%=mission.MissionID%>' class='mission left' onclick='window.location="<%= Url.Action("Detail", new {id= mission.MissionID}) %>"' title="$mission$<%= mission.MissionID%>" >
	<b><%=Html.Encode(mission.Name)%></b><br/>
	<%=Html.PrintAccount(mission.Account)%><br/>
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
<span><%= mission.TopScoreLine != null ? string.Format("<small><img src='/img/cup.png' class='icon16'>{0}</small>", mission.TopScoreLine) : ""%></span>
</div>
<%
	}
%>
