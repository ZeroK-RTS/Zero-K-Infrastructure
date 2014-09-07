using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using ZkData;


namespace ZeroKWeb
{
  public partial class UnlocksAdmin : System.Web.UI.Page
  {
    protected void Page_Load(object sender, EventArgs e)
    {
   
      if (Global.Account == null || !(Global.Account.IsZeroKAdmin || Global.Account.IsLobbyAdministrator)) throw new ApplicationException("You are not an admin!");
      if (!IsPostBack)
      {
        foreach (int kvp in Enum.GetValues(typeof(UnlockTypes)))
        {
          ddType.Items.Add(new ListItem(Enum.GetName(typeof(UnlockTypes), kvp), kvp.ToString()));
        }
      }
    }

    private int? GetInt(string text)
    {
      if (string.IsNullOrEmpty(text)) return null;
      else return int.Parse(text);
    }


    protected void btnAdd_Click(object sender, EventArgs e)
    {
			if (!Global.Account.IsZeroKAdmin && !Global.Account.IsLobbyAdministrator) throw new ApplicationException("You are not an admin!");
			var db = new ZkDataContext();
      var unlock = new Unlock()
                   {
                     Code = tbCode.Text,
                     Name = tbName.Text,
                     XpCost =  int.Parse(tbXpCost.Text),
                     Description = tbDescription.Text,
                     NeededLevel = int.Parse(tbMinLevel.Text),
                     RequiredUnlockID = GetInt(tbPreq.Text),
                     UnlockType = (UnlockTypes)int.Parse(ddType.SelectedValue),
                     MorphLevel = GetInt(tbMorphLevel.Text) ?? 0,
                     MaxModuleCount =GetInt(tbMaxCount.Text) ?? 1,
                     LimitForChassis = tbChassisLimit.Text,
                     MetalCost = GetInt(tbMetalCost.Text),
                     MetalCostMorph2 = GetInt(tbMorph2.Text),
                     MetalCostMorph3 = GetInt(tbMorph3.Text),
                     MetalCostMorph4 = GetInt(tbMorph4.Text),
                     MetalCostMorph5 = GetInt(tbMorph5.Text)
                   };
      db.Unlocks.InsertOnSubmit(unlock);
      db.SubmitChanges();
      GridView1.DataBind();
    }

    protected void LinqDataSource1_Deleting(object sender, LinqDataSourceDeleteEventArgs e)
    {
      var db = new ZkDataContext();
      var unlock = db.Unlocks.Single(x => x.UnlockID == ((Unlock)e.OriginalObject).UnlockID);
      db.CommanderModules.DeleteAllOnSubmit(db.CommanderModules.Where(x=>x.ModuleUnlockID == unlock.UnlockID));
      db.Unlocks.DeleteOnSubmit(unlock);
      db.SubmitChanges();
    }
  }
}
