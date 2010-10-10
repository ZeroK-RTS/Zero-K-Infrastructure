using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using LuaManagerLib;

namespace ZeroKLobby.LuaMgr
{
    public partial class LuaMgrTab: UserControl
    {
        protected const string descriptionPrequel =
            "<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Transitional//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\"><html xmlns=\"http://www.w3.org/1999/xhtml\"><head><meta http-equiv=\"Content-Type\" content=\"text/html; charset=ISO-8859-1\"><style type=\"text/css\"> body {background-color: #e5e5e5; font-family:Gisha;  font-size: 14px; } </style></head><body>";
        protected const string descriptionSequel = "</body></html>";

        protected int curDbListDisplayCount;
        protected int curHddListDisplayCount;
        protected WidgetInfo currentWidgetCurVersion;
        protected WidgetInfo currentWidgetLatest;
        WidgetHandler handler;
        String helpUrl = "http://widgetdb.springrts.de/help";
        protected string linuxHtmlTmpFile;
        protected string linuxHtmlTmpFileHelp;
        protected readonly int tabCtrlOrgHeightDiff;
        protected readonly AnchorStyles tabCtrlOriginalAnchor = AnchorStyles.None;
        protected readonly int tabCtrlOriginalWidth;
        Thread updaterThread;
        protected string widgetFilterString = "";

        public LuaMgrTab()
        {
        		var isDesignMode = Process.GetCurrentProcess().ProcessName == "devenv"; // workaround for this.DesignMode not working in constructor
            try
            {
                if (!isDesignMode) handler = new WidgetHandler();
                InitializeComponent();
                comboBoxSortDb.SelectedItem = "Name";
                comboBoxSortingHdd.SelectedItem = "Name";
                panelWidget.Visible = false;

                setupWidgetListView();

                tabCtrlOriginalWidth = tabCtrlWidgetCategory.Size.Width;
                tabCtrlOrgHeightDiff = panelWidget.Size.Height - tabCtrlWidgetCategory.Size.Height;
                tabCtrlOriginalAnchor = tabCtrlWidgetCategory.Anchor;

                setTooltips();

                //startRefreshListsThread(3000);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error initializing widgets tab: {0}", ex);
            }
        }

        ~LuaMgrTab()
        {
            try
            {
                if (linuxHtmlTmpFile != null) File.Delete(linuxHtmlTmpFile);
            }
            catch {}

            try
            {
                if (linuxHtmlTmpFile != null) File.Delete(linuxHtmlTmpFileHelp);
            }
            catch {}
        }

        public static bool checkLobbyInputData()
        {
            return checkLobbyInputData(true);
        }

        public static bool checkLobbyInputData(bool showNotifyBox)
        {
            if (Program.Conf.LobbyPlayerPassword == null || Program.Conf.LobbyPlayerPassword.Length == 0 || Program.Conf.LobbyPlayerName.Length == 0 ||
                Program.Conf.LobbyPlayerName == null)
            {
                if (showNotifyBox) MessageBox.Show("Please enter your spring lobby account data in the Advanced Options menu to use this function.");
                return false;
            }
            return true;
        }

        protected override void OnLoad(EventArgs e)
        {
            buttonRefresh_Click(null, null);
            //dunno if this whole function is still needed at all
            if (updaterThread != null && !updaterThread.IsAlive)
            {
                labelUpdating.Visible = false;
                Enabled = true;

                refreshCategories();
                refreshActivationModCombo();

                displayDbList();
                showWidgetDb(0);
								webBrowserHelp.Url = new Uri(helpUrl);
            }
        }

        void displayDbList()
        {
            listViewDbWidgets.BeginUpdate();

            //save the list of currently selected items
            var selectedItems = new List<string>();
            foreach (ListViewItem sel in listViewDbWidgets.SelectedItems) selectedItems.Add(sel.Text);

            //now clear the list and rebuild from scratch
            listViewDbWidgets.Items.Clear();

            var ienum = handler.widgetsInstall.Values.GetEnumerator();

            var shownWidgetCount = 0;
            while (ienum.MoveNext())
            {
                var info = (WidgetInfo)ienum.Current;

                var widgetName = info.name;
                var item = new ListViewItem(widgetName);

                //check if item was selected
                if (selectedItems.Contains(widgetName)) item.Selected = true;

                if ((widgetFilterString.Length > 0) && (!widgetName.ToLower().Contains(widgetFilterString.ToLower())))
                {
                    //filter is activated but string not found, skip it
                    continue;
                }

                if ((info.hidden && !checkBoxHiddenWidgets.Checked) || (!info.hidden && checkBoxHiddenWidgets.Checked))
                {
                    //skip hidden widgets if checkbox isnt checked
                    continue;
                }

                var selCat = comboBoxGroupByDb.SelectedItem as Category;
                if ((selCat != null) && (selCat.id != -1) && (selCat.id != info.CategoryId)) continue;

                shownWidgetCount++;

                var imageId = 0;
                switch (info.state)
                {
                    case WidgetState.INSTALLED:
                    {
                        //this widget is installed
                        imageId = 2;
                        item.ToolTipText = "Installed";
                    }
                        break;
                    case WidgetState.OUTDATED:
                    {
                        imageId = 1;
                        item.ToolTipText = "Update available";
                    }
                        break;
                    case WidgetState.UNKNOWN_VERSION:
                    {
                        imageId = 3;
                        item.ToolTipText = "Unknown version";
                    }
                        break;
                    case WidgetState.NOT_INSTALLED:
                    {
                        imageId = 0;
                        item.ToolTipText = "Not Installed";
                    }
                        break;
                    case WidgetState.INSTALLED_LOCALLY:
                    {
                        imageId = 0;
                        item.ToolTipText = "Local-only-widget";
                    }
                        break;
                }

                var tnd = new ListViewTagData();
                tnd.id = info.id;
                var widgetSortString = (string)comboBoxSortDb.SelectedItem;
                if (widgetSortString == "Name" || widgetSortString.Length == 0) tnd.sorting = widgetName;
                else if (widgetSortString == "Date") tnd.sorting = info.entry;
                else if (widgetSortString == "Downloads") tnd.sorting = info.downloadCount;
                else if (widgetSortString == "State") tnd.sorting = info.state;
                else if (widgetSortString == "Popularity") tnd.sorting = info.downsPerDay;
                else if (widgetSortString == "Rating") tnd.sorting = info.rating + (info.voteCount/10000.0);
                else if (widgetSortString == "Author") tnd.sorting = info.author;
                item.Tag = tnd;
                item.ImageIndex = imageId;

                item.ImageIndex = imageId;

                listViewDbWidgets.Items.Add(item);
            }
            listViewDbWidgets.ListViewItemSorter = new ListViewTagSorter();

            //scroll to selected item
            if (listViewDbWidgets.SelectedItems.Count > 0) listViewDbWidgets.EnsureVisible(listViewDbWidgets.SelectedItems[0].Index);

            //update widget count display
            curDbListDisplayCount = shownWidgetCount;
            updateWidgetCountDisplay();
            listViewDbWidgets.EndUpdate();
        }

