#region using

using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

#endregion

namespace ModStats
{
	public partial class Default : Page
	{
		#region Fields

		private bool invalidateUnits;

		private int restrictGamesToID
		{
			get
			{
				if (ViewState["restrictID"] == null) return 0;
				else return (int) ViewState["restrictID"];
			}
			set { ViewState["restrictID"] = value; }
		}

		#endregion

		#region Constructors

		public Default()
		{
			Load += Default_Load;
		}

		#endregion

		#region Other methods

		protected void btnFilterGames_Click(object sender, EventArgs e)
		{
			restrictGamesToID = 0;
			RefreshGameFilter();
		}

		protected void btnFilterUnits_Click(object sender, EventArgs e)
		{
			gridUnits.DataBind();
		}

		protected void btnHideKillers_Click(object sender, EventArgs e)
		{
			Panel1.Visible = false;
		}


		protected void btnMatrix_Click(object sender, EventArgs e)
		{
			gridMatrix.DataBind();
		}

		private IQueryable<Game> GamesFilter(bool ignoreRestrict)
		{
      if (!IsPostBack) return new List<Game>().AsQueryable();

      Table<Game> src = Global.Db.Games;

			if (!ignoreRestrict && restrictGamesToID != 0) return src.Where(x => x.GameID == restrictGamesToID);

			int pmin = int.Parse(tbPlayersMin.Text);
			int pmax = int.Parse(tbPlayersMax.Text);
			int tmin = int.Parse(tbTeamsMin.Text);
			int tmax = int.Parse(tbTeamsMax.Text);

			int vmin = int.Parse(tbVersionMin.Text);
			int vmax = int.Parse(tbVersionMax.Text);

			IQueryable<Game> ret;

			if (pmin > 0 || pmax < 16 || tmin > 0 || tmax < 16 || vmin > 0 || vmax < 99999) ret = src.Where(g => g.Teams >= tmin && g.Teams <= tmax && g.Players >= pmin && g.Players <= pmax && g.Version >= vmin && g.Version <= vmax);
			else ret = src;

			if (!string.IsNullOrEmpty(tbModName.Text)) ret = ret.Where(x => x.Mod.StartsWith(tbModName.Text));

			if (!string.IsNullOrEmpty(tbMapName.Text)) ret = ret.Where(x => x.Mod.StartsWith(tbMapName.Text));

			if (!string.IsNullOrEmpty(tbPlayer.Text)) ret = ret.Where(x => x.PlayerList.Contains(tbPlayer.Text));

			return ret.OrderByDescending(x=>x.GameID).Take(500);
		}

		protected void gridUnits_RowCommand(object sender, GridViewCommandEventArgs e)
		{
			if (e.CommandName == "victims") {
				victimsUnitKey.Value = (string) e.CommandArgument;
				Panel1.Visible = true;
				gridVictims.DataBind();
				gridKillers.DataBind();
			}
		}


		protected void GridView1_RowCommand(object sender, GridViewCommandEventArgs e)
		{
			if (e.CommandName == "filter") {
				restrictGamesToID = int.Parse(e.CommandArgument.ToString());
				RefreshGameFilter();
			}
		}

		protected void LinqDataSourceGames_Selecting(object sender, LinqDataSourceSelectEventArgs e)
		{
			e.Result = GamesFilter(true);
		}

