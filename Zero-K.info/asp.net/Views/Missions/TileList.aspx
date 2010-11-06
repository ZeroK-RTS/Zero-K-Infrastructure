<%@ Page Language="C#" Inherits="System.Web.Mvc.ViewPage<System.Linq.IQueryable<ZkData.Mission>>" %>

<%	
	foreach (var mission in Model)
	{
%>
<div id='<%=mission.MissionID%>' class='mission left' onclick='window.location="<%= Url.Action("Detail", new {id= mission.MissionID}) %>"' >
	<span class="abstop left mission_credit">By <%=Html.Encode(mission.Account.Name)%></span>
	<h3 class="mission_title"><%=Html.Encode(mission.Name)%></h3>
	<img width='96' height='96' border='1' src='<%=Url.Content(string.Format("~/img/missions/{0}.png", mission.MissionID)) %>' class='left' />
	<p><%=mission.Description %></p>
		Rating: <span class="cgreen">* * * * *</span> <span class="cgray"></span> <br />
		Difficulty: <span class="cred">* * * *</span> <span class="clight">*</span> <br />
</div>
<%
	}
%>
