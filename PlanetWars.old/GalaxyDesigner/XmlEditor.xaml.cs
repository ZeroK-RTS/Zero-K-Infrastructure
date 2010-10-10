using System;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;
using ICSharpCode.TextEditor;
using MessageBox=System.Windows.MessageBox;
using Point=System.Drawing.Point;
using UserControl=System.Windows.Controls.UserControl;

// bits and pieces taken from kaxaml

namespace GalaxyDesigner
{
    /// <summary>
    /// Interaction logic for XmlEditor.xaml
    /// </summary>
    public partial class XmlEditor : UserControl
    {
        readonly DispatcherTimer FoldingTimer;
        int findIndex;

        public string Text { get { return textEditor.Text; } set { textEditor.Text = value; } }

        public XmlEditor()
        {
            InitializeComponent();
            if (textEditor.Document.FoldingManager.FoldingStrategy == null) {
                textEditor.Document.FoldingManager.FoldingStrategy = new XmlFoldingStrategy();
            }

            if (FoldingTimer == null) {
                FoldingTimer = new DispatcherTimer {Interval = TimeSpan.FromMilliseconds(1000)};
                FoldingTimer.Tick += FoldingTimer_Tick;
            }

            textEditor.EnableFolding = true;
            FoldingTimer.Start();
            textEditor.SetHighlighting("XML");
        }


        public int CaretIndex
        {
            get { return textEditor.ActiveTextAreaControl.TextArea.Caret.Offset; }
            set { textEditor.ActiveTextAreaControl.Caret.Position = textEditor.Document.OffsetToPosition(value); }
        }

        public string SelectedText
        {
            get { return textEditor.ActiveTextAreaControl.SelectionManager.SelectedText; }
        }

        public IntPtr EditorHandle
        {
            get { return textEditor.Handle; }
        }

        public TextEditorControl HostedEditorControl
        {
            get { return textEditor; }
        }

        void FoldingTimer_Tick(object sender, EventArgs e)
        {
            textEditor.Document.FoldingManager.UpdateFoldings(String.Empty, null);
            var area = textEditor.ActiveTextAreaControl.TextArea;
            area.Refresh(area.FoldMargin);
        }

        public void InsertString(string s, int offset)
        {
            textEditor.ActiveTextAreaControl.TextArea.MotherTextEditorControl.BeginUpdate();

            textEditor.ActiveTextAreaControl.TextArea.Document.Insert(offset, s);

            textEditor.ActiveTextAreaControl.TextArea.MotherTextEditorControl.EndUpdate();
        }

        public void RemoveString(int beginIndex, int count)
        {
            textEditor.ActiveTextAreaControl.TextArea.MotherTextEditorControl.BeginUpdate();

            textEditor.ActiveTextAreaControl.TextArea.Document.Remove(beginIndex, count);

            if (CaretIndex > beginIndex) {
                textEditor.ActiveTextAreaControl.TextArea.Caret.Column -= count;
            }
        }

        public void ReplaceSelectedText(string s)
        {
            if (textEditor.ActiveTextAreaControl.SelectionManager.SelectionCollection[0] != null) {
                var offset = textEditor.ActiveTextAreaControl.SelectionManager.SelectionCollection[0].Offset;
                var length = textEditor.ActiveTextAreaControl.SelectionManager.SelectionCollection[0].Length;

                RemoveString(offset, length);
                InsertString(s, offset);

                SetSelection(offset, offset + s.Length, true);
            }
        }

        public void SetSelection(int fromOffset, int toOffset, bool suppressSelectionChangedEvent)
        {
            var from = textEditor.Document.OffsetToPosition(fromOffset);
            var to = textEditor.Document.OffsetToPosition(toOffset);
            textEditor.ActiveTextAreaControl.SelectionManager.SetSelection(from, to);
        }

        public void SelectLine(int lineNumber)
        {
                var startPoint = new Point(0, lineNumber);
                var endPoint = new Point(0, lineNumber + 1);

                textEditor.ActiveTextAreaControl.SelectionManager.SetSelection(startPoint, endPoint);
                textEditor.ActiveTextAreaControl.Caret.Position = new Point(0, lineNumber);
        }

        public void Find(string s)
        {
            findBox.Text = s;
            findIndex = CaretIndex;

            if (SelectedText.ToUpperInvariant() == findBox.Text.ToUpperInvariant()) {
                findIndex = CaretIndex + 1;
            }

            FindNext();
        }

        public void FindNext()
        {
            FindNext(true);
        }

        public void FindNext(bool allowStartFromTop)
        {
            if (String.IsNullOrEmpty(textEditor.Text)) {
                return;
            }
            var index = textEditor.Text.ToUpperInvariant().IndexOf(findBox.Text.ToUpperInvariant(), findIndex);
            if (index > 0) {
                SetSelection(index, index + findBox.Text.Length, false);
                findIndex = index + 1;
                CaretIndex = index;

                Focus();
                textEditor.Select();
            } else if (allowStartFromTop) {
                findIndex = 0;
                FindNext(false);
            } else {
                MessageBox.Show("The text \"" + findBox.Text + "\" could not be found.");
                findIndex = 0;
            }
        }

        public void Replace(string s, string replacement, bool selectedonly)
        {
            if (selectedonly) {
                var c = CaretIndex;
                var sub = SelectedText;
                var r = ReplaceEx(sub, s, replacement);

                ReplaceSelectedText(r);

                CaretIndex = c;
            } else {
                var c = CaretIndex;
                var r = ReplaceEx(textEditor.Text, s, replacement);
                textEditor.Text = r;
                CaretIndex = c;
            }
        }

        string ReplaceEx(string original, string pattern, string replacement)
        {
            var count = 0;
            var position0 = 0;
            var position1 = 0;

            var upperString = original.ToUpper();
            var upperPattern = pattern.ToUpper();

            var inc = (original.Length/pattern.Length)*(replacement.Length - pattern.Length);
            var chars = new char[original.Length + Math.Max(0, inc)];

            while ((position1 = upperString.IndexOf(upperPattern, position0)) != -1) {
                for (var i = position0; i < position1; ++i) {
                    chars[count++] = original[i];
                }
                for (var i = 0; i < replacement.Length; ++i) {
                    chars[count++] = replacement[i];
                }
                position0 = position1 + pattern.Length;
            }

            if (position0 == 0) {
                return original;
            }

            for (var i = position0; i < original.Length; ++i) {
                chars[count++] = original[i];
            }

            return new string(chars, 0, count);
        }

        private void findButton_Click(object sender, RoutedEventArgs e)
        {
            FindNext();
        }

        private void replaceButton_Click(object sender, RoutedEventArgs e)
        {
           FindNext();
           ReplaceSelectedText(replaceBox.Text);
        }

        private void replaceAll_Click(object sender, RoutedEventArgs e)
        {
            Replace(findBox.Text, replaceBox.Text, false);
        }
    }
}