<%@ Page Title="" Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage<ZkData.Mission>" %>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <h2>Detail</h2>
    <fieldset>
        <legend>Fields</legend>
        
				<h2>
				<a href='
				<%= "zerok://" + Html.Encode(string.Format("http://zero-k.info/missions.mvc/detail/{0}@start_mission:{0}", Model.MissionID))
							 %>'>PLAY NOW</a>
							 </h2>


        <div class="display-label">MissionID</div>
        <div class="display-field"><%: Model.MissionID %></div>
        
        <div class="display-label">Name</div>
        <div class="display-field"><%: Model.Name %></div>
        
        <div class="display-label">Mod</div>
        <div class="display-field"><%: Model.Mod %></div>
        
        <div class="display-label">Map</div>
        <div class="display-field"><%: Model.Map %></div>
        
        <div class="display-label">Description</div>
        <div class="display-field"><%: Model.Description %></div>
        
        <div class="display-label">DownloadCount</div>
        <div class="display-field"><%: Model.DownloadCount %></div>
        
        <div class="display-label">CreatedTime</div>
        <div class="display-field"><%: String.Format("{0:g}", Model.CreatedTime) %></div>
        
        <div class="display-label">ModifiedTime</div>
        <div class="display-field"><%: String.Format("{0:g}", Model.ModifiedTime) %></div>
        
        <div class="display-label">ScoringMethod</div>
        <div class="display-field"><%: Model.ScoringMethod %></div>
        
        <div class="display-label">TopScoreLine</div>
        <div class="display-field"><%: Model.TopScoreLine %></div>
        
        <div class="display-label">MissionEditorVersion</div>
        <div class="display-field"><%: Model.MissionEditorVersion %></div>
        
        <div class="display-label">SpringVersion</div>
        <div class="display-field"><%: Model.SpringVersion %></div>
        
        <div class="display-label">Revision</div>
        <div class="display-field"><%: Model.Revision %></div>
        
        <div class="display-label">Dependencies</div>
        <div class="display-field"><%: Model.Dependencies %></div>
        
        <div class="display-label">TokenCondition</div>
        <div class="display-field"><%: Model.TokenCondition %></div>
        
        <div class="display-label">CampaignID</div>
        <div class="display-field"><%: Model.CampaignID %></div>
        
        <div class="display-label">AccountID</div>
        <div class="display-field"><%: Model.AccountID %></div>
        
        <div class="display-label">ModOptions</div>
        <div class="display-field"><%: Model.ModOptions %></div>
        
    </fieldset>
    <p>
				<%: Html.ActionLink("Back to List", "Index") %>
    </p>
</asp:Content>