		protected void LinqDataSourceUnits_Selecting(object sender, LinqDataSourceSelectEventArgs e)
		{
			List<UnitRow> resList;

			if (!invalidateUnits && Session["units"] != null) resList = (List<UnitRow>) Session["units"];
			else {
				IQueryable<Game> gameFilter = from game in GamesFilter(false)
				                              select game;


				var ret = from g in gameFilter
				          let totalSpending = gameFilter.SelectMany(x => x.Units).Sum(x => (double?) x.Cost*x.Created)
				          let totalDamage = gameFilter.SelectMany(x => x.Damages).Sum(x => (double?)x.Damage1)
				          from u in g.Units
				          let unitDamageDone = g.Damages.Where(x => x.AttackerUnit == u.Unit1).Sum(x => (double?)x.Damage1)
				          let unitDamageRecieved = g.Damages.Where(x => x.VictimUnit == u.Unit1).Sum(x => (double?)x.Damage1)
				          
						  let costParalyze =
				          	g.Damages.Where(x => x.AttackerUnit == u.Unit1).Sum(
				          	x => x.Paralyze*g.Units.Where(y => y.Unit1 == x.VictimUnit).Select(y => (double?) y.Cost/y.Health).Single())
				          let costDamageDone =
				          	g.Damages.Where(x => x.AttackerUnit == u.Unit1).Sum(
				          	x => x.Damage1*g.Units.Where(y => y.Unit1 == x.VictimUnit).Select(y => (double?) y.Cost/y.Health).Single())
				          select
				          	new
				          		{
				          			Name = u.Unit1,
				          			unitDamageDone,
				          			unitDamageRecieved,
				          			totalDamage,
				          			totalSpending,
				          			u.Cost,
				          			u.Created,
				          			u.Destroyed,
				          			u.Health,
				          			costDamageDone,
				          			costDamageRecieved = unitDamageRecieved*u.Cost/u.Health,
				          			costParalyze
				          		};
				var pom = ret.ToList();


				int totalGames = gameFilter.Count();
				Label1.Text = totalGames.ToString();

				IQueryable<UnitRow> result = from u in ret
				                             group u by u.Name
				                             into grp let thisUnitSpending = grp.Sum(x => (double?) x.Created*x.Cost)
				                             	let costLost = grp.Sum(x => (double?) x.costDamageRecieved)
												 let damageDone = grp.Sum(x => (double?)x.unitDamageDone)
												 let costDamaged = grp.Sum(x => (double?) x.costDamageDone)
												 let costParalyzed = grp.Sum(x => (double?)x.costParalyze)
				                             	select
				                             	new UnitRow(grp.Key,
				                             	            grp.Average(x => (double?)x.Cost),
				                             	            grp.Sum(x => (double?) x.Created)/totalGames,
				                             	            grp.Sum(x => (double?) x.Destroyed)/totalGames,
				                             	            damageDone/costLost,
				                             	            costDamaged/costLost,
				                             	            100.0*thisUnitSpending/grp.Select(x => x.totalSpending).First(),
				                             	            100.0*damageDone/grp.Select(x => x.totalDamage).First(),
				                             	            costDamaged/thisUnitSpending,
				                             	            costParalyzed/thisUnitSpending,
				                             	            100.0*grp.Count(x => x.Created > 0)/totalGames,
				                             	            costParalyzed/costLost,
				                             	            grp.Average(x => (double?)x.Health));

				resList = result.ToList();
				Session["units"] = resList;
			}
			List<string> unitFilter = unitSelector.GetSelectedUnits();
			if (unitFilter.Count > 0) resList = resList.Where(u => unitFilter.Contains(u.Name)).ToList();

			e.Result = resList;
		}

		protected void MatrixDataSource_Selecting(object sender, LinqDataSourceSelectEventArgs e)
		{
			List<string> matrixFilter = UnitSelector2.GetSelectedUnits();

			var ret = from g in GamesFilter(false)
			          from u in g.Units
			          where matrixFilter.Contains(u.Unit1)
			          from u2 in g.Units
			          where matrixFilter.Contains(u2.Unit1) && u2.Unit1 != u.Unit1
			          let d = from x in g.Damages
			                  where (x.AttackerUnit == u.Unit1 || x.AttackerUnit == u2.Unit1) && (x.VictimUnit == u.Unit1 || x.VictimUnit == u2.Unit1)
			                  select x
			          let unitDamageDone = d.Where(x => x.AttackerUnit == u.Unit1 && x.VictimUnit == u2.Unit1).Sum(x => x.Damage1)
			          let unitDamageRecieved = d.Where(x => x.VictimUnit == u.Unit1 && x.AttackerUnit == u2.Unit1).Sum(x => x.Damage1)
			          let costDamageDone =
			          	d.Where(x => x.AttackerUnit == u.Unit1 && x.VictimUnit == u2.Unit1).Sum(
			          	x => x.Damage1*g.Units.Where(y => y.Unit1 == x.VictimUnit).Select(y => (double) y.Cost/y.Health).Single())
			          let costParalyze =
			          	d.Where(x => x.AttackerUnit == u.Unit1 && x.VictimUnit == u2.Unit1).Sum(
			          	x => x.Paralyze*g.Units.Where(y => y.Unit1 == x.VictimUnit).Select(y => (double) y.Cost/y.Health).Single())
			          select
			          	new
			          		{
			          			Name = u.Unit1,
			          			Name2 = u2.Unit1,
			          			unitDamageDone,
			          			unitDamageRecieved,
			          			u.Cost,
			          			u.Created,
			          			Created2 = u2.Created,
			          			u.Destroyed,
			          			u.Health,
			          			costDamageDone,
			          			costDamageRecieved = unitDamageRecieved*u.Cost/u.Health,
			          			costParalyze
			          		};


			e.Result = from u in ret
			           group u by new {u.Name, u.Name2}
			           into grp
			           	select
			           	new
			           		{
			           			grp.Key.Name,
			           			grp.Key.Name2,
			           			DamageEffectivity = grp.Sum(x => (double?) x.unitDamageDone)/grp.Sum(x => (double?) x.unitDamageRecieved),
			           			DamageCostEffectivity = grp.Sum(x => (double?) x.costDamageDone)/grp.Sum(x => (double?) x.costDamageRecieved),
			           			ParalyzeCostEffectivity = grp.Sum(x => (double?) x.costParalyze)/grp.Sum(x => (double?) x.costDamageRecieved),
			           			DamageDoneTotal = grp.Sum(x => (double?) x.unitDamageDone)
			           		};
		}

