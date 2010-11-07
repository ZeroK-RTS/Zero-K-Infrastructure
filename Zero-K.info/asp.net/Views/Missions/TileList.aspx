<%@ Page Language="C#" Inherits="System.Web.Mvc.ViewPage<System.Linq.IQueryable<ZkData.Mission>>" %>

<%	
	foreach (var mission in Model)
	{
%>
<div id='<%=mission.MissionID%>' class='mission left' onclick='window.location="<%= Url.Action("Detail", new {id= mission.MissionID}) %>"' title="$mission$<%= mission.MissionID%>" >
	<span class="abstop left mission_credit">By <%=Html.Encode(mission.Account.Name)%></span>
	<h3 class="mission_title"><%=Html.Encode(mission.Name)%></h3>
	<img width='96' height='96' border='1' src='<%=Url.Content(string.Format("~/img/missions/{0}.png", mission.MissionID)) %>' class='left' />
<table>
	<tr>
	<td>Rating:</td>
	<td><span class="greenStarSmall" style="width:70px;"></span></td>
	</tr>
	<tr>
	<td>Difficulty:</td>
	<td><span class="redStarSmall" style="width:70px;"></span></td>
	</tr>
	<tr>
	<td colspan='2'>short,chickens,coop</td>
	</tr>
</table>
<span style="float:left">Record: [LCC]Licho[MVC][0K][CA]</span>
</div>
<%
	}
%>
