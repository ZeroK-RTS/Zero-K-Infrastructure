using System;
using System.Collections.Generic;
using System.Runtime.Serialization;


namespace ServiceData.TradeTab
{
	[DataContract()]
	public class TradeData
	{
		[DataMember]
		public IEnumerable<TradeOrder> AllTradeOrders { get; set; }

		[DataMember]
		public IEnumerable<TradeOrder> MyTradeOrders { get; set; }

		[DataMember]
		public IEnumerable<Delivery> PendingDeliveries { get; set; }

		[DataMember]
		public IEnumerable<SellableItem> SellableItems { get; set; }

		[DataMember]
		public IEnumerable<CompletedTrade> TradeLog { get; set; }
	}

	/// <summary>
	/// Describes an item a player can potentially sell
	/// </summary>
	[DataContract()]
	public class SellableItem
	{
		[DataMember]
		public string MaxQuantity { get; set; }

		[DataMember]
		public string Name { get; set; }
	}

	[DataContract()]
	public enum OrderType
	{
		Buy,
		Sell,
	}

	[DataContract()]
	public enum OrderScope
	{
		Universe,
		StarSystem,
		Player,
	}

	[DataContract()]
	public class TradeOrder
	{
		[DataMember]
		public TimeSpan Eta { get; set; }

		[DataMember]
		public string ItemName { get; set; }

		[DataMember]
		public int OrderId { get; set; }

		[DataMember]
		public OrderType OrderType { get; set; }

		/// <summary>
		/// If the scope is a player, this is the name of the player for 
		/// which the offer is valid
		/// </summary>
		[DataMember]
		public string OtherParty { get; set; }

		[DataMember]
		public string Player { get; set; }

		[DataMember]
		public int Price { get; set; }

		[DataMember]
		public string Quantity { get; set; }

		[DataMember]
		public OrderScope Scope { get; set; }
	}

	[DataContract()]
	public class Delivery
	{
		[DataMember]
		public TimeSpan Eta { get; set; }

		[DataMember]
		public string ItemName { get; set; }

		[DataMember]
		public int Quantity { get; set; }

		[DataMember]
		public int Seller { get; set; }
	}

	[DataContract()]
	public class Trade
	{
		[DataMember]
		public DateTime Date { get; set; }

		[DataMember]
		public int Price { get; set; }

		[DataMember]
		public int Volume { get; set; }
	}

	/// <summary>
	/// History of all trades by all players involving an item
	/// </summary>
	[DataContract()]
	public class TradeHistory
	{
		[DataMember]
		public IEnumerable<Trade> History { get; set; }

		[DataMember]
		public string ItemName { get; set; }
	}

	[DataContract()]
	public class CompletedTrade
	{
		[DataMember]
		public DateTime Date { get; set; }

		[DataMember]
		public string ItemName { get; set; }

		[DataMember]
		public OrderType OrderType { get; set; }

		[DataMember]
		public string OtherPartyName { get; set; }

		[DataMember]
		public int Price { get; set; }

		[DataMember]
		public int Quantity { get; set; }
	}
}