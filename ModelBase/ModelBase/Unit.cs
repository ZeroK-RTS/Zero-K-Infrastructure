using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ModelBase
{
	public partial class Unit
	{
		partial void OnValidate(System.Data.Linq.ChangeAction action)
		{
			if (action == System.Data.Linq.ChangeAction.Update || action== System.Data.Linq.ChangeAction.Insert) {
				if (ModelProgress < 0) ModelProgress = 0;
				if (ModelProgress > 100) ModelProgress = 100;

				if (TextureProgress < 0) TextureProgress = 0;
				if (TextureProgress > 100) TextureProgress = 100;

				if (ScriptProgress < 0) ScriptProgress = 0;
				if (ScriptProgress > 100) ScriptProgress = 100;


				OverallProgress = (int)(50 * (LicenseType/2.0) + 20 * (ModelProgress/100.0) + 20 * (TextureProgress/100.0) + 10 * (ScriptProgress/100.0));
				if (Global.LoggedUserID != null) LastChangeUserID = Global.LoggedUserID;
			}
		}
	}
}
