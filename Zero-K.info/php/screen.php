<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" xml:lang="en" lang="en">
<head>
	<title>Zero-K - Screenshots</title>
	<meta http-equiv="Content-type" content="text/html; charset=utf-8" />
	<meta name="description" content="Screenshots" />
	<meta name="keywords" content="Zero K, zero-k, game, rts, Real Time Strategy, awesome, robot, mech" />
	<link rel="stylesheet" href="styles/style.css" type="text/css" media="screen" title="Main Style" charset="utf-8" />
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

<body id="screen" onload="SwitchContent('showme','hideme','','');">

<?php include("menu.inc");?>

<div id="screenshots" class="midl">
<center>
<?php
$images = scandir('img/screenshots');
$thumbs = scandir('img/screenshots_thumb');
for( $i = 2; $i <= count($thumbs); $i++)
{

  $fname = $thumbs[$i];
  $gname = $images[$i];
  if (substr($fname, strlen($fname) - 4) == ".png") echo "<a href='img/screenshots/$gname'><img src='img/screenshots_thumb/$fname' class='scr' ></img></a>\n";
}
?>
</center>
</div> <!-- close screenshots -->

<?php include("footer.inc");?>