		protected void Page_Load(object sender, EventArgs e) {}

		bool queryEnabled = false;

		private void RefreshGameFilter()
		{
			queryEnabled = true;
			invalidateUnits = true;
			GridView1.DataBind();
			gridUnits.DataBind();
			gridMatrix.DataBind();
		}

		#endregion

		#region Event Handlers




		private void Default_Load(object sender, EventArgs e)
		{
			if (!IsPostBack) {
				GridView1.Sort("Created", SortDirection.Descending);
			}
		}

		#endregion

		#region Nested type: UnitRow

		public class UnitRow
		{
			#region Properties

			public double? Cost { get; private set; }
			public double? CostParalyzedPerLost { get; private set; }
			public double? Created { get; private set; }
			public double? DamageCostEffectivity { get; private set; }
			public double? DamageDonePercentage { get; private set; }
			public double? DamageDonePerCost { get; private set; }
			public double? DamageEffectivity { get; private set; }
			public double? Destroyed { get; private set; }
			public double? GamesUsedPercentage { get; private set; }
			public double? Health { get; private set; }
			public string Name { get; private set; }
			public double? ParalyzeDonePerCost { get; private set; }
			public double? SpendingPercentage { get; private set; }

			#endregion

			#region Constructors

			public UnitRow(string name,
			               double? cost,
			               double? created,
			               double? destroyed,
			               double? damageEffectivity,
			               double? damageCostEffectivity,
			               double? spendingPercentage,
			               double? damageDonePercentage,
			               double? damageDonePerCost,
			               double? paralyzeDonePerCost,
			               double? gamesUsedPercentage,
			               double? costParalyzedPerLost,
			               double? health)
			{
				Name = name;
				Cost = cost;
				Created = created;
				Destroyed = destroyed;
				DamageEffectivity = damageEffectivity;
				DamageCostEffectivity = damageCostEffectivity;
				SpendingPercentage = spendingPercentage;
				DamageDonePercentage = damageDonePercentage;
				DamageDonePerCost = damageDonePerCost;
				ParalyzeDonePerCost = paralyzeDonePerCost;
				GamesUsedPercentage = gamesUsedPercentage;
				CostParalyzedPerLost = costParalyzedPerLost;
				Health = health;
			}

			#endregion
		}

		#endregion

	    protected void LinqDataSourceVictims_Selecting(object sender, LinqDataSourceSelectEventArgs e)
	    {
            var costHp = (from g in GamesFilter(false)
                          from u in g.Units
                          where u.Unit1 == victimsUnitKey.Value
                          select u).Average(u => (double?)u.Cost / u.Health);

            var ret = from g in GamesFilter(false)
                      from d in g.Damages
                      where d.AttackerUnit == victimsUnitKey.Value
                      group d by d.VictimUnit
                          into grp
                          let Damage = grp.Sum(x => (double?)x.Damage1)

                          let DamageReverse = GamesFilter(false).SelectMany(x => x.Damages).Where(x => x.VictimUnit == victimsUnitKey.Value && x.AttackerUnit == grp.Key).Sum(
                            x => (double?)x.Damage1)
                          let grpCostHp = (from g in GamesFilter(false) from u in g.Units where u.Unit1 == grp.Key select u).Average(u => (double?)u.Cost / u.Health)
                          let Ratio = (DamageReverse * costHp) != 0 ? (Damage * grpCostHp) / (DamageReverse * costHp) : null

                          orderby Ratio descending
                          select new { Name = grp.Key, Damage, Ratio, CostDamaged = Damage * grpCostHp };

            e.Result = ret;
	    }

        
        protected void LinqDataSourceKillers_Selecting(object sender, LinqDataSourceSelectEventArgs e)
	    {
            
            var costHp = (from g in GamesFilter(false)
                          from u in g.Units
                          where u.Unit1 == victimsUnitKey.Value
                          select u).Average(u => (double?)u.Cost / u.Health);

            var ret2 = from g in GamesFilter(false)
                       from d in g.Damages
                       where d.VictimUnit == victimsUnitKey.Value
                       group d by d.AttackerUnit
                           into grp

                           let Damage = grp.Sum(x => (double?)x.Damage1)
                           let DamageReverse = GamesFilter(false).SelectMany(x => x.Damages).Where(x => x.AttackerUnit == victimsUnitKey.Value && x.VictimUnit == grp.Key).Sum(
                           x => (double?)x.Damage1)
                           let grpCostHp = (from g in GamesFilter(false) from u in g.Units where u.Unit1 == grp.Key select u).Average(u => (double?)u.Cost / u.Health)
                           let Ratio = (DamageReverse * grpCostHp) != 0 ? (Damage * costHp) / (DamageReverse * grpCostHp) : null


                           orderby Ratio descending
                           select new { Name = grp.Key, Damage, Ratio };

            e.Result = ret2;

	    }
	}
}