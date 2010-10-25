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
	<a href="http://planet-wars.eu/sd/setup.exe" id="download" class="button border">Download</a><br /><br /><br />
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
</div><!close wrapper>
</body>
</html>