        void displayHddList()
        {
            listViewHddWidgets.BeginUpdate();

            //save the list of currently selected items
            var selectedItems = new List<string>();
            foreach (ListViewItem sel in listViewHddWidgets.SelectedItems) selectedItems.Add(sel.Text);

            listViewHddWidgets.Clear();

            var category = (string)comboBoxActCategory.SelectedItem;
            if (category == "None")
            {
                //there is no mod to activate widgets for
                return;
            }

            var ienum = handler.widgetsActivate.Values.GetEnumerator();

            var shownWidgetCount = 0;
            while (ienum.MoveNext())
            {
                var info = (WidgetInfo)ienum.Current;

                if ((info.modName != null) && (category != info.modName)) continue;

                var widgetName = info.name;
                if (!info.dbIsAvail) widgetName = info.headerName;

                var item = new ListViewItem(widgetName);

                if (selectedItems.Contains(widgetName)) item.Selected = true;

                if (!info.dbIsAvail && info.headerIsAvail)
                {
                    //its a locally installed widget
                    item.ForeColor = Color.FromArgb(110, 110, 110);
                }

                if ((widgetFilterString.Length > 0) && (!widgetName.ToLower().Contains(widgetFilterString.ToLower())))
                {
                    //filter is activated but string not found, skip it
                    continue;
                }

                shownWidgetCount++;

                var imageId = 0;
                if (info.activatedState[category])
                {
                    imageId = 2;
                    item.ToolTipText = "Activated";
                }
                else
                {
                    imageId = 0;
                    item.ToolTipText = "Deactivated";
                }

                var widgetSortString = (string)comboBoxSortingHdd.SelectedItem;
                var tagData = new ListViewTagData();
                tagData.id = info.id;

                if (widgetSortString == "Name" || widgetSortString.Length == 0) tagData.sorting = widgetName;
                else if (widgetSortString == "State") tagData.sorting = info.activatedState[category];
                item.Tag = tagData;
                item.ImageIndex = imageId;
                listViewHddWidgets.Items.Add(item);
            }
            listViewHddWidgets.ListViewItemSorter = new ListViewTagSorter();

            //scroll to selected item
            if (listViewHddWidgets.SelectedItems.Count > 0) listViewHddWidgets.EnsureVisible(listViewHddWidgets.SelectedItems[0].Index);

            curHddListDisplayCount = shownWidgetCount;
            updateWidgetCountDisplay();
            listViewHddWidgets.EndUpdate();
        }

        void exportActivationPreset()
        {
            using (var fa = new SaveFileDialog()) {
                fa.AddExtension = true;
                fa.Filter = "Widget Activation Preset (*.wap)|*.wap";
                fa.Title = "Export Widget Activation Preset";

                if (fa.ShowDialog() == DialogResult.OK) handler.exportActivationPreset(fa.FileName);
            }
        }

        void exportCurrentWidgetList()
        {
            using (var fa = new SaveFileDialog()) {
                fa.AddExtension = true;
                fa.Filter = "Widget Index List (*.xml)|*.xml";
                fa.Title = "Export Widget Index List";

                if (fa.ShowDialog() == DialogResult.OK) handler.exportWidgetList(fa.FileName);
            }
        }

        int getSelectedDbLuaId()
        {
            Debug.Assert(listViewDbWidgets.SelectedItems.Count > 0);
            return (listViewDbWidgets.SelectedItems[0].Tag as ListViewTagData).id;
        }

        List<int> getSelectedDbLuaIds()
        {
            var ids = new List<int>();

            foreach (ListViewItem i in listViewDbWidgets.SelectedItems) ids.Add((listViewDbWidgets.SelectedItems[0].Tag as ListViewTagData).id);
            return ids;
        }

        int getSelectedHddLuaId()
        {
            Debug.Assert(listViewHddWidgets.SelectedItems.Count > 0);
            return (listViewHddWidgets.SelectedItems[0].Tag as ListViewTagData).id;
        }

        List<int> getSelectedHddLuaIds()
        {
            var ids = new List<int>();

            foreach (ListViewItem item in listViewHddWidgets.SelectedItems)
            {
                //Console.Write((String)selectedItem.Tag);
                ids.Add((item.Tag as ListViewTagData).id);
            }
            return ids;
        }


        /*
         * removes a widget 
         */

