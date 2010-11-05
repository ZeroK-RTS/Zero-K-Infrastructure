<%@ Page Title="" Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage<ZeroKWeb.Controllers.MissionDetailData>" %>
<%@ Import Namespace="PlasmaShared" %>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
	<h1>
	<%var m = Model.Mission; %>
		<%=m.Name%></h1>
		<h2>
			By
			<%=m.Name%></h2>
		<h2>
			<a href='<%="spring://" +
			                  Html.Encode(Url.Action("Detail", "Missions", new { id = m.MissionID }, Request.Url.Scheme) + "@start_mission:" + m.Name)%>'>
				PLAY NOW</a>
		</h2>
				<p class="border">
			<%=Html.Encode(Model.Mission.Description)%>
		<br /><br />
		
		Players:	<%=m.MinHumans%> - <%=m.MaxHumans%><br/>
		Map: <%=m.Map%><br />
		Game: <%=m.Mod%><br />
		Created: <%=m.CreatedTime.ToLocalTime()%><br />
		Changed: <%=m.ModifiedTime.ToLocalTime()%> (revision<%=m.Revision%>)<br />
		Downloads: <%=m.Resources.DownloadCount%><br />
		Runs: <%= m.MissionRunCount%> <br />
	</p>
	<h3>Top score</h3>
	<p>
	<ul>
		<% foreach (var score in Model.TopScores)
     {
     	%>
				<li><%= score.Score %>   <%=score.Account.Name %>  <%= score.GameSeconds > 0 ? "in " + Utils.PrintTimeRemaining(score.GameSeconds) : "" %></li>
				<%
			 
     }
  
	 %>
	 </ul>
	</p>

	<p>
		<%:Html.ActionLink("Back to List", "Index")%>
	</p>
</asp:Content>
