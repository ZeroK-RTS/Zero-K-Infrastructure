<%@ Page Title="" Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage<ZeroKWeb.Controllers.MissionDetailData>" %>

<%@ Import Namespace="PlasmaShared" %>
<%@ Import Namespace="ZeroKWeb" %>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
	<div class="wrapper">
		<h1>
			<%
				var m = Model.Mission;%><%=m.Name%></h1>
		<h2>
			By
			<%=m.Account.Name%></h2>
		<%
			if (m.MinHumans < 2)
			{%>
		<h2>
			<a href='<%="spring://" +
    	                  Html.Encode(Url.Action("Detail", "Missions", new { id = m.MissionID }, Request.Url.Scheme) +
    	                              (m.IsScriptMission ? "@start_script_mission:" : "@start_mission:") + m.Name)%>'>
				PLAY NOW</a></h2>
		<%
			}%>
		<%
			if (!m.IsScriptMission)
			{%>
		<h2>
			<a href='<%="spring://" +
    	                  Html.Encode(Url.Action("Detail", "Missions", new { id = m.MissionID }, Request.Url.Scheme) + "@host_mission:" + m.Name)%>'>
				HOST IN MULTIPLAYER</a></h2>
		<%
			}%>
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
			<%=m.Mod ?? m.ModRapidTag%><br />
			Created:<%=m.CreatedTime.ToLocalTime()%><br />
			<% if (m.Revision > 0)
			{%>
			Changed:
			<%=m.ModifiedTime.ToLocalTime()%>
			(revision
			<%=m.Revision%>)<br />
			<%
				}
			if (m.Resources != null)
			{
			%>
			Downloads:
			<%= m.Resources.DownloadCount%><br />
			<%
				}
			%>
			Runs:
			<%=m.MissionRunCount%>
			<br />
		</p>
		<div>
			<form method="post" action="<%= Url.Action("Rate","Missions",new { id = m.MissionID}) %>">
			<table>
				<tr>
					<td>
						Rating:
					</td>
					<td>
						<%=Html.Stars(StarType.GreenStarSmall, m.Rating) %>
					</td>
					<td>
						<div class='rating'>
							<select name="rating">
								<option value='1'>Poor</option>
								<option value='2'>Below average</option>
								<option value='3'>Average</option>
								<option value='4'>Good</option>
								<option value='5'>Awesome</option>
							</select>
						</div>
					</td>
				</tr>
				<tr>
					<td>
						Difficulty:
					</td>
					<td>
						<%=Html.Stars(StarType.RedStarSmall, m.Difficulty) %>
					</td>
					<td>
						<div class='rating'>
							<select name="difficulty">
								<option value='1'>Trivial</option>
								<option value='2'>Easy</option>
								<option value='3'>Average</option>
								<option value='4'>Hard</option>
								<option value='5'>Impossible</option>
							</select>
						</div>
					</td>
				</tr>
				<tr>
					<td colspan='3'>
						<input type="submit" value="Submit" />
					</td>
				</tr>
			</table>
			</form>
		</div>
		<%
			if (!Global.IsAccountAuthorized && !m.IsScriptMission)
			{%>
		Manual download: <a href='<%=Url.Action("File", "Missions", new { name = m.Name })%>'>
			<%=m.SanitizedFileName%></a> and <a href='<%=Url.Action("Script", "Missions", new { id = m.MissionID })%>'>
				script.txt</a>
		<%
			}
			if (Global.IsAccountAuthorized && Global.Account.IsLobbyAdministrator)
			{%>
		<a href='<%=Url.Action("Delete", "Missions", new { id = m.MissionID })%>' class='delete'>
			Delete mission</a>
		<%
			}%>
		<h3>
			Top scores</h3>
		<div class="border">
			<ul>
				<%
					foreach (var score in Model.TopScores)
					{%>
				<li>
					<%=score.Score%>
					<%=score.Account.Name%>
					<%=score.GameSeconds > 0 ? "in " + Utils.PrintTimeRemaining(score.GameSeconds) : ""%></li>
				<%
					}%>
			</ul>
		</div>
		<%:Html.ActionLink("Back to List", "Index")%>
	</div>
	<!close wrapper>
</asp:Content>
