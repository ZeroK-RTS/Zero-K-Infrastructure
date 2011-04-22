using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Transactions;
using System.Web.UI;
using System.Xml;
using ZkData;

namespace ZeroKWeb
{
	public partial class StructuresAdmin: Page
	{
		public void ImportUnitUnlocks()
		{
			var db = new ZkDataContext();
			db.StructureTypes.DeleteAllOnSubmit(db.StructureTypes.Where(x=>x.EffectUnlockID != null));
			db.SubmitChanges();

			foreach (var u in db.Unlocks.Where(x => x.UnlockType == UnlockTypes.Unit)) {
				db.StructureTypes.InsertOnSubmit(new StructureType() {
					Cost = u.XpCost,
					Name = u.Name,
					Description = "Gives your clan access to " + u.Name,
					EffectUnlockID = u.UnlockID,
					IsIngameDestructible = true,
					MapIcon = u.Code + ".png",
					DestroyedMapIcon = u.Code + "_dead.png",
					IngameUnitName = "pw_" + u.Code
				});

			}
			db.SubmitChanges();
		}


		protected void Page_Load(object sender, EventArgs e)
		{

			if (!IsPostBack)
			{
				var db = new ZkDataContext();
				var data = db.StructureTypes.ToList();
				GridView1.DataSource = data;
				GridView1.DataBind();
				var sb = new StringBuilder();

				using (
					var writer = XmlWriter.Create(new StringWriter(sb),
					                              new XmlWriterSettings { Indent = true, CheckCharacters = true })) new DataContractSerializer(data.GetType()).WriteObject(writer, data);
				tbData.Text = sb.ToString();
			}
		}

		protected void btnUpdateClick(object sender, EventArgs e)
		{
			if (!Global.Account.IsZeroKAdmin) throw new ApplicationException("You are not an admin!");
			var data = (List<StructureType>)new DataContractSerializer(typeof(List<StructureType>)).ReadObject(XmlReader.Create(new StringReader(tbData.Text)));

			var db2 = new ZkDataContext();
			//using (var scope = new TransactionScope())
			{
				using (var db = new ZkDataContext())
				{
					// first pass insert structures but without interdependencies links 
					foreach (var s in data)
					{
						s.UpgradesToStructureID = null;
						s.IngameDestructionNewStructureTypeID = null;

						var org = db2.StructureTypes.SingleOrDefault(x => x.StructureTypeID == s.StructureTypeID);
						if (org != null) db.StructureTypes.Attach(s, org);
						else db.StructureTypes.InsertOnSubmit(s);
					}

					db.SubmitChanges();
				}

				data = (List<StructureType>)new DataContractSerializer(typeof(List<StructureType>)).ReadObject(XmlReader.Create(new StringReader(tbData.Text)));

				using (var db = new ZkDataContext())
				{
					foreach (var s in data)
					{
						var ds = db.StructureTypes.Single(x => x.StructureTypeID == s.StructureTypeID);
						ds.UpgradesToStructureID = s.UpgradesToStructureID;
						ds.IngameDestructionNewStructureTypeID = s.IngameDestructionNewStructureTypeID;
					}
					db.SubmitChanges();
				}

				//scope.Complete();
			}
		}
	}
}