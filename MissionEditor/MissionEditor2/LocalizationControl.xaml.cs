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
using System.Windows.Shapes;
using CMissionLib;
using CMissionLib.Actions;

namespace MissionEditor2
{
    /// <summary>
    /// Interaction logic for LocalizationControl.xaml
    /// </summary>
    public partial class LocalizationControl : UserControl
	{
        readonly SolidColorBrush NO_EDIT_COLOR = null;
        readonly int TEXTBOX_WIDTH = 240;
        readonly bool READ_ONLY = false;
        StackPanel stack;

		public LocalizationControl()
		{
			InitializeComponent();
            stack = (StackPanel)FindName("LocalizationStack");
            AddControls();
		}

        void AddBinding(TextBox box, ILocalizable source, string propertyName)
        {
            Binding binding = new Binding(propertyName);
            binding.Source = source;
            binding.Mode = BindingMode.TwoWay;
            box.SetBinding(TextBox.TextProperty, binding);
        }

        void AddControls()
        {
            var mission = MissionEditor2.MainWindow.Instance.Mission;
            foreach (CMissionLib.Action action in mission.AllLogic.Where(x => x is ILocalizable && x is CMissionLib.Action))
            {
                var groupBox = new GroupBox
                {
                    Header = action.ToString()
                };
                var grid = new Grid()
                {                
                };
                grid.RowDefinitions.Add(new RowDefinition());
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) });
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                
                TextBox id1 = new TextBox { Width = TEXTBOX_WIDTH };
                Grid.SetColumn(id1, 0);
                TextBox text1 = new TextBox { IsReadOnly = READ_ONLY, Background = NO_EDIT_COLOR };
                Grid.SetColumn(text1, 1);
                TextBox id2 = null;
                TextBox text2 = null;

                if (action is AddObjectiveAction)
                {
                    AddObjectiveAction obj = (AddObjectiveAction)action;
                    AddBinding(id1, obj, "TitleStringID");
                    AddBinding(text1, obj, "Title");
                    id2 = new TextBox { Width = TEXTBOX_WIDTH };
                    text2 = new TextBox {IsReadOnly = READ_ONLY, Background = NO_EDIT_COLOR };
                    AddBinding(id2, obj, "StringID");
                    AddBinding(text2, obj, "Description");
                }
                else if (action is ModifyObjectiveAction)
                {
                    ModifyObjectiveAction obj = (ModifyObjectiveAction)action;
                    AddBinding(id1, obj, "TitleStringID");
                    AddBinding(text1, obj, "Title");
                    id2 = new TextBox { Width = TEXTBOX_WIDTH };
                    text2 = new TextBox { IsReadOnly = READ_ONLY, Background = NO_EDIT_COLOR };
                    AddBinding(id2, obj, "StringID");
                    AddBinding(text2, obj, "Description");
                }
                else if (action is ConvoMessageAction)
                {
                    ConvoMessageAction msg = (ConvoMessageAction)action;
                    AddBinding(id1, msg, "StringID");
                    AddBinding(text1, msg, "Message");
                }
                else if (action is GuiMessageAction)
                {
                    GuiMessageAction msg = (GuiMessageAction)action;
                    AddBinding(id1, msg, "StringID");
                    AddBinding(text1, msg, "Message");
                }
                else if (action is GuiMessagePersistentAction)
                {
                    GuiMessagePersistentAction msg = (GuiMessagePersistentAction)action;
                    AddBinding(id1, msg, "StringID");
                    AddBinding(text1, msg, "Message");
                }
                else if (action is MarkerPointAction)
                {
                    MarkerPointAction msg = (MarkerPointAction)action;
                    AddBinding(id1, msg, "StringID");
                    AddBinding(text1, msg, "Text");
                }
                
                grid.Children.Add(text1);
                grid.Children.Add(id1);
                if (id2 != null && text2 != null)
                {
                    grid.RowDefinitions.Add(new RowDefinition());
                    Grid.SetColumn(text2, 1);
                    Grid.SetColumn(id2, 0);
                    Grid.SetRow(text2, 1);
                    Grid.SetRow(id2, 1);
                    grid.Children.Add(text2);
                    grid.Children.Add(id2);
                }

                groupBox.Content = grid;
                stack.Children.Add(groupBox);
            }
        }
	}
}
