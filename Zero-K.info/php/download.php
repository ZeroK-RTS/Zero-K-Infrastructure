<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" xml:lang="en" lang="en">
<head>
	<title>Zero-K - Download</title>
<?php include("inc_head.inc"); ?>
</head>

<?php include("inc_rotate.inc"); ?>

<body>
	<div id="wrapper">
<!-------------------------------------------------------------- -->
<?php include("inc_menu.inc"); ?>
<!-------------------------------------------------------------- -->
<div id="windows" class="border">
	<h1>Windows</h1>
	<br /><br />
	<a href="http://zero-k.info/lobby/setup.exe" id="download" class="button border">Download</a><br />
	<br /><br />
	<h3>Problems installing?</h3>
	<ul>
	<li>Try running installer again
	<li>Try rebooting and then running installer again
	<li>Download <a href='http://www.microsoft.com/downloads/en/details.aspx?displaylang=en&FamilyID=5765d7a8-7722-4888-a970-ac39b33fd8ab'>Microsoft .NET 4.0 CP</a> manually and then run installer again 
	</ul>
	<b>Start Playing!</b>
</div><!close windows>
<!-------------------------------------------------------------- -->
<div id="linux" class="border">
	<h1>Linux</h1>
	<b>1.)</b> <a href="http://springrts.com/wiki/SetupGuide">Get the Engine</a><br />
	<b>2.)</b> Install python/pip and rapid downloader
<pre>
sudo apt-get install python-dev python-setuptools python-pip
sudo easy_install rapid-spring
rapid pin zk:test
</pre>
	<b>3.)</b> Download maps from <a href="http://www.springfiles.com/files.php?subcategory_id=2">here</a>.<br /><br />
	<b>Start Playing!</b>
	<p>To upgrade to newer version:
<pre>
rapid clean-upgrade
</pre>
</div><!close linux>
<!-------------------------------------------------------------- -->
<div id="mac" class="border">
	<h1>Mac</h1>
	<p>See <a href="http://springrts.com/phpbb/viewforum.php?f=65">Engine forums</a> for installing Spring on a Mac.
</div><!close mac>
<!-------------------------------------------------------------- -->
<?php include("inc_footer.inc"); ?>
<!-------------------------------------------------------------- -->
<div class="border">
	<h1>From SVN</h1>
	<b>1.)</b> get svn <pre>sudo apt-get install subversion</pre>
	<b>2.)</b> checkout source from site <pre>svn co ---svn address--- ~where you want it</pre>
	<b>3.)</b> make link to spring so you can play it any time <pre>ln -s ~/where you put it ~/.spring/mods</pre>
</div><!close sources>
</div><!close wrapper>
</body>
</html>