        ImageList getWidgetIconStateImages()
        {
            var imageListSmall = new ImageList();
            try
            {
                imageListSmall.ColorDepth = ColorDepth.Depth32Bit;
                imageListSmall.Images.Add(LuaMgrResources.red); //(Image)rm.GetObject("red"));
                imageListSmall.Images.Add(LuaMgrResources.yellow);
                imageListSmall.Images.Add(LuaMgrResources.green);
                imageListSmall.Images.Add(LuaMgrResources.blue);
            } catch
            {
                imageListSmall.Dispose();
                throw;
            }
            return imageListSmall;
        }

        void importWidgetList()
        {
            using (var fa = new OpenFileDialog()) {
                fa.Filter = "Widget Index List (*.xml)|*.xml";
                fa.Title = "Import Widget Index List";

                if (fa.ShowDialog() == DialogResult.OK)
                {
                    if (fa.FileName.Length <= 0) return;

                    var widgets = handler.loadWidgetListFromXmlFile(fa.FileName);
                    var dlg = new PresetInstaller(widgets);

                    if (dlg.ShowDialog() == DialogResult.OK) handler.installWidgetList(widgets);
                }
            }
        }

        void loadActivationPreset()
        {
            var fa = new OpenFileDialog();
            fa.Filter = "Widget Activation Preset (*.wap)|*.wap";
            fa.Title = "Load Widget Activation Preset";

            if (fa.ShowDialog() == DialogResult.OK)
            {
                if (fa.FileName.Length <= 0) return;

                if (
                    MessageBox.Show("You want to load activation preset from \"" + fa.FileName + "\"?",
                                    "Confirmation",
                                    MessageBoxButtons.YesNo,
                                    MessageBoxIcon.Question) == DialogResult.Yes) handler.loadActivationPreset(fa.FileName);
            }
        }

        void refreshActivationModCombo()
        {
            comboBoxActCategory.Items.Clear();

            foreach (var elem in handler.mods.Values)
            {
                var abb = elem.abbreviation;
                if (abb.Length > 0) comboBoxActCategory.Items.Add(abb);
            }

            if (comboBoxActCategory.Items.Count == 0) comboBoxActCategory.Items.Add("None");

            comboBoxActCategory.SelectedIndex = 0;
        }

        void refreshCategories()
        {
            comboBoxGroupByDb.Items.Clear();

            var catAll = new Category(" All", -1);
            comboBoxGroupByDb.Items.Add(catAll);
            comboBoxGroupByDb.Items.Add(new Category(" None", 0));

            foreach (var cat in handler.categories.Values) comboBoxGroupByDb.Items.Add(cat);

            comboBoxGroupByDb.SelectedItem = catAll;
        }

        void refreshDbList()
        {
            handler.refreshDbWidgetList();

            //autoupdate/autoinstall
            var ienum = handler.widgetsInstall.Values.GetEnumerator();
            while (ienum.MoveNext())
            {
                var info = (WidgetInfo)ienum.Current;

                if ((info.state == WidgetState.OUTDATED) && (Program.Conf.AutoUpdateWidgets))
                {
                    Trace.TraceInformation("Updating widget: " + info.name + " to version: " + info.version);
                    handler.updateWidget(info);
                    info.state = WidgetState.INSTALLED;
                }
                else if ((info.state == WidgetState.NOT_INSTALLED) && (Program.Conf.AutoInstallWidgets))
                {
                    Trace.TraceInformation("Installing widget: " + info.name + " in version: " + info.version);
                    handler.installLua(info.id, true);
                    info.state = WidgetState.INSTALLED;
                }
            }
        }

        void refreshHddList()
        {
            handler.refreshHddWidgetList();
        }

        /* private ListViewItem GetDbListItemByName(string name)
        {
            ListViewItem target = null;
            foreach (ListViewItem i in listViewDbWidgets.Items)
            {
                if (i.Text == name)
                {
                    target = i;
                }
            }

            return target;
        }*/

        void removeDeletedItemsFromDbList()
        {
            var removeList = new List<ListViewItem>();

            //find all items that do not exist anymore
            foreach (ListViewItem i in listViewDbWidgets.Items) if (handler.widgetsInstall.getByName(i.Text) == null) removeList.Add(i);

            //now actually remove them
            foreach (var i in removeList) listViewDbWidgets.Items.Remove(i);
        }

        void removeWidgetFromInstalled(int luaId)
        {
            handler.removeLua(luaId);

            var listedWidget = handler.getWidgetFromDbListRelatedToHddList(luaId);
            displayHddList();
            displayDbList();
            showWidgetDb(listedWidget.id);
        }

        void setTooltips()
        {
            toolTip1.SetToolTip(pictureBoxThumb, "Show image gallery");
            toolTip1.SetToolTip(buttonChangelog, "Show Changelog");
            toolTip1.SetToolTip(buttonShowCurVerFiles, "Show all files in your current version");

            toolTip1.SetToolTip(labelSupportedGames, "Games supported by this widget");
            toolTip1.SetToolTip(buttonShowFiles, "Show all files belonging to this widget");
            toolTip1.SetToolTip(button_update, "Update to latest version");
            toolTip1.SetToolTip(button_update_options, "Advanced update options");
            toolTip1.SetToolTip(button_zipDownload, "Download latest version or current version as ZIP archive");

            toolTip1.SetToolTip(execButton, "Un-/Install this widget");
            toolTip1.SetToolTip(buttonRefresh, "Refresh complete widget list");
            toolTip1.SetToolTip(labelFilter, "Keywords to filter list");
            toolTip1.SetToolTip(textBoxFilter, "Keywords to filter list");
        }

