using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using ZeroKLobby.MicroLobby;

namespace ZeroKLobby
{
	class TabItemHeaderSelector : DataTemplateSelector
	{
		public override DataTemplate SelectTemplate(object item, DependencyObject container)
		{
			if (DesignerProperties.GetIsInDesignMode(container) || item == null) return null;
			string templateKey = null;
			if (item is BattleChatControl) templateKey = "battleTabHeaderTemplate";
			else if (item is PrivateMessageControl) templateKey = "pmTabHeaderTemplate";
			else if (item is ChatControl)
			{
				var chatControl = (ChatControl)item;
				if (chatControl.GameInfo != null) templateKey = "gameTabHeaderTemplate";
				else templateKey = "channelTabHeaderTemplate";
			}
			if (templateKey == null) throw new Exception("No template selected for chat tab header");
			return (DataTemplate)NavigationControl.Instance.ChatTab.FindResource(templateKey);
		}
	}
}
