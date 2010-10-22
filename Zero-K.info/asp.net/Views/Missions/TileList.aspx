<%@ Page Language="C#" Inherits="System.Web.Mvc.ViewPage<System.Linq.IQueryable<ZkData.Mission>>" %>

<%	
	foreach (var mission in Model)
	{
%>
<span class="mission-tile">
	<h3>
		<%=Html.Encode(mission.Name)%></h3>
	<img src='<%=Url.Action("Img", new { id = mission.MissionID })%>' />
	Author:
	<%=Html.Encode(mission.Account.Name)%>
</span>
<%
	}
%>
