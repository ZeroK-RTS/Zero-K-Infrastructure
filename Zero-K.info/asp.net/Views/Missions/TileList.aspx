<%@ Page Language="C#" Inherits="System.Web.Mvc.ViewPage<System.Linq.IQueryable<ZkData.Mission>>" %>

<%	
	foreach (var mission in Model)
	{
%>
<a href='<%= Url.Action("Detail", new {id= mission.MissionID}) %>' style='color:white;'>
<span class="mission-tile">
	<h3>
		<%=Html.Encode(mission.Name)%></h3>
	<img width='96' height='96' border='1' src='<%=Url.Action("Img", new { id = mission.MissionID })%>' />
	Author:
	<%=Html.Encode(mission.Account.Name)%>
</span>
</a>
<%
	}
%>