        void setupWidgetListView()
        {
            // Initialize the ImageList objects with bitmaps.
            var imageListSmall = getWidgetIconStateImages();

            //Assign the ImageList objects to the ListView.
            listViewDbWidgets.SmallImageList = imageListSmall;
            listViewHddWidgets.SmallImageList = imageListSmall;
            buttonActivateWidget.ImageList = imageListSmall;
        }

        void showWidgetDb(int luaId)
        {
            showWidgetEx(luaId, true);
        }

        /*
         * Call with luaId == 0 to not show any widget -> make the panel invisible
         */

        void showWidgetEx(int luaId, bool db)
        {
            if (luaId == 0)
            {
                panelWidget.Visible = false;
                return;
            }

            panelWidget.Visible = true;
            if (db)
            {
                currentWidgetLatest = (WidgetInfo)handler.widgetsInstall[luaId];
                currentWidgetCurVersion = handler.widgetsActivate.getOlderVersion(currentWidgetLatest);
                panelButtonsDb.Visible = true;
                panelButtonsHdd.Visible = false;
            }
            else
            {
                currentWidgetCurVersion = (WidgetInfo)handler.widgetsActivate[luaId];
                currentWidgetLatest = currentWidgetCurVersion;
                if (currentWidgetCurVersion.dbIsAvail) currentWidgetLatest = handler.widgetsInstall.getByNameId(currentWidgetCurVersion.nameId);

                //this.currentWidgetLatest = (WidgetInfo)handler.widgetsActivate[luaId];
                //this.currentWidgetCurVersion = null;
                panelButtonsDb.Visible = false;
                panelButtonsHdd.Visible = true;
                var modShort = (string)comboBoxActCategory.SelectedItem;
                if (currentWidgetCurVersion.activatedState[modShort])
                {
                    buttonActivateWidget.Text = "Deactivate";
                    buttonActivateWidget.ImageIndex = 2;
                }
                else
                {
                    buttonActivateWidget.Text = "Activate";
                    buttonActivateWidget.ImageIndex = 0;
                }
            }

            var description = "";
            if (currentWidgetLatest.dbIsAvail)
            {
                panelInfoDb.Visible = true;

                labelAuthorTitle.Visible = true;
                label1.Text = currentWidgetLatest.name;
                buttonChangelog.Text = currentWidgetLatest.version.ToString(CultureInfo.CreateSpecificCulture("en-US").NumberFormat);
                labelAuthor.Text = currentWidgetLatest.author;
                labelDownloadCount.Text = currentWidgetLatest.downloadCount + " (" +
                                          String.Format(CultureInfo.CreateSpecificCulture("en-US").NumberFormat,
                                                        "{0:0.##}",
                                                        currentWidgetLatest.downsPerDay) + " D/d)";
                labelEntryDate.Text = currentWidgetLatest.entry.ToShortDateString(); // ToString() + " UTC+1";
                labelMods.Text = currentWidgetLatest.supportedMods;
                buttonShowCurVerFiles.ForeColor = Color.Black;

                var ratTooltip = "based on " + currentWidgetLatest.voteCount + " vote(s)";
                ratTooltip += Environment.NewLine + "(left-click to give a rating)";
                toolTip1.SetToolTip(ratingBarWidget, ratTooltip);
                ratingBarWidget.setToolTipForStars(toolTip1, ratTooltip);
                //labelRatingVoteCount.Text = ratTooltip;

                ratingBarWidget.Rate = currentWidgetLatest.rating;
                buttonComments.Text = "Comments (" + currentWidgetLatest.commentCount + ")";

                button_zipDownload.Visible = true;
                description = currentWidgetLatest.description;

                switch (currentWidgetLatest.state)
                {
                    case WidgetState.OUTDATED:
                    {
                        button_update.Enabled = true;
                        // labelYourVersion.Visible = true;
                        labelYourVersion.Visible = true;
                        buttonShowCurVerFiles.Visible = true;
                        buttonShowCurVerFiles.Text = currentWidgetCurVersion.version.ToString("G29", CultureInfo.InvariantCulture);
                        //overwrite with correct version
                        buttonShowCurVerFiles.ForeColor = Color.Red;

                        execButton.Text = "Uninstall";
                        button_update_options.Enabled = true;
                        execButton.Tag = 0;
                        break;
                    }
                    case WidgetState.INSTALLED:
                    {
                        button_update.Enabled = false;
                        button_update_options.Enabled = true;
                        execButton.Text = "Uninstall";
                        execButton.Tag = 0;
                        buttonShowCurVerFiles.Visible = false;
                        labelYourVersion.Visible = false;
                        break;
                    }
                    case WidgetState.UNKNOWN_VERSION:
                    case WidgetState.NOT_INSTALLED:
                    {
                        //buttonShowCurVerFiles.Visible = false;
                        //buttonShowCurVerFiles.Text = "N/A";
                        button_update.Enabled = false;
                        execButton.Text = "Install";
                        execButton.Tag = 1;
                        button_update_options.Enabled = false;
                        buttonShowCurVerFiles.Visible = false;
                        labelYourVersion.Visible = false;
                        break;
                    }
                }
            }
            else
            {
                panelInfoDb.Visible = false;
                label1.Text = currentWidgetLatest.headerName;
                description = currentWidgetLatest.headerDescription;
                if (currentWidgetLatest.headerIsAvail)
                {
                    labelAuthorTitle.Visible = true;
                    labelAuthor.Text = currentWidgetLatest.headerAuthor;
                }
                else
                {
                    labelAuthor.Text = "";
                    labelAuthorTitle.Visible = false;
                }
            }

            labelGalleryInfo.Text = "( " + currentWidgetLatest.imageCount + " Images )";

            pictureBoxThumb.Visible = true;
            pictureBoxThumb.LoadCompleted += PictureBox_LoadCompleted;
            ;
            pictureBoxThumb.ImageLocation = WidgetHandler.fetcher.getThumbnailUrl(currentWidgetLatest.nameId);

						pictureBoxThumb.LoadAsync();

            /* trying to emulate loadasync for linux here. but its as slow as directly load()
            new Thread(() => {
            try
            {
                Invoke(new Func(() =>
                {
                    pictureBoxThumb.Load();
                }
                ));
            }
           catch (Exception)
            {
                pictureBoxThumb.Image = LuaMgrResources.SpringThumbGlass;            
            }
            }).Start();
             */

            var htmlData = descriptionPrequel + description + descriptionSequel;
            webBrowserDescription.AllowNavigation = true;
            webBrowserDescription.DocumentText = htmlData;
        }

