<%@ Page Title="" Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage<ZeroKWeb.Controllers.MissionDetailData>" %>
<%@ Import Namespace="PlasmaShared" %>
<%@ Import Namespace="ZeroKWeb" %>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
	<%=Html.ActionLink("Back to List", "Index")%>

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
			<small><%=string.Join("<br/>", m.GetPseudoTags())%></small><br />
			Map:
			<%=m.Map%><br />
			Game:
			<%=m.Mod ?? m.ModRapidTag%><br />
			Created:<%=m.CreatedTime.ToLocalTime()%><br />
			<%
				if (m.Revision > 0)
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
			<%=m.Resources.DownloadCount%><br />
			<%
    }
%>
			Runs:
			<%=m.MissionRunCount%>
			<br />
		</p>
		<script type="text/javascript">
			$(document).ready(function () {
				$("#rating").stars({
					callback: function (ui, type, value) {
						$.get('<%=Url.Action("Rate", "Missions", new { id = m.MissionID })%>?rating=' + value, function (ret) { if (ret != "") alert(ret); });
					}
				});

				$("#difficulty").stars({
					callback: function (ui, type, value) {
						$.get('<%=Url.Action("Rate", "Missions", new { id = m.MissionID })%>?difficulty=' + value, function (ret) { if (ret != "") alert(ret); });
					}
				});

			});

		</script>
		<div>
			<table>
				<tr>
					<td>
						Rating:
					</td>
					<td width='80px'>
						<%=Html.Stars(StarType.GreenStarSmall, m.Rating)%>
					</td>
					<td>
						<div id='rating'>
							<%=Html.Select("rating",
			                              new List<SelectOption>()
			                              {
			                              	new SelectOption() { Value = "1", Name = "Poor" },
			                              	new SelectOption() { Value = "2", Name = "Below average" },
			                              	new SelectOption() { Value = "3", Name = "Average" },
			                              	new SelectOption() { Value = "4", Name = "Good" },
			                              	new SelectOption() { Value = "5", Name = "Awesome" }
			                              },
			                              Model.MyRating.Rating1.ToString())%>


						</div>
					</td>
				</tr>
				<tr>
					<td>
						Difficulty:
					</td>
					<td>
						<%=Html.Stars(StarType.RedStarSmall, m.Difficulty)%>
					</td>
					<td>
						<div id='difficulty'>
							<%=Html.Select("difficulty",
			                              new List<SelectOption>()
			                              {
			                              	new SelectOption() { Value = "1", Name = "Trivial" },
			                              	new SelectOption() { Value = "2", Name = "Easy" },
			                              	new SelectOption() { Value = "3", Name = "Average" },
			                              	new SelectOption() { Value = "4", Name = "Hard" },
			                              	new SelectOption() { Value = "5", Name = "Impossible" }
			                              },
			                              Model.MyRating.Difficulty.ToString())%>
						</div>
					</td>
				</tr>
			</table>
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
			<%=m.ScoringMethod%>
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
	</div>
	<form method="post" action="<%=Url.Action("SubmitPost", "Forum", new { threadID = m.ForumThreadID })%>">
		<textarea name="text" rows="5" cols="80"></textarea><br />
		<input type="submit" />
	</form>
	<%
				foreach (var p in Model.Posts )
    {%>
		<span class="post">
			<span class="post-header">
				<%=p.Account.Name%><br />
				<%=p.Created%>
			</span>
			<span class="post-body"><%=p.Text%></span>
		</span>

	<%
    }%>
		<br />


	<!close wrapper>
	<%=Html.ActionLink("Back to List", "Index")%>
</asp:Content>
