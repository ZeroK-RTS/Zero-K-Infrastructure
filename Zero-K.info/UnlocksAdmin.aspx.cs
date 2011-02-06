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
      if (!Global.IsAdmin) throw new ApplicationException("You are not an admin!");
      if (!IsPostBack)
      {
        foreach (int kvp in Enum.GetValues(typeof(UnlockTypes)))
        {
          ddType.Items.Add(new ListItem(Enum.GetName(typeof(UnlockTypes), kvp), kvp.ToString()));
        }
      }
    }

    protected void btnAdd_Click(object sender, EventArgs e)
    {
      if (!Global.IsAdmin) throw new ApplicationException("You are not an admin!");
      var db = new ZkDataContext();
      var unlock = new Unlock()
                   {
                     Code = tbCode.Text,
                     Name = tbName.Text,
                     Description = tbDescription.Text,
                     NeededLevel = int.Parse(tbMinLevel.Text),
                     RequiredUnlockID = string.IsNullOrEmpty(tbPreq.Text) ? null : (int?)int.Parse(tbPreq.Text),
                     UnlockType = (UnlockTypes)int.Parse(ddType.SelectedValue),
                     MorphLevel = string.IsNullOrEmpty(tbMorphLevel.Text) ? 0 : int.Parse(tbMorphLevel.Text),
                     LimitForChassis = tbChassisLimit.Text,
                   };
      db.Unlocks.InsertOnSubmit(unlock);
      db.SubmitChanges();
      GridView1.DataBind();
    }
  }
}