        void showWidgetHdd(int luaId)
        {
            showWidgetEx(luaId, false);
        }

        void startRefreshListsThread(int secOffset)
        {
            if (updaterThread == null || !updaterThread.IsAlive)
            {
                Trace.TraceInformation("Updating widget information");
                Enabled = false; //.Clear();
                labelUpdating.Visible = true;
            		labelUpdating.BringToFront();

                updaterThread = new Thread(() =>
                    {
                        Name = "Widget Update Thread";

                        try
                        {
                            long timeSummary = 0;
                            var watch = new Stopwatch();

                            watch.Start();
                            handler.refreshModList();
                            handler.refreshCategories();
                            watch.Stop();

                            timeSummary += watch.ElapsedMilliseconds;
                            Trace.TraceInformation("Widgets: Game List updated done in " + watch.ElapsedMilliseconds + " ms (1/3)");

                            watch.Reset();
                            watch.Start();
                            refreshDbList();
                            watch.Stop();
                            timeSummary += watch.ElapsedMilliseconds;
                            Trace.TraceInformation("Widgets: Database Widgets checked done in " + watch.ElapsedMilliseconds + " ms (2/3)");

                            watch.Reset();
                            watch.Start();
                            refreshHddList();
                            watch.Stop();
                            timeSummary += watch.ElapsedMilliseconds;
                            Trace.TraceInformation("Widgets: Local Widgets checked done in " + watch.ElapsedMilliseconds +
                                                   " ms (3/3) - Widgets Completed in " + timeSummary + " ms");

                            if (IsHandleCreated)
                            {
                                MethodInvoker invoker = new MethodInvoker(delegate()
                                {
                                    refreshCategories();
                                    refreshActivationModCombo();

                                    displayDbList();
                                    showWidgetDb(0);
                                });
                                Invoke(invoker);
                            }
                        }
                        catch (Exception exp)
                        {
                            Trace.TraceError("Error updating widget list: {0}", exp);
                        }
                        finally
                        {
                            if (IsHandleCreated)
                            {
                                MethodInvoker invoker = new MethodInvoker(delegate()
                                {
                                    //                            this.buttonRefresh.Enabled = true;
                                    labelUpdating.Visible = false;
                                    Enabled = true;
                                });
                                Invoke(invoker);
                            }
                        }
                    });

                updaterThread.Start();
            }
            else Trace.TraceWarning("Can't start widget update thread. It's still running!");
        }

        void toogleHddListActivateImage(bool activate)
        {
            var imageIdx = 2;
            if (!activate) imageIdx = 0;

            var ienum = listViewHddWidgets.SelectedItems.GetEnumerator();
            while (ienum.MoveNext())
            {
                var selectedItem = (ListViewItem)ienum.Current;

                selectedItem.ImageIndex = imageIdx;
            }
            buttonActivateWidget.ImageIndex = imageIdx;
        }

        void toogleHddListActivateImage(String widgetName, bool activate)
        {
            var imageIdx = 2;
            if (!activate) imageIdx = 0;

            var ienum = listViewHddWidgets.Items.GetEnumerator();
            while (ienum.MoveNext())
            {
                var item = (ListViewItem)ienum.Current;

                if (item.Text == widgetName) item.ImageIndex = imageIdx;
            }
        }


        void updateWidgetCountDisplay()
        {
            var curCount = curDbListDisplayCount;
            var curMax = handler.widgetsInstall.Values.Count;
            if (tabCtrlWidgetCategory.SelectedIndex == 1)
            {
                curCount = curHddListDisplayCount;
                curMax = handler.widgetsActivate.Values.Count;
            }

            labelWidgetCount.Text = curCount + "/" + curMax;
        }

        void activateWidgetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var ids = getSelectedHddLuaIds();
            var modShort = (string)comboBoxActCategory.SelectedItem;

            try
            {
                for (var i = 0; i < ids.Count; i++) handler.activateDeactivateWidgetsEx(ids, true, modShort);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Activation failed!\nError: " + ex.Message, "Error");
                return;
            }

            toogleHddListActivateImage(true);
        }

