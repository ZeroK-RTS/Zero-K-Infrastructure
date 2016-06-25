// BatchFileUploader.cs
// This program uploads batch of files, found in specified folder, to MediaWiki-powered wiki site
// This is a C# program, based on DotNetWikiBot Framework 3, it requires .NET or Mono to run
// Typical build command: csc.exe /target:winexe BatchFileUploader.cs /reference:DotNetWikiBot.dll
// Latest version can be obtained at http://dotnetwikibot.sourceforge.net
// Distributed under the terms of the GNU GPLv2 license: http://www.gnu.org/licenses/gpl-2.0.html
// Copyright © Iaroslav Vassiliev (Moscow, 2009-2013) codedriller@gmail.com

using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using DotNetWikiBot;

public class UploadForm : Form
{
	private Site site;
	private char dirSepChar = Path.DirectorySeparatorChar;

	private Button UploadButton = new Button ();
	private Button FolderBrowserButton = new Button ();
	private FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog ();
	private ProgressBar UploadProgressBar = new ProgressBar ();
	private TextBox siteTextBox = new TextBox ();
	private TextBox userNameTextBox = new TextBox ();
	private TextBox userDomainTextBox = new TextBox ();
	private TextBox passwordTextBox = new TextBox ();
	private TextBox folderTextBox = new TextBox ();
	private TextBox fileTypesTextBox = new TextBox ();
	private TextBox filesDescrTextBox = new TextBox ();
	private Label siteLabel = new Label ();
	private Label userNameLabel = new Label ();
	private Label userDomainLabel = new Label ();
	private Label passwordLabel = new Label ();
	private Label folderLabel = new Label ();
	private Label fileTypesLabel = new Label ();
	private Label filesDescrLabel = new Label ();

	public UploadForm()
	{
		this.Text = "Batch file uploader for MediaWiki-powered sites";
		this.Size = new Size( 600, 550 );
		this.Location = new Point( 50, 50 );
		this.FormBorderStyle = FormBorderStyle.FixedSingle;

		siteLabel.Text = "Site:";
		siteLabel.TextAlign = ContentAlignment.TopRight;
		siteLabel.Size = new Size( 200, 20 );
		siteLabel.Location = new Point( 20, 20 );
		this.Controls.Add( siteLabel );

		siteTextBox.Text = "http://wiki.domain.com";
		siteTextBox.Size = new Size( 150, 20 );
		siteTextBox.Location = new Point( 250, 15 );
		this.Controls.Add( siteTextBox );

		userNameLabel.Text = "User:";
		userNameLabel.TextAlign = ContentAlignment.TopRight;
		userNameLabel.Size = new Size( 200, 20 );
		userNameLabel.Location = new Point( 20, 50 );		// + 0,30
		this.Controls.Add( userNameLabel );

		userNameTextBox.Size = new Size( 150, 20 );
		userNameTextBox.Location = new Point( 250, 45 );	// + 0,30
		this.Controls.Add( userNameTextBox );

		passwordLabel.Text = "Password:";
		passwordLabel.TextAlign = ContentAlignment.TopRight;
		passwordLabel.Size = new Size( 200, 20 );
		passwordLabel.Location = new Point( 20, 80 );		// + 0,30
		this.Controls.Add( passwordLabel );

		passwordTextBox.Size = new Size( 150, 20 );
		passwordTextBox.Location = new Point( 250, 75 );	// + 0,30
		passwordTextBox.PasswordChar = '*';
		this.Controls.Add( passwordTextBox );

		userDomainLabel.Text = "LDAP authentication domain:";
		userDomainLabel.TextAlign = ContentAlignment.TopRight;
		userDomainLabel.Size = new Size( 200, 20 );
		userDomainLabel.Location = new Point( 20, 110 );		// + 0,30
		this.Controls.Add( userDomainLabel );

		userDomainTextBox.Text = "";
		userDomainTextBox.Size = new Size( 150, 20 );
		userDomainTextBox.Location = new Point( 250, 105 );	// + 0,30
		this.Controls.Add( userDomainTextBox );

		folderLabel.Text = "Folder with files to upload:";
		folderLabel.TextAlign = ContentAlignment.TopRight;
		folderLabel.Size = new Size( 200, 20 );
		folderLabel.Location = new Point( 20, 140 );		// + 0,30
		this.Controls.Add( folderLabel );

		folderTextBox.Text = Environment.CurrentDirectory + dirSepChar + "Files";
		folderTextBox.Size = new Size( 250, 20 );
		folderTextBox.Location = new Point( 250, 135 );	// + 0,30
		this.Controls.Add( folderTextBox );

		FolderBrowserButton.Size = new Size( 60, 20 );
		FolderBrowserButton.Location = new Point( 510, 135 );
		FolderBrowserButton.Text = "Select";
		this.Controls.Add( FolderBrowserButton );
		FolderBrowserButton.Click += new EventHandler( FolderBrowserButtonClick );

		fileTypesLabel.Text = "File types (extensions) to upload:";
		fileTypesLabel.TextAlign = ContentAlignment.TopRight;
		fileTypesLabel.Size = new Size( 200, 20 );
		fileTypesLabel.Location = new Point( 20, 170 );		// + 0,30
		this.Controls.Add( fileTypesLabel );

		fileTypesTextBox.Text = "png, gif, jpg, jpeg, xcf, pdf, mid, ogg, ogv, svg, djvu, oga";
		fileTypesTextBox.Size = new Size( 250, 20 );
		fileTypesTextBox.Location = new Point( 250, 165 );	// + 0,30
		this.Controls.Add( fileTypesTextBox );

		filesDescrLabel.Text = "Files description:";
		filesDescrLabel.Size = new Size( 200, 20 );
		filesDescrLabel.Location = new Point( 20, 210 );		// + 0,45
		this.Controls.Add( filesDescrLabel );

		filesDescrTextBox.Size = new Size( 550, 200 );
		filesDescrTextBox.Location = new Point( 20, 230 );		// + 0,20
		filesDescrTextBox.Multiline = true;
		filesDescrTextBox.ScrollBars = ScrollBars.Vertical;
		filesDescrTextBox.AcceptsReturn = true;
		filesDescrTextBox.AcceptsTab = true;
		filesDescrTextBox.WordWrap = true;
		filesDescrTextBox.Text = File.Exists( "DefaultDescription.txt" )
			? File.ReadAllText( "DefaultDescription.txt" )
			:	"{{Information\r\n" + 
				"| Description    = \r\n" +
				"| Source         = \r\n" +
				"| Date           = \r\n" +
				"| Author         = \r\n" +
				"| other_versions = \r\n" +
				"}}";
		this.Controls.Add( filesDescrTextBox );

		UploadButton.Size = new Size( 120, 20 );
		UploadButton.Location = new Point( 450, 440 );		// + 0,210
		UploadButton.Text = "Upload files";
		this.Controls.Add( UploadButton );
		UploadButton.Click += new EventHandler( UploadButtonClick );

		UploadProgressBar.Size = new Size( 550, 20 );
		UploadProgressBar.Location = new Point( 20, 480 );	// + 0,40
		this.Controls.Add( UploadProgressBar );
	}

