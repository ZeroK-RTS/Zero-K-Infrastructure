using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Floe.UI;
using ZeroKLobby.Lines;

namespace ZeroKLobby.MicroLobby
{
	/// <summary>
	/// Interaction logic for ChatControl2.xaml
	/// </summary>
	public partial class ChatControl2 : UserControl
	{
		public ChatControl2()
		{
			InitializeComponent();
		}

		public string ChannelName { get; set; }
		public GameInfo GameInfo { get; set; }
		public DateTime Date { get; set; }

		public virtual void AddLine(IChatLine line)
		{
			Date = DateTime.Now;
			if ((line is SaidLine && Program.Conf.IgnoredUsers.Contains(((SaidLine)line).AuthorName)) ||
				(line is SaidExLine && Program.Conf.IgnoredUsers.Contains(((SaidExLine)line).AuthorName))) return;



			HistoryManager.LogLine(ChannelName, line);
		}
	}
}
