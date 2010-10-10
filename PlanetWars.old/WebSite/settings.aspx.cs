using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class SettingsPage : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (Globals.Player == null) return;

        var p = Globals.Player;
        
        if (!IsPostBack) {
            cbPreparing.Checked = (p.ReminderEvent & PlanetWarsShared.Springie.ReminderEvent.OnBattlePreparing) > 0;
            cbStarted.Checked = (p.ReminderEvent & PlanetWarsShared.Springie.ReminderEvent.OnBattleStarted) > 0;
            cbEnded.Checked = (p.ReminderEvent & PlanetWarsShared.Springie.ReminderEvent.OnBattleEnded) > 0;

            drTimeZone.DataSource = TimeZoneInfo.GetSystemTimeZones();
            drTimeZone.DataValueField = "Id";
            drTimeZone.DataTextField = "DisplayName";
            drTimeZone.DataBind();
            drTimeZone.SelectedValue = p.LocalTimeZone.Id;
            
            drPlanet.SelectedValue = p.ReminderLevel == PlanetWarsShared.Springie.ReminderLevel.AllBattles ? "All" : "My";
            

            cbAttacking.Checked = (p.ReminderRoundInitiative & PlanetWarsShared.Springie.ReminderRoundInitiative.Offense) > 0;
            cbDefending.Checked = (p.ReminderRoundInitiative & PlanetWarsShared.Springie.ReminderRoundInitiative.Defense) > 0;

            tbTitle.Text = p.Title;
        }
    }
    protected void btnNotification_Click(object sender, EventArgs e)
    {
        PlanetWarsShared.Springie.ReminderEvent ev = PlanetWarsShared.Springie.ReminderEvent.None;
        if (cbPreparing.Checked) ev = ev | PlanetWarsShared.Springie.ReminderEvent.OnBattlePreparing;
        if (cbStarted.Checked) ev = ev | PlanetWarsShared.Springie.ReminderEvent.OnBattleStarted;
        if (cbEnded.Checked) ev = ev | PlanetWarsShared.Springie.ReminderEvent.OnBattleEnded;


        PlanetWarsShared.Springie.ReminderLevel level = drPlanet.SelectedValue == "All" ? PlanetWarsShared.Springie.ReminderLevel.AllBattles : PlanetWarsShared.Springie.ReminderLevel.MyPlanet;

        
        PlanetWarsShared.Springie.ReminderRoundInitiative ini = 0;
        if (cbAttacking.Checked) ini = ini | PlanetWarsShared.Springie.ReminderRoundInitiative.Offense;
        if (cbDefending.Checked) ini =  ini | PlanetWarsShared.Springie.ReminderRoundInitiative.Defense;

        string message;
        if (!Globals.Server.SetReminderOptions(ev, level, ini, Globals.CurrentLogin, out message)) {
            MessageBox.Show(message);
        }
    }
    protected void btnSetTitle_Click(object sender, EventArgs e)
    {
        string message;
        if (!Globals.Server.ChangeCommanderInChiefTitle(tbTitle.Text, Globals.CurrentLogin, out message)) {
            MessageBox.Show(message);
        }
    }
    protected void btnSetTimeZone_Click(object sender, EventArgs e)
    {
        string message;
        string timeZoneID = drTimeZone.SelectedValue;
        TimeZoneInfo LocalTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneID);
        if (!Globals.Server.SetTimeZone(LocalTimeZone, Globals.CurrentLogin, out message))
        {
            MessageBox.Show(message);
        }
    }
    protected void tbSetPassword_Click(object sender, EventArgs e)
    {
        if (Globals.CurrentLogin.Password != tbCurPass.Text) {
            MessageBox.Show("Current password is incorrect");
            return;
        } 
        if (tbNewPass1.Text != tbNewPass2.Text) {
            MessageBox.Show("Password and password confirmation are not identical");
            return;
        }

        string message;
        if (!Globals.Server.ChangePlayerPassword(tbNewPass1.Text, Globals.CurrentLogin, out message)) {
            MessageBox.Show(message);
        } else {
            Globals.CurrentLogin.Password = tbNewPass1.Text;
            Response.SetCookie(new HttpCookie("password", Globals.CurrentLogin.Password) { Expires = DateTime.Now.AddDays(30) });
        }
    }
}
