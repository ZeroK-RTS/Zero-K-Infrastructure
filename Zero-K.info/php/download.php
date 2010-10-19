<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" xml:lang="en" lang="en">
<head>
	<title>Zero-K - Download</title>
	<meta http-equiv="Content-type" content="text/html; charset=utf-8" />
	<meta name="description" content="Download the best RTS ever!" />
	<meta name="keywords" content="Zero K, zero-k, game, rts, Real Time Strategy, awesome, robot, mech" />
	<link rel="stylesheet" href="style.css" type="text/css" media="screen" title="Main Style" charset="utf-8" />
	<link rel="icon" href="img/favicon.png" />
	<script type="text/javascript" language="JavaScript"><!--
function SwitchContent(a,b,c,d) 
{
	document.getElementById(a).style.display = "";
	document.getElementById(b).style.display = "none";
	document.getElementById(c).style.display = "none";
	document.getElementById(d).style.display = "none";
}
function clearinput(a) { document.getElementById(a).value = ""; }

//--></script>
</head>

<body id="dl" onload="SwitchContent('showme','hideme','','');">

<?phpinclude("menu.inc");?>

<div id="download" class="midl">
	<p>The installer is not yet fully operational. There are a few things that need to be completed beforehand. For now you'll just have to follow three easy steps to start playing. These are outlined below.</p>
	<h1>Quick Start</h1>
	<p>At the moment the engine is not included in the download. You can get it <a href="http://springrts.com/wiki/Download">here</a>.</p>
	<p>Once you have the engine, you can download the latest stable version <a href="http://files.caspring.org/snapshots/latest.sdz.php">here</a>.</p>
	<p>Now you have the engine, and the game, but no maps! No worries, you can get them <a href="http://www.springfiles.com/files.php?subcategory_id=2">here</a>. (you can also get them through the lobby's integrated downloader)</p>
	
	<!--
	<h1>New Users (Windows)</h1>
	<p>You will need the <a href="http://files.caspring.org/caupdater/SpringDownloader.exe">Spring Downloader</a> as well as <a href="http://www.microsoft.com/downloads/details.aspx?familyid=0856eacb-4362-4b0d-8edd-aab15c5e04f5&displaylang=en">Microsoft .NET 2.0 or newer</a> (If you don't auto-update your Windows XP) to download maps and other files, and also to keep your game up to date.</p>
	
	<p>You will also need a <b>lobby</b> to access the multiplayer chatrooms and battlerooms. Pick one from the list below:</p>
	<ul>	
		<li><a href="http://tasclient.licho.eu/TASClientLatest.7z">TASClient</a> - stable and feature complete lobby for windows</li>
		<li><a href="http://springlobby.info/wiki/springlobby/">Springlobby</a> - multiplatform lobby for windows and linux</li>
		<li><a href="http://code.google.com/p/qtlobby/">QtLobby</a> - multiplatform lobby still in development</li>
	</ul>
	-->
	
	<!--
	<h1>New Users (Linux)</h1>
		<h2>Ubuntu</h2>
		<div id="ubuntu">
			<p>CA Installer is included in the Spring Ubuntu repository. The package name is "ca-installer".</p>
			<p>The repository address is: </p>
			<pre>deb http://ppa.launchpad.net/spring/ubuntu intrepid main
deb-src http://ppa.launchpad.net/spring/ubuntu intrepid main</pre>
			<p>To install: </p>
			<pre>sudo aptitude update
sudo aptitude install ca-installer</pre>
			<p>Once installed, you will have a menu entry under Applications->Games->CA Installer.</p>
			<p>Please refer to the <a href="http://spring.clan-sy.com/wiki/Ubuntu_install">Spring Install Guide</a> if you need more detailed directions on enabling this repository. </p>
		</div> --> <!-- close ubuntu -->

	<div id="svn" style="display:none;">
		<h1>Source/Subversion</h1>
		<ol>
			<li>Get <a href="http://tortoisesvn.tigris.org/">Subversion</a> (commonly known as "svn")</li>
			<li>Create a folder inside your spring/mods directory with an <b>.sdd</b> extension. (ie. <i>zero-k.sdd</i>)</li>
			<!-- Note: the svn address for modelbase is  svn://springrts.com/modelbase -->
			<li>Check out the svn repository. (right-click on the folder and select "SVN Checkout")</li>
			<li>The URL of the repository: <b>svn://svn.caspring.org/trunk/mods/ca</b> </li>
			<li>Wait until files have downloaded.</li>
			<li>Congrats! You now have a working version of the source game (if you need any more help click <a href="http://trac.caspring.org/wiki/SubversionRepository">here</a>)</li>
		</ol>
	</div>
</div> <!-- close download -->

<?phpinclude("footer.inc");?>

