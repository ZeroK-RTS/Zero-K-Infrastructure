<%@ Page Title="" Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage<ZeroKWeb.Controllers.MissionDetailData>" %>

<%@ Import Namespace="PlasmaShared" %>
<%@ Import Namespace="ZkData" %>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
<div class="wrapper">
	<h1><%var m = Model.Mission;%><%=m.Name%></h1>
	<h2>By <%=m.Account.Name%></h2>
	<h2><a href='<%="spring://" + 
	Html.Encode(Url.Action("Detail", "Missions", new { id = m.MissionID }, Request.Url.Scheme) + 
	(m.IsScriptMission ? "@start_script_mission:" : "@start_mission:") + m.Name)%>'> PLAY NOW</a></h2>
	
	<%if (!m.IsScriptMission) {%>
	<h2><a href='<%="spring://" + 
	Html.Encode(Url.Action("Detail", "Missions", new { id = m.MissionID }, Request.Url.Scheme) + 
	"@host_mission:" + m.Name)%>'> HOST IN MULTIPLAYER</a></h2>
	<%}%>
	
	<p class="border">
		<%=Html.Encode(Model.Mission.Description)%>
		<br />
		<br />
		Players:
		<%=m.MinHumans%>
		-
		<%=m.MaxHumans%><br />
		Map:
		<%=m.Map%><br />
		Game:
		<%=m.Mod%><br />
		Created:
		<%=m.CreatedTime.ToLocalTime()%><br />
		Changed:
		<%=m.ModifiedTime.ToLocalTime()%>
		(revision<%=m.Revision%>)<br />
		Downloads:
		<%=m.Resources != null ? m.Resources.DownloadCount.ToString() : "N/A"%><br />
		Runs:
		<%=m.MissionRunCount%>
		<br />
	</p>
	<% if (!(User is Account)) {%>
	Manual download:
	<% if (!m.IsScriptMission) {%>
	<a href='<%=Url.Action("File", "Missions", new { name = m.Name })%>'><%=m.SanitizedFileName%></a>
		and
	<%}%>
	<a href='<%=Url.Action("Script", "Missions", new { id = m.MissionID })%>'>script.txt</a>
	<%}
		else if (((Account)User).IsLobbyAdministrator)
		{%>
			<a href='<%=Url.Action("Delete","Missions", new {id = m.MissionID}) %>' class='delete'>Delete mission</a>
	
	<%}%>
	<h3>Top scores</h3>
	<div class="border">
		<ul>
			<%foreach (var score in Model.TopScores)
				{%>
			<li>
				<%=score.Score%>
				<%=score.Account.Name%>
				<%=score.GameSeconds > 0 ? "in " + Utils.PrintTimeRemaining(score.GameSeconds) : ""%></li>
			<%}%>
		</ul>
	</div>
	<%:Html.ActionLink("Back to List", "Index")%>
</div><!close wrapper>
</asp:Content>
