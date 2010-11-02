<%@ Page Language="C#" Inherits="System.Web.Mvc.ViewPage<System.Linq.IQueryable<ZkData.Mission>>" %>

<%	
	foreach (var mission in Model)
	{
%>
<div class="tile" >
	<a href='<%= Url.Action("Detail", new {id= mission.MissionID}) %>'>
		<h3>
			<%=Html.Encode(mission.Name)%></h3>
		<img width='96' height='96' border='1' src='<%=Url.Action("Img", new { name = mission.Name })%>' />
		By <%=Html.Encode(mission.Account.Name)%>
	</a>
</div>
<%
	}
%>
