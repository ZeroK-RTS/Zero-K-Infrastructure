using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Collections;
using LuaManagerLib;
using System.Globalization;
using System.Net;
using System.Threading;
using System.Configuration;

namespace LuaAdmin
{
    public partial class WidgetAdmin : Form
    {
        private readonly string thumbnailBaseUrl;// = "http://gadgetdb.springrts.de/thumbnails/";

        public WidgetAdmin(string serverUrl)
        {
            InitializeComponent();

            thumbnailBaseUrl = serverUrl + "/thumbnails/";

            //set to us to get version numbers ALWAYS in this form "4.1" (not country-specific)
            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en-US");

            this.Text = "Spring Widget Admin " + Utils.commaToDot( Program.version.ToString() );

            try
            {
                this.autoUpdate();
            }
            catch
            {
                MessageBox.Show("Auto-Update failed! Wrong Server URL?", "Auto-Update Error");
            }
        }

        private void autoUpdate()
        {
            Decimal latVer;
            string url = "";
            string filename = "";
#pragma warning disable 612,618
            this.labelServerUrl.Text = ConfigurationSettings.AppSettings["ServerUrl"];
#pragma warning restore 612,618
            Program.fetcher.getLatestVersionInfo(out latVer, out url, out filename);

            if ((latVer > Program.version) && (MessageBox.Show("There is a new version available. Do you want to download now?", "Update available", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes))
            {
                try
                {
                    SaveFileDialog fa = new SaveFileDialog();
                    fa.AddExtension = true;
                    fa.Filter = "ZIP Archive (*.zip)|*.zip";
                    fa.Title = "Download ZIP Package";
                    fa.FileName = filename;

                    if (fa.ShowDialog() == DialogResult.OK)
                    {
                        WebClient wc = new WebClient() { Proxy = null };
                        wc.DownloadFile(url, fa.FileName);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("ZIP Download failed! Error: " + ex.Message, "Error");
                    return;
                }
            }
        }

        private void login(string name, string password)
        {
            Program.fetcher.setLoginData(name, password);
            try
            {
                Program.UserId = Program.fetcher.getUserId();
                if (Program.UserId < 0)
                {
                    Program.UserId = -1 * Program.UserId;
                    Program.isSuperAdmin = true;
                }

           
                updateNamesListBox();
                updateGamesListBox();
                updateCategoriesListBox();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Login Error! Message: " + ex.Message, "Error");
            }
        }

        private void buttonConnect_Click(object sender, EventArgs e)
        {
            this.login(textBoxLogin.Text, textBoxPassword.Text);
        }

        private void buttonAddWidget_Click(object sender, EventArgs e)
        {
            if (textBoxAddWidgetName.Text.Length > 0)
            {
                if (MessageBox.Show("Really add a new widget \"" + textBoxAddWidgetName.Text + "\" to the database?", "Add New Widget", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    Program.fetcher.addName(textBoxAddWidgetName.Text);
                    updateNamesListBox();
                }
            }
        }

        private void listViewWidgets_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                this.updateWidgetNameDisplay(getSelectedWidgetName());
            }
            catch (Exception)
            {
                buttonUpdateThumbnail.Enabled = false;
                pictureBoxThumb.Enabled = false;
                buttonSaveName.Enabled = false;
                buttonRemoveImage.Enabled = false;
                buttonAddImage.Enabled = false;
                textBoxDesc.Enabled = false;
                textBoxMods.Enabled = false;
                textBoxName.Enabled = false;
                textBoxAuthor.Enabled = false;
                listViewImages.Enabled = false;

                listViewVersions.Enabled = false;
                buttonAddVersion.Enabled = false;
                buttonRemoveVersion.Enabled = false;
                textBoxAddVersion.Enabled = false;

                textBoxGameShortname.Enabled = false;
                textBoxOrderFilename.Enabled = false;

                buttonSaveOrderFilename.Enabled = false;

                checkBoxWidgetHidden.Enabled = false;
                comboBoxCategory.Enabled = false;
            }
        }

        private void listViewVersions_SelectedIndexChanged(object sender, EventArgs e)
        {
            updateVersionView();
        }

        private void saveName_Click(object sender, EventArgs e)
        {
            Program.fetcher.updateName(this.getSelectedNameId(), textBoxName.Text, textBoxAuthor.Text, textBoxMods.Text, textBoxDesc.Text, checkBoxWidgetHidden.Checked, (comboBoxCategory.SelectedItem as Category).id );

            WidgetName current = (WidgetName)Program.allLuaNames[this.getSelectedWidgetName()];
            current.Name = textBoxName.Text;
            current.Author = textBoxAuthor.Text;
            current.SupportedMods = textBoxMods.Text;
            current.Description = textBoxDesc.Text;
            current.Hidden = checkBoxWidgetHidden.Checked;
            current.CategoryId = (comboBoxCategory.SelectedItem as Category).id;
        }

        private void buttonAddVersion_Click(object sender, EventArgs e)
        {
            if (textBoxAddVersion.Text.Length == 0)
            {
                MessageBox.Show("Please enter a version, like \"3.16\" .");
                return;
            }

            decimal dec = new decimal(0.0);
            try
            {
//                dec = decimal.Parse(this.textBoxAddVersion.Text.Replace(".", ","));
                dec = decimal.Parse(this.textBoxAddVersion.Text);
            }
            catch (FormatException)
            {
                MessageBox.Show("Please enter a decimal dotted number.","Error",MessageBoxButtons.OK,MessageBoxIcon.Warning );
                return;
            }

            try
            {
                Program.fetcher.addLuaVersion(dec, getSelectedNameId());

                //refresh - todo: make a function out of it
                Program.allLuas = Program.fetcher.getAllLuas();
                this.updateWidgetNameDisplay(getSelectedWidgetName());
            }
            catch (Exception exc)
            {
                MessageBox.Show("Failed: " + exc.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void saveVersion_Click(object sender, EventArgs e)
        {
            int check = 1;
            if (!checkBoxActive.Checked)
            {
                check = 0;
            }

            Program.fetcher.updateLuaVersion(getSelectedLuaId(), textBoxChangelog.Text, check);

            WidgetInfo current = this.getSelectedLua();
            current.changelog = textBoxChangelog.Text;
            current.active = check;

        }

        private void updateNamesListBox()
        {
            Program.allLuas = Program.fetcher.getAllLuas();
            Program.allLuaNames = Program.fetcher.getNames();
            listViewWidgets.Clear();

            IEnumerator ienum = Program.allLuaNames.Values.GetEnumerator();
            while (ienum.MoveNext())
            {
                WidgetName info = (WidgetName)ienum.Current;

                ListViewItem item = new ListViewItem(info.Name);
                item.Tag = info.Id.ToString();

                if (!Program.isSuperAdmin && (info.ownerId != Program.UserId))
                {
                    item.ForeColor = Color.LightGray;
                }

                listViewWidgets.Items.Add(item);
            }

            try
            {
                this.updateWidgetNameDisplay(getSelectedWidgetName());
            }
            catch (Exception)
            {
                this.updateWidgetNameDisplay("");
            }

        }

        private void updateGamesListBox()
        {
            Program.allGames = Program.fetcher.getActivationMods();
            Program.allGameWidgets = Program.fetcher.getModWidgetsAll();
            listViewGames.Clear();

            IEnumerator ienum = Program.allGames.Values.GetEnumerator();
            while (ienum.MoveNext())
            {
                ModInfoDb modInfo = (ModInfoDb)ienum.Current;

                ListViewItem item = new ListViewItem(modInfo.abbreviation);
                item.Tag = modInfo.id.ToString();

                if (!Program.isSuperAdmin && (modInfo.ownerId != Program.UserId))
                {
                    item.ForeColor = Color.LightGray;
                }

                listViewGames.Items.Add(item);
            }

            try
            {
                this.updateGameWidgetList(getSelectedGame());
            }
            catch (Exception)
            {
                this.updateGameWidgetList("");
            }

        }
 

        private int getSelectedNameId()
        {
            IEnumerator ienum = this.listViewWidgets.SelectedItems.GetEnumerator();
            ienum.MoveNext();
            ListViewItem selectedItem = (ListViewItem)ienum.Current;

            return int.Parse((String)selectedItem.Tag);
        }

        private string getSelectedWidgetName()
        {
            IEnumerator ienum = this.listViewWidgets.SelectedItems.GetEnumerator();
            ienum.MoveNext();
            ListViewItem selectedItem = (ListViewItem)ienum.Current;

            return selectedItem.Text;
        }


        private string getSelectedGameWidget()
        {
            IEnumerator ienum = this.listViewGameWidgetList.SelectedItems.GetEnumerator();
            ienum.MoveNext();
            ListViewItem selectedItem = (ListViewItem)ienum.Current;

            return selectedItem.Text;
        }

        private int getSelectedGameWidgetId()
        {
            IEnumerator ienum = this.listViewGameWidgetList.SelectedItems.GetEnumerator();
            ienum.MoveNext();
            ListViewItem selectedItem = (ListViewItem)ienum.Current;

            return Convert.ToInt32(selectedItem.Tag);
        }


        private Category getSelectedCategory()
        {
            return Program.allCategories[this.getSelectedCategoryId()];
        }

        private int getSelectedCategoryId()
        {
            IEnumerator ienum = this.listViewCategories.SelectedItems.GetEnumerator();
            ienum.MoveNext();
            ListViewItem selectedItem = (ListViewItem)ienum.Current;

            return Convert.ToInt32(selectedItem.Tag);
        }

        private String getSelectedGame()
        {
            IEnumerator ienum = this.listViewGames.SelectedItems.GetEnumerator();
            ienum.MoveNext();
            ListViewItem selectedItem = (ListViewItem)ienum.Current;

            return selectedItem.Text;
        }

        private int getSelectedGameId()
        {
            IEnumerator ienum = this.listViewGames.SelectedItems.GetEnumerator();
            ienum.MoveNext();
            ListViewItem selectedItem = (ListViewItem)ienum.Current;

            return int.Parse((String)selectedItem.Tag);
        }

        private int getSelectedLuaId()
        {
            IEnumerator ienum = this.listViewVersions.SelectedItems.GetEnumerator();
            ienum.MoveNext();
            ListViewItem selectedItem = (ListViewItem)ienum.Current;

            return int.Parse((String)selectedItem.Tag);
        }

        private WidgetInfo getSelectedLua()
        {
            return (WidgetInfo)Program.allLuas[this.getSelectedLuaId()];
        }

        private void toggleModSettingsDisplayElements(bool active)
        {
            textBoxGameShortname.Enabled = active;
            textBoxOrderFilename.Enabled = active;

            buttonSaveOrderFilename.Enabled = active;
        }

        private void toggleWidgetNameDisplayElements(bool active)
        {
            buttonUpdateThumbnail.Enabled = active;
            pictureBoxThumb.Enabled = active;
            buttonSaveName.Enabled = active;
            buttonRemoveImage.Enabled = active;
            buttonAddImage.Enabled = active;
            textBoxDesc.Enabled = active;
            textBoxMods.Enabled = active;
            textBoxName.Enabled = active;
            textBoxAuthor.Enabled = active;
            listViewImages.Enabled = active;

            listViewVersions.Enabled = active;
            buttonAddVersion.Enabled = active;
            buttonRemoveVersion.Enabled = active;
            textBoxAddVersion.Enabled = active;

            checkBoxWidgetHidden.Enabled = active;
            comboBoxCategory.Enabled = active;
        }


        private void toggleGameWidgetListDisplayElements(bool active)
        {
            listViewGameWidgetList.Enabled = active;
            buttonRemoveGameWidget.Enabled = active;
            buttonAddGameWidget.Enabled = active;

            if ( active == false )
            {
                toggleGameWidgetInfoDisplayElements(false);
            }
        }

        private void toggleGameWidgetInfoDisplayElements(bool active)
        {
            textBoxGameWidgetName.Enabled = active;

            textBoxGameWidgetDescription.Enabled = active;
            buttonSaveGameWidgetInfo.Enabled = active;
        }

        private void updateGameWidgetList(String game)
        {
            listViewGameWidgetList.Clear();

            if ((game.Length == 0 || (Program.UserId != ((ModInfoDb)Program.allGames[game]).ownerId) && (Program.isSuperAdmin == false)))
            {
                this.toggleGameWidgetListDisplayElements(false);
                return;
            }

            toggleGameWidgetListDisplayElements(true);
            this.toggleGameWidgetInfoDisplayElements(false);

            Program.allGames = Program.fetcher.getActivationMods();
            Program.allGameWidgets = Program.fetcher.getModWidgetsAll();

            ModInfoDb minfo = (ModInfoDb)Program.allGames[game];
            WidgetList modWidgetList = minfo.modWidgets;
            foreach (DictionaryEntry elem in modWidgetList )
            {
                WidgetInfo winfo = (WidgetInfo)elem.Value;
                ListViewItem item = new ListViewItem(winfo.headerName);
                item.Tag = winfo.id.ToString();

                listViewGameWidgetList.Items.Add(item);
            }
            try
            {
                this.updateGameWidgetInfo(getSelectedGameWidgetId());
            }
            catch (Exception)
            {
            }

            this.textBoxGameShortname.Text = minfo.abbreviation;
            this.textBoxOrderFilename.Text = minfo.configOrderFilename;
        }

        private void updateGameWidgetInfo(int gameWidgetId)
        {
            WidgetInfo winfo = (WidgetInfo)Program.allGameWidgets[gameWidgetId];
            if (gameWidgetId == 0 )
            {
                this.toggleGameWidgetInfoDisplayElements(false);
                return;
            }

            textBoxGameWidgetName.Text = winfo.headerName;
            textBoxGameWidgetDescription.Text = winfo.headerDescription;
            this.toggleGameWidgetInfoDisplayElements(true);
        }

        private void updateWidgetNameDisplay(string nameIn)
        {

            if ((nameIn.Length == 0 || (Program.UserId != ((WidgetName)Program.allLuaNames[nameIn]).ownerId) && (Program.isSuperAdmin == false)))
            {
                this.toggleWidgetNameDisplayElements(false);
                return;
            }

            this.toggleWidgetNameDisplayElements(true);

            WidgetName name = (WidgetName)Program.allLuaNames[nameIn];
           
            //thumbnail
            pictureBoxThumb.ImageLocation = thumbnailBaseUrl + name.Id.ToString();
            pictureBoxThumb.Visible = true;
            try
            {
                pictureBoxThumb.Load();
            }
            catch (Exception)
            {
                pictureBoxThumb.Image = null;
            }

            textBoxName.Text = name.Name;
            textBoxAuthor.Text = name.Author;
            textBoxDesc.Text = name.Description;
            textBoxMods.Text = name.SupportedMods;
            checkBoxWidgetHidden.Checked = name.Hidden;
            if (name.CategoryId == 0)
            {
                comboBoxCategory.SelectedIndex = 0;
            }
            else
            {
                comboBoxCategory.SelectedItem = Program.allCategories[name.CategoryId];
            }

            listViewImages.Clear();
            LinkedList<FileInfo> images = Program.fetcher.getImagesByNameId(name.Id);
            IEnumerator ienum = images.GetEnumerator();
            while (ienum.MoveNext())
            {
                FileInfo info = (FileInfo)ienum.Current;

                ListViewItem item = new ListViewItem();
                item.Text = info.Url;
                item.Tag = info.id.ToString();
                listViewImages.Items.Add(item);
            }

            listViewVersions.Clear();
            ienum = Program.allLuas.Values.GetEnumerator();
            while (ienum.MoveNext())
            {
                WidgetInfo winfo = (WidgetInfo)ienum.Current;
                if (winfo.nameId == name.Id)
                {
                    ListViewItem item = new ListViewItem();
                    item.Tag = winfo.id.ToString();
                    item.Text = winfo.version.ToString("G29", CultureInfo.InvariantCulture);

                    listViewVersions.Items.Add(item);
                }
            }


            this.updateVersionView();
        }

        private void updateFiles(int luaId)
        {
            LinkedList<FileInfo> files = Program.fetcher.getFileListByLuaId(luaId);
            IEnumerator ienum = files.GetEnumerator();
            listViewFiles.Clear();
            while (ienum.MoveNext())
            {
                FileInfo info = (FileInfo)ienum.Current;
                ListViewItem item = new ListViewItem();
                item.Tag = info.id.ToString();
                item.Text = info.localPath + " Url: " + info.Url + " MD5:" + info.Md5;
                listViewFiles.Items.Add(item);
            }
        }

        private void toggleVersionDisplayElements(bool active)
        {
            buttonAddFile.Enabled = active;
            buttonRemoveFile.Enabled = active;
            saveVersion.Enabled = active;
            textBoxChangelog.Enabled = active;
            checkBoxActive.Enabled = active;
            listViewFiles.Enabled = active;
        }

        private void updateVersionView()
        {
            try
            {
                int luaId = getSelectedLuaId();

                WidgetInfo winfo = ((WidgetInfo)Program.allLuas[luaId]);

                if (winfo.active == 1)
                {
                    checkBoxActive.CheckState = CheckState.Checked;
                }
                else
                {
                    checkBoxActive.CheckState = CheckState.Unchecked;
                }
                textBoxChangelog.Text = winfo.changelog;
                updateFiles(luaId);

                toggleVersionDisplayElements(true);
            }
            catch (Exception)
            {
                textBoxChangelog.Text = "";
                checkBoxActive.CheckState = CheckState.Unchecked;
                updateFiles(0);

                toggleVersionDisplayElements(false);
            }
        }

        

        private void buttonAddFile_Click(object sender, EventArgs e)
        {
            try
            {
                LuaFileUploadDlg dlg = new LuaFileUploadDlg(getSelectedLuaId());
                dlg.ShowDialog();

                updateVersionView();
            }
            catch (Exception)
            {
                MessageBox.Show("Failed!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void buttonAddImage_Click(object sender, EventArgs e)
        {
            try
            {
                AddImageForm dlg = new AddImageForm(this.getSelectedNameId(), false);
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    updateNamesListBox();
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Failed!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void buttonUpdateThumb_Click(object sender, EventArgs e)
        {
            try
            {
                AddImageForm dlg = new AddImageForm(this.getSelectedNameId(), true);
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    updateNamesListBox();
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Failed!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void buttonDeleteWidget_Click(object sender, EventArgs e)
        {
            try
            {
                if (MessageBox.Show("Really wipe the selected widget \"" + this.getSelectedWidgetName() + "\" completely from the database?", "Remove Widget", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    Program.fetcher.deleteName(this.getSelectedNameId());
                    MessageBox.Show("Widget removed!", "Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    updateNamesListBox();
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Remove failed! No widget selected?", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void buttonRemoveImage_Click(object sender, EventArgs e)
        {
            try
            {
                if (MessageBox.Show("Really remove the selected images from the database?", "Remove Widget", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    IEnumerator ienum = this.listViewImages.SelectedItems.GetEnumerator();
                    while (ienum.MoveNext())
                    {
                        ListViewItem selectedItem = (ListViewItem)ienum.Current;

                        Program.fetcher.deleteImage(int.Parse((string)selectedItem.Tag));
                    }

                    updateNamesListBox();
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Failed!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void buttonDeleteVersion_Click(object sender, EventArgs e)
        {
            try
            {
                IEnumerator ienum = this.listViewVersions.SelectedItems.GetEnumerator();
                while (ienum.MoveNext())
                {
                    ListViewItem selectedItem = (ListViewItem)ienum.Current;

                    Program.fetcher.deleteLua(int.Parse((string)selectedItem.Tag));

                }
                updateNamesListBox();
            }
            catch (Exception)
            {
                MessageBox.Show("Failed!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void buttonDeleteFile_Click(object sender, EventArgs e)
        {
            try
            {
                IEnumerator ienum = this.listViewFiles.SelectedItems.GetEnumerator();
                while (ienum.MoveNext())
                {
                    ListViewItem selectedItem = (ListViewItem)ienum.Current;

                    Program.fetcher.deleteFile(int.Parse((string)selectedItem.Tag));
                }

                updateVersionView();
            }
            catch (Exception)
            {
                MessageBox.Show("Failed!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void listViewGames_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                toggleModSettingsDisplayElements(true);
                this.updateGameWidgetList(getSelectedGame());
            }
            catch 
            {
                
            }
        }

        private void listViewGameWidgetList_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                this.updateGameWidgetInfo(getSelectedGameWidgetId());
            }
            catch
            {

            }
        }

        private void buttonAddGame_Click(object sender, EventArgs e)
        {
            try
            {
                if (textBoxAddGameName.Text.Length > 0)
                {
                    if (MessageBox.Show("Really add a new game \"" + textBoxAddGameName.Text + "\" to the database?", "Add New Game", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        Program.fetcher.addMod(textBoxAddGameName.Text);
                        updateGamesListBox();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Exception occured: " + ex.Message);
            }
        }

        private void buttonRemoveGame_Click(object sender, EventArgs e)
        {
            try
            {
                if (MessageBox.Show("Really wipe the selected game \"" + this.getSelectedGame() + "\" completely from the database?", "Remove Widget", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    Program.fetcher.deleteMod(this.getSelectedGameId());
                    MessageBox.Show("Game removed!", "Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    updateGamesListBox();
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Remove failed! No game selected?", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void buttonAddGameWidget_Click(object sender, EventArgs e)
        {
            try
            {
                int modId = this.getSelectedGameId();
                if (textBoxAddGameWidget.Text.Length > 0)
                {
                    if (MessageBox.Show("Really add a new game widget \"" + textBoxAddGameWidget.Text + "\" to the database?", "Add New Game", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        Program.fetcher.addModWidget(textBoxAddGameWidget.Text, modId);
                        updateGameWidgetList(this.getSelectedGame());
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Exception occured: " + ex.Message);
            }
        }

        private void buttonRemoveGameWidget_Click(object sender, EventArgs e)
        {
            try
            {
                if (MessageBox.Show("Really wipe the selected game widget \"" + this.getSelectedGameWidget() + "\" completely from the database?", "Remove Widget", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    Program.fetcher.deleteModWidget(this.getSelectedGameWidgetId());
                    MessageBox.Show("Game widget removed!", "Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    updateGameWidgetList(this.getSelectedGame());
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Remove failed! No game widget selected?", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void buttonSaveGameWidgetInfo_Click(object sender, EventArgs e)
        {
            try
            {
                Program.fetcher.updateModWidget( this.getSelectedGameWidgetId(), textBoxGameWidgetName.Text, textBoxGameWidgetDescription.Text );

                WidgetInfo current = (WidgetInfo)Program.allGameWidgets[this.getSelectedGameWidgetId()];
                current.headerName = textBoxGameWidgetName.Text;
                current.description = textBoxGameWidgetDescription.Text;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Exception occured: " + ex.Message);
            }
        }

        private void buttonSaveOrderFilename_Click(object sender, EventArgs e)
        {
            try
            {
                String modShort = this.textBoxGameShortname.Text;
                String orderFilename = this.textBoxOrderFilename.Text;

                Program.fetcher.updateMod(this.getSelectedGameId(), modShort, orderFilename);

                ModInfoDb modinfo = Program.allGames[this.getSelectedGame()];
                modinfo.abbreviation = modShort;
                modinfo.configOrderFilename = orderFilename;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Exception occured: " + ex.Message);
            }
        }


        private void updateCategoriesListBox()
        {
            Program.allCategories = Program.fetcher.getCategories();
            
            Category noneCat = new Category();
            noneCat.name = "None";
            noneCat.id = 0;
            noneCat.ownerId = 0;

            listViewCategories.Clear();
            comboBoxCategory.Items.Clear();
            comboBoxCategory.Items.Add(noneCat);
            comboBoxCategory.SelectedItem = noneCat;

            foreach( Category cat in Program.allCategories.Values )
            {
                ListViewItem item = new ListViewItem(cat.name);
                item.Tag = cat.id.ToString();

                if (!Program.isSuperAdmin && (cat.ownerId != Program.UserId))
                {
                    item.ForeColor = Color.LightGray;
                }

                listViewCategories.Items.Add(item);

                comboBoxCategory.Items.Add(cat);
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            try
            {
                if (MessageBox.Show("Really delete the selected category \"" + this.getSelectedCategory().name + "\" completely from the database? (Setting all widgets in the category to \"no category\")", "Remove Category", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    Program.fetcher.removeCategory(this.getSelectedCategoryId());
                    MessageBox.Show("Category removed!", "Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    updateCategoriesListBox();
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Remove failed! No category selected?", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void buttonAddCategory_Click(object sender, EventArgs e)
        {
            try
            {
                if (textBoxAddCategory.Text.Length > 0)
                {
                    if (MessageBox.Show("Really add a category \"" + textBoxAddCategory.Text + "\" to the database?", "Add New Category", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        Program.fetcher.addCategory(textBoxAddCategory.Text);
                        updateCategoriesListBox();
                        textBoxAddCategory.Text = "";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Exception occured: " + ex.Message);
            }
        }

        private void listViewCategories_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                bool val = false;
                if (this.getSelectedCategory().ownerId == Program.UserId || Program.isSuperAdmin )
                {
                    val = true;
                }
                buttonRemoveCategory.Enabled = val;
            }
            catch { }
        }
    }
}
