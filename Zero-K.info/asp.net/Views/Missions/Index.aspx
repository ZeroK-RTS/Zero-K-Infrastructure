<%@ Page Title="" Language="C#" Inherits="System.Web.Mvc.ViewPage<ZeroKWeb.Controllers.MissionsIndexData>"
	MasterPageFile="~/Views/Shared/Site.Master" %>

<asp:Content runat="server" ID="Title" ContentPlaceHolderID="TitleContent">
</asp:Content>
<asp:Content runat="server" ID="Main" ContentPlaceHolderID="MainContent">
	<%=Html.TextBox("MissionSearch")%><br />
	<%
		foreach (var mission in Model.LastUpdated)
		{
	%>
	<span class="mission-tile">
		<h3><%=Html.Encode(mission.ShortName)%></h3>
		<img src='<%=Url.Action("Img", new { id = mission.MissionID })%>' />
		Author: <%= Html.Encode(mission.Account.Name) %>
	</span>
	<%
		}
	%>
	<h3>
		Most popular</h3>
	<ul>
		<% foreach (var mission in Model.MostPopular.Take(15))
		 {
			 Response.Write(string.Format("<li>{0}</li>", Html.ActionLink(mission.ShortName, "Detail", new { id = mission.MissionID })));
		 } %>
	</ul>
	<h3>
		Last comments</h3>

	<ul>
		<% foreach (var mission in Model.LastCommented.Take(15))
		 {
			 Response.Write(string.Format("<li>{0}</li>", Html.ActionLink(mission.ShortName, "Detail", new { id = mission.MissionID })));
		 } %>
	</ul>

</asp:Content>