        void button_update_Click_1(object sender, EventArgs e)
        {
            try
            {
                handler.updateWidget(currentWidgetLatest);

                displayHddList();
                displayDbList();
                showWidgetDb(currentWidgetLatest.id);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Update failed! Error: " + ex.Message, "Error");
                return;
            }

            MessageBox.Show("Update complete!", "Success");
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        void button_update_options_Click(object sender, EventArgs e)
        {
            try
            {
                var c = contextMenuStripUpdateOptions.Items[0] as ToolStripMenuItem; //mind the hardcoded index!
                c.DropDown.Items.Clear();

                var versionLuas = WidgetHandler.fetcher.getLuasByNameId(currentWidgetLatest.nameId);
                var sorted = versionLuas.getAsSortedByVersion();

                foreach (WidgetInfo info in sorted)
                {
                    if (info.active == 0) continue;

                    var newItem = new ToolStripMenuItem(info.version.ToString("G29", new CultureInfo("en-US")));
                    //dont dare to display the dot as comma!
                    newItem.Click += contextMenu_updateToRevision;

                    if (info.version == currentWidgetCurVersion.version)
                    {
                        //disable currently installed version
                        newItem.Enabled = false;
                    }
                    newItem.Tag = info; //glue the widget here, so the event handle knows which version was chosen

                    //add the new item
                    c.DropDown.Items.Add(newItem);
                }

                contextMenuStripUpdateOptions.Show(button_update_options, 0, 0); //Control.MousePosition.X, Control.MousePosition.Y );
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error");
            }
        }

        void button_zipDownload_Click(object sender, EventArgs e)
        {
            try
            {
                var zipWidget = currentWidgetCurVersion;
                if (zipWidget == null) zipWidget = currentWidgetLatest;

                using (var fa = new SaveFileDialog()) {
                    fa.AddExtension = true;
                    fa.Filter = "ZIP Archive (*.zip)|*.zip";
                    fa.Title = "Download ZIP Package";
                    fa.FileName = zipWidget.name + "_" + zipWidget.version.ToString("G29", CultureInfo.InvariantCulture) + ".zip";

                    if (fa.ShowDialog() == DialogResult.OK) handler.zipDownloadWidget(zipWidget.id, fa.FileName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("ZIP Download failed! Error: " + ex.Message, "Error");
                return;
            }
        }

        void buttonActivateWidget_Click(object sender, EventArgs e)
        {
            var newState = true;
            var modShort = (string)comboBoxActCategory.SelectedItem;
            if (buttonActivateWidget.ImageIndex == 2) newState = false;
            else if (buttonActivateWidget.ImageIndex == 0) newState = true;

            handler.activateDeactivateWidget(currentWidgetCurVersion.id, newState, modShort);
            toogleHddListActivateImage(newState);

            showWidgetHdd(currentWidgetCurVersion.id);
        }

        void buttonChangelog_Click_1(object sender, EventArgs e)
        {
            using (var cl = new Changelog(currentWidgetLatest.nameId)) {
                cl.ShowDialog();
            }
        }

        void buttonClearFilter_Click(object sender, EventArgs e)
        {
            textBoxFilter.Text = "";
        }

        void buttonComments_Click(object sender, EventArgs e)
        {
            try
            {
                using (var cwnd = new CommentWindow(currentWidgetLatest.name, currentWidgetLatest.nameId)) {
                    cwnd.ShowDialog();

                    if (cwnd.DialogResult == DialogResult.OK)
                    {
                        var info = WidgetHandler.fetcher.getLuaById(currentWidgetLatest.id);
                        currentWidgetLatest.commentCount = info.commentCount;

                        if (currentWidgetCurVersion != null) currentWidgetCurVersion.commentCount = info.commentCount;

                        showWidgetDb(currentWidgetLatest.id);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error occured: " + ex.Message);
            }
        }

        void buttonRefresh_Click(object sender, EventArgs e)
        {
            startRefreshListsThread(0);
        }

        void buttonShowCurVerFiles_Click_1(object sender, EventArgs e)
        {
            try
            {
                var list = WidgetHandler.fetcher.getFileListByLuaId(currentWidgetCurVersion.id);

                var disp = new FilesDisplay(list);
                disp.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        void buttonShowFiles_Click_1(object sender, EventArgs e)
        {
            try
            {
                var list = WidgetHandler.fetcher.getFileListByLuaId(currentWidgetLatest.id);

                using (var disp = new FilesDisplay(list)) {
                    disp.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        void checkBoxHiddenWidgets_CheckedChanged(object sender, EventArgs e)
        {
            displayDbList();
        }

        void comboBoxActCategory_SelectedIndexChanged(object sender, EventArgs e)
        {
            displayHddList();
            showWidgetHdd(0);
        }

        void comboBoxGroupByDb_SelectedIndexChanged(object sender, EventArgs e)
        {
            displayDbList();
        }

        void comboBoxSortDb_SelectedIndexChanged(object sender, EventArgs e)
        {
            displayDbList();
            //this.showWidgetDb(0);
        }

        void comboBoxSortingHdd_SelectedIndexChanged(object sender, EventArgs e)
        {
            displayHddList();
            //            this.showWidgetHdd(0);
        }

        void contextMenu_updateToRevision(object sender, EventArgs e)
        {
            try
            {
                var si = sender as ToolStripDropDownItem;
                var wi = si.Tag as WidgetInfo;

                handler.updateWidget(wi);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Update failed! Error: " + ex.Message, "Error");
                return;
            }
            finally
            {
                showWidgetDb(currentWidgetLatest.id);
                displayHddList();
                displayDbList();
            }

            MessageBox.Show("Update complete!", "Success");
        }

        void contextMenuStripHddWidgetList_Opening(object sender, CancelEventArgs e)
        {
            try
            {
                var ids = getSelectedHddLuaIds();
                var modShort = (string)comboBoxActCategory.SelectedItem;

                var allActive = true;
                var allNonActive = true;
                for (var i = 0; i < ids.Count; i++)
                {
                    var w = (WidgetInfo)handler.widgetsActivate[ids[i]];
                    if (w.activatedState[modShort]) allNonActive = false;
                    else allActive = false;
                }

                activateWidgetToolStripMenuItem.Enabled = false;
                deactivateSelectedToolStripMenuItem.Enabled = false;
                if (allActive) deactivateSelectedToolStripMenuItem.Enabled = true;
                else if (allNonActive) activateWidgetToolStripMenuItem.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error");
            }
        }

        void contextMenuStripWidgetList_Opening(object sender, CancelEventArgs e)
        {
            var ids = getSelectedDbLuaIds();

            var installed = true;
            var notInstalled = true;
            foreach (var id in ids)
            {
                var w = (WidgetInfo)handler.widgetsInstall[id];
                if (w.state != WidgetState.INSTALLED) installed = false;
                else if (w.state != WidgetState.NOT_INSTALLED) notInstalled = false;
            }

            installAllSelectedToolStripMenuItem.Enabled = false;
            removeSelectedToolStripMenuItem.Enabled = false;
            if (installed) removeSelectedToolStripMenuItem.Enabled = true;
            else if (notInstalled) installAllSelectedToolStripMenuItem.Enabled = true;
        }

        void deactivateSelectedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var ids = getSelectedHddLuaIds();
            var modShort = (string)comboBoxActCategory.SelectedItem;

            try
            {
                handler.activateDeactivateWidgetsEx(ids, false, modShort);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Deactivation failed!\nError: " + ex.Message, "Error");
                return;
            }

            toogleHddListActivateImage(false);
            //this.displayHddList();
            //probably quite annoying, so no msgbox
            //MessageBox.Show("Deactivation complete! " + ids.Count + " widgets deactivated!", "Success");
        }

        void execButton_Click_1(object sender, EventArgs e)
        {
            try
            {
                switch ((int)execButton.Tag)
                {
                    case 0:
                    {
                        try
                        {
                            var targetId = currentWidgetLatest.id;
                            if (currentWidgetLatest.state == WidgetState.OUTDATED) targetId = currentWidgetCurVersion.id;

                            handler.uninstallWidget(targetId);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Uninstall failed! Error: " + ex.Message, "Error");
                            return;
                        }

                        MessageBox.Show("Uninstall complete!", "Success");
                    }
                        break;
                    case 1:
                    {
                        try
                        {
                            handler.installLua(currentWidgetLatest.id, true);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Installation failed!\nError: " + ex.Message, "Error");
                            return;
                        }

                        MessageBox.Show("Installation complete!", "Success");
                    }
                        break;
                }

                displayDbList();
                displayHddList();
                showWidgetDb(currentWidgetLatest.id);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error");
            }
        }

        void exportWidgetListToolStripMenuItem_Click(object sender, EventArgs e)
        {
            exportCurrentWidgetList();
        }

        void installAllSelectedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var ids = getSelectedDbLuaIds();

            try
            {
                foreach (var id in ids) handler.installLua(id, true);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Installation failed!\nError: " + ex.Message, "Error");
                return;
            }

            displayDbList();
            displayHddList();
            showWidgetDb(0);
            MessageBox.Show("Installation complete! " + ids.Count + " widgets added!", "Success");
        }

        void installWidgetListToolStripMenuItem_Click(object sender, EventArgs e)
        {
            importWidgetList();

            displayDbList();
            showWidgetDb(0);
        }

        void listViewDbWidgets_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listViewDbWidgets.SelectedItems.Count != 1) return;

            try
            {
                showWidgetDb(getSelectedDbLuaId());
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        void listViewHddWidgets_DoubleClick(object sender, EventArgs e)
        {
            var imageIdx = 2;
            var lv = (ListView)sender;
            var item = lv.FocusedItem;
            var modShort = (string)comboBoxActCategory.SelectedItem;

            try
            {
                var widget = (WidgetInfo)handler.widgetsActivate[Convert.ToInt32(item.SubItems[1].Text)];

                handler.activateDeactivateWidget(widget.id, !widget.activatedState[modShort], modShort);

                if (!widget.activatedState[modShort]) imageIdx = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("De-/Activation failed!\nError: " + ex.Message, "Error");
                return;
            }

            //this.displayHddList();
            //dont do a full list update, update image index manually
            item.ImageIndex = imageIdx;
            buttonActivateWidget.ImageIndex = imageIdx;
        }

        void listViewHddWidgets_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listViewHddWidgets.SelectedItems.Count != 1) return;

            try
            {
                showWidgetHdd(getSelectedHddLuaId());
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        void loadFromOnlineProfileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (checkLobbyInputData() == false) return;

                if (MessageBox.Show("Do you really want to load the current online profile?", "Overwrite?", MessageBoxButtons.YesNo) ==
                    DialogResult.No) return;

                var installs = WidgetHandler.fetcher.getProfileInstallation(Program.Conf.LobbyPlayerName, Program.Conf.LobbyPlayerPassword);
                foreach (WidgetInfo winfo in handler.widgetsInstall.Values)
                {
                    if (installs.Contains(winfo.nameId))
                    {
                        if (winfo.state == WidgetState.NOT_INSTALLED) handler.installLua(winfo.id, true);
                        else if (winfo.state == WidgetState.OUTDATED) handler.updateWidget(winfo);
                    }
                    else if (winfo.state == WidgetState.INSTALLED || winfo.state == WidgetState.OUTDATED) handler.removeLua(winfo.id);
                }

                //activate
                foreach (var mod in handler.mods.Values)
                {
                    var actives = WidgetHandler.fetcher.getProfileActivation(Program.Conf.LobbyPlayerName, Program.Conf.LobbyPlayerPassword, mod.id);
                    var activateIds = new List<int>();
                    var deactivateIds = new List<int>();
                    foreach (WidgetInfo winfo in handler.widgetsActivate.Values)
                    {
                        if (actives.Contains(winfo.headerName))
                        {
                            //activate
                            if (winfo.activatedState[mod.abbreviation] == false)
                            {
                                activateIds.Add(winfo.id);
                                toogleHddListActivateImage(winfo.headerName, true);
                            }
                        }
                        else
                        {
                            //deactivate
                            if (winfo.activatedState[mod.abbreviation])
                            {
                                deactivateIds.Add(winfo.id);
                                toogleHddListActivateImage(winfo.headerName, false);
                            }
                        }
                    }

                    if (activateIds.Count > 0) handler.activateDeactivateWidgetsEx(activateIds, true, mod.abbreviation);

                    if (deactivateIds.Count > 0) handler.activateDeactivateWidgetsEx(deactivateIds, false, mod.abbreviation);
                }

                refreshDbList();
                refreshHddList();
                showWidgetDb(0);

                MessageBox.Show("Online profile loaded!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Loading online profile failed! Error: " + ex.Message);
            }
        }

        void loadFromOnlineProfileToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            loadFromOnlineProfileToolStripMenuItem_Click(sender, e);
        }

        void loadPresetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            loadActivationPreset();

            displayHddList();
            showWidgetHdd(0);
        }

        void PictureBox_LoadCompleted(Object sender, AsyncCompletedEventArgs e)
        {
            if (e.Error != null) pictureBoxThumb.Image = LuaMgrResources.SpringThumbGlass;
        }

        void pictureBoxThumb_Click(object sender, EventArgs e)
        {
            //show large screenshots
            try
            {
                if (currentWidgetLatest.imageCount == 0) MessageBox.Show("Sorry, there are no images for this widget.", "Image Gallery", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                else
                {
                    var widgetImages = WidgetHandler.fetcher.getImagesByNameId(currentWidgetLatest.nameId);
                    var disp = new ImageGallery(widgetImages);
                    disp.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        void ratingBarWidget_Click(object sender, EventArgs e)
        {
            try
            {
                if (checkLobbyInputData() == false) return;
                var userRat = handler.getPersonalRating(Program.Conf.LobbyPlayerName, Program.Conf.LobbyPlayerPassword, currentWidgetLatest.nameId);

                var rwnd = new RateWindow(currentWidgetLatest.name, currentWidgetLatest.nameId, userRat);
                rwnd.ShowDialog();

                if (rwnd.DialogResult == DialogResult.OK)
                {
                    var info = WidgetHandler.fetcher.getLuaById(currentWidgetLatest.id);
                    currentWidgetLatest.voteCount = info.voteCount;
                    currentWidgetLatest.rating = info.rating;

                    if (currentWidgetCurVersion != null)
                    {
                        currentWidgetCurVersion.voteCount = info.voteCount;
                        currentWidgetCurVersion.rating = info.rating;
                    }

                    showWidgetDb(currentWidgetLatest.id);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Rating failed!\nError: " + ex.Message, "Error");
                return;
            }
        }

        void removeSelectedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var ids = getSelectedDbLuaIds();

            try
            {
                foreach (var id in ids) handler.uninstallWidget(id);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Uninstall failed!\nError: " + ex.Message, "Error");
                return;
            }

            displayDbList();
            showWidgetDb(0);
            MessageBox.Show("Uninstall complete! " + ids.Count + " widgets wiped!", "Success");
        }

        void saveAsPresetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            exportActivationPreset();
        }

        void saveToOnlineProfileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (checkLobbyInputData() == false) return;

                if (
                    MessageBox.Show("Do you really want to save your current profile online overwritting your current online profile?",
                                    "Overwrite?",
                                    MessageBoxButtons.YesNo) == DialogResult.No) return;

                var activated = new List<String>();
                foreach (var mod in handler.mods.Values)
                {
                    activated.Clear();
                    foreach (WidgetInfo winfo in handler.widgetsActivate.Values)
                    {
                        if (winfo.activatedState[mod.abbreviation])
                        {
                            /*string name;
                            if (winfo.dbIsAvail)
                            {
                                name = winfo.name;
                            }
                            else
                            {
                                name = winfo.headerName;
                            }*/
                            activated.Add(winfo.headerName);
                        }
                    }

                    WidgetHandler.fetcher.setProfileActivation(activated, mod.id, Program.Conf.LobbyPlayerName, Program.Conf.LobbyPlayerPassword);
                }

                var installed = new List<int>();
                foreach (WidgetInfo winfo in handler.widgetsInstall.Values) if (winfo.state == WidgetState.INSTALLED) installed.Add(winfo.nameId);
                WidgetHandler.fetcher.setProfileInstallation(installed, Program.Conf.LobbyPlayerName, Program.Conf.LobbyPlayerPassword);

                MessageBox.Show("Profile saved online!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Saving online profile failed! Error: " + ex.Message);
            }
        }

        void saveToOnlineProfileToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            saveToOnlineProfileToolStripMenuItem_Click(sender, e);
        }

        void tabCtrlWidgetCategory_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                var tabCtrl = (TabControl)sender;
                if (tabCtrl.SelectedIndex == 2)
                {
                    tabCtrl.Size = new Size(panelWidget.Location.X + panelWidget.Size.Width - tabCtrl.Location.X, panelWidget.Size.Height + 5);
                    tabCtrl.Anchor = tabCtrl.Anchor | AnchorStyles.Right;
                    tabCtrl.Refresh();
                }
                else
                {
                    tabCtrl.Size = new Size(tabCtrlOriginalWidth, panelWidget.Size.Height - tabCtrlOrgHeightDiff);
                    tabCtrl.Anchor = tabCtrlOriginalAnchor;
                    tabCtrl.Refresh();
                    updateWidgetCountDisplay();

                    showWidgetEx(0, true);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error");
            }
        }

        void textBoxFilter_KeyUp(object sender, KeyEventArgs e)
        {
            //update member filter string and refresh listView
            widgetFilterString = textBoxFilter.Text;
            displayDbList();
            displayHddList();
        }

        void textBoxFilter_TextChanged(object sender, EventArgs e)
        {
            textBoxFilter_KeyUp(sender, null);
        }

        void timerWidgetRefresh_Tick(object sender, EventArgs e)
        {
            startRefreshListsThread(0);
        }
    }
}