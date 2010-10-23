<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" xml:lang="en" lang="en">
<head>
	<title>Zero-K - Download</title>
<?php include("inc_head.inc"); ?>
</head>

<body>
	<div id="wrapper">
<!-------------------------------------------------------------- -->
<?php include("inc_header.inc"); ?>
<!-------------------------------------------------------------- -->
<?php include("inc_menu.inc"); ?>
<!-------------------------------------------------------------- -->
<div id="download" class="border">
	<h1>Windows</h1>
	<a href="http://planet-wars.eu/sd/setup.exe" id="download" class="border">Download</a>
	<h2>Start Playing!</h2>
<!-------------------------------------------------------------- -->
	<h1>Linux</h1>
	<h2>1.)</h2> <p><a href="http://springrts.com/wiki/SetupGuide">Get the Engine</a>
	<h2>2.)</h2> <p>Install python/pip and rapid downloader
<pre>
sudo apt-get install python-dev python-setuptools python-pip
sudo easy_install rapid-spring
rapid pin zk:test
</pre>
	<h2>3.)</h2> <p>Download maps from <a href="http://www.springfiles.com/files.php?subcategory_id=2">here</a>.
	<h2>Start Playing!</h2>
	<p>To upgrade to newer version:
<pre>
rapid clean-upgrade
</pre>
		<br />
<!-------------------------------------------------------------- -->
	<h1>Mac</h1>
	<p>See <a href="http://springrts.com/phpbb/viewforum.php?f=65">Engine forums</a> for installing Spring on a Mac.
</div><!close download>
</div><!close wrapper>
</body>
</html>