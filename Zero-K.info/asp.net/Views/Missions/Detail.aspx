<%@ Page Title="" Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage<ZkData.Mission>" %>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
	<h1>
		<%=Model.Name%></h1>
		<h2>
			By
			<%=Model.Account.Name%></h2>
		<h2>
			<a href='<%="zerok://" +
			                  Html.Encode(Url.Action("Detail", "Missions", new { id = Model.MissionID }, Request.Url.Scheme) + "@start_mission:" + Model.Name)%>'>
				PLAY NOW</a>
		</h2>
				<p class="border">
			<%=Html.Encode(Model.Description)%>
		<br /><br />
		
		Players:	<%=Model.MinHumans%> - <%=Model.MaxHumans%><br/>
		Map: <%=Model.Map%><br />
		Game: <%=Model.Mod%><br />
		Created: <%=Model.CreatedTime.ToLocalTime()%><br />
		Changed: <%=Model.ModifiedTime.ToLocalTime()%> (revision<%=Model.Revision%>)<br />
		Downloads: <%=Model.Resources.DownloadCount%><br />
	</p>
	<p>
		<%:Html.ActionLink("Back to List", "Index")%>
	</p>
</asp:Content>