	void FolderBrowserButtonClick( object sender, EventArgs ea )
	{
		DialogResult result = folderBrowserDialog.ShowDialog();
		if ( result == DialogResult.OK )
		{
			folderTextBox.Text = folderBrowserDialog.SelectedPath;
		}
	}

	void UploadButtonClick( object sender, EventArgs ea )
	{
		if ( string.IsNullOrEmpty( siteTextBox.Text 		) ||
			 string.IsNullOrEmpty( userNameTextBox.Text 	) ||
			 string.IsNullOrEmpty( passwordTextBox.Text 	) ||
			 string.IsNullOrEmpty( folderTextBox.Text 		) ||
			 string.IsNullOrEmpty( filesDescrTextBox.Text 	) )
		{
			MessageBox.Show( "Fill in all required data fileds, please." );
			return;
		}

		this.Cursor = Cursors.WaitCursor;

		if ( site == null )
		{
			try
			{
				site = new Site(
					siteTextBox.Text.Trim(),
					userNameTextBox.Text.Trim(),
					passwordTextBox.Text.Trim(),
					userDomainTextBox.Text.Trim()
				);
			}
			catch ( Exception e )
			{
				this.Cursor = Cursors.Default;
				MessageBox.Show( e.Message + "\n" + e.InnerException);
				return;
			}
		}

		if ( !Directory.Exists( folderTextBox.Text ) )
		{
			this.Cursor = Cursors.Default;
			MessageBox.Show( "Specified folder doesn't exist." );
			return;
		}

		Regex allowedFileTypes;
		if ( !string.IsNullOrEmpty( fileTypesTextBox.Text ) )
		{
			allowedFileTypes = new Regex( String.Format( "(?i)\\.({0})$",
				fileTypesTextBox.Text.Replace( " ", "" ).Replace( ",", "|" ) ) );
		}
		else
		{
			allowedFileTypes = new Regex( "." );
		}
		string[] filenames =
			Array.FindAll( Directory.GetFiles( folderTextBox.Text ), allowedFileTypes.IsMatch );
		if ( filenames.Length == 0 )
		{
			this.Cursor = Cursors.Default;
			MessageBox.Show( "Specified folder doesn't contain files, that could be uploaded." );
			return;
		}

		UploadProgressBar.Visible = true;
		UploadProgressBar.Minimum = 0;
		UploadProgressBar.Maximum = filenames.Length;
		UploadProgressBar.Value = 0;
		UploadProgressBar.Step = 1;

		Page p = new Page( site );
		string filename;
		for ( int i = 0; i < filenames.Length; UploadProgressBar.PerformStep(), i++ )
		{
			filename = Path.GetFileName( filenames[i] );
			p.title = site.GetNsPrefix(6) + filename;
			try
			{
				p.Load();
			}
			catch ( Exception e )
			{
				MessageBox.Show( e.Message );
				continue;
			}
			if ( p.Exists() && MessageBox.Show(
					String.Format( "File \"{0}\" already exists. Overwrite?", filename ),
					"Вопрос",
					MessageBoxButtons.YesNo,
					MessageBoxIcon.Question ) != DialogResult.Yes )
			{
				continue;
			}

			try
			{
				p.UploadImage( filenames[i], filesDescrTextBox.Text, "", "", "" );
			}
			catch ( Exception e )
			{
				MessageBox.Show( e.Message );
				continue;
			}
			File.AppendAllText("UploadedFiles.txt", filenames[i] + "\r\n");
		}

		MessageBox.Show( "Upload completed." );
		UploadProgressBar.Value = 0;
		this.Cursor = Cursors.Default;
	}

	[STAThread]
	public static void Main()
	{
		Application.EnableVisualStyles();
		Application.Run( new UploadForm () );
	}
}