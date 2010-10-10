<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<?
$screenshots = scandir('img/screenshots');

$count = count($screenshots);

for($i=0; $i<$count; $i++)
{
	$file = array_shift($screenshots);
	if( strtolower( substr($file, -4) ) == '.jpg' )
	{
		array_push($screenshots, $file);
	}
}
$screenshot = $screenshots[array_rand($screenshots)];
?>

<html xmlns="http://www.w3.org/1999/xhtml" xml:lang="en" lang="en">
<head>
	<title>Zero-K - Home</title>
	<meta http-equiv="Content-type" content="text/html; charset=utf-8" />
	<meta name="description" content="Zero-K is a FREE, multiplatform, open-source RTS game, which aims to be a dynamic, action-packed, hassle-free game, full of clever strategies and constantly moving combat which usually lasts on average 20-30 minutes." />
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
<style type="text/css" > 
body { background: #000 url('img/screenshots/<? echo $screenshot; ?>') no-repeat fixed top center; } 
</style>

</head>

<body id="index" onload="SwitchContent('showme','hideme','','');">

<?include("menu.inc");?>

<div id="rlist">

	<div id="poll" class="box">
		<h2><a href="">Poll</a></h1>
		What is your favorite kind of cake?
		<form method="post" action="poll.php">
		<ul class="poll">
			<li><input type="radio" name="cake" value="vanilla" /> Vanilla </li>
			<li><input type="radio" name="cake" value="choc" /> Chocolate </li>
			<li><input type="radio" name="cake" value="straw" /> Strawberry </li>
			<li><input type="radio" name="cake" value="lemon" /> Lemon </li>
			<li><input type="radio" name="cake" value="lie" /> The Cake is a Lie </li>
		</ul>
		<center><input type="submit" value="vote" /></center>
		</form>
	</div> <!-- close poll -->

	<div id="planet" class="box">
		<h2><a href="http://planet-wars.eu/">Planet Wars!</a></h2>
	</div> <!-- close planet -->
	
	<div id="unit" class="box"> <!-- unit of the day. changes once a day. to a different unit. --> 
		<span class="lefty"><b>The <a href="">PYRO</a></b></span>
		<span class="righty"><a href=""><b>&lt;&lt;</b></a> | <a href=""><b>?</b></a> | <a href=""><b>&gt;&gt;</b></a></span>
		<br />
		<p><b>Armed with a deadly flamethrower and jumpjets, this versatile bot is dangerous in any situation. Able to leap small chasms and up steep walls, the pyro is perfect for surprise attacks at the rear of the enemy base, where it is lightly defended.</b></p>
	</div> <!-- close unit -->

	<div id="commits" class="box"> <!--Like Revision Ninja, except for the website?-->
		<h2><a href="http://trac.caspring.org/timeline">Recent Development</a></h2>
		added awesome feature "x" <br />
		added awesome feature "x" <br />
		added awesome feature "x" <br />
		added awesome feature "x" <br />
	</div> <!--close commits-->


	<div id="top10" class="box"> <!--Ideally pulled from the top 50 players. Perhaps could even scroll through all of them, making top [insert number] bold?-->
		<h2><a href="http://springie.licho.eu/top50.php">Top Players</a></h2>
    
		<table>
		<tr><th width="30" align="left">#</th> <th width="200" align="left">Name</th> <th>ELO</th></tr>
		<?php
    require_once("../../licho/web/springie/globals.php");
    $r = mysql_query("SELECT name, elo, w FROM Players WHERE lastSeen>".(time()-24*3600*31)." ORDER BY elo DESC LIMIT 0,10");
    $cnt = 1;
    while ($row = mysql_fetch_array($r)) {
      printf("<tr><td>$cnt.</td><td>$row[name]</td><td>%d</td></tr>\n", $row[elo]);
      $cnt++;
    } 
    ?>
		</table>
	</div> <!--close top10 -->
</div> <!-- close rlist -->

<div id="intro" class="mid">
<!--  -->
	<br /><br /><br />
	<center> <object width="425" height="344"><param name="movie" value="http://www.youtube.com/v/870o3gPUku4&hl=en&fs=1&color1=0x3a3a3a&color2=0x999999"></param><param name="allowFullScreen" value="true"></param><param name="allowscriptaccess" value="always"></param><embed src="http://www.youtube.com/v/870o3gPUku4&hl=en&fs=1&color1=0x3a3a3a&color2=0x999999" type="application/x-shockwave-flash" allowscriptaccess="always" allowfullscreen="true" width="425" height="344"></embed></object> </center>
<!---->
	<p>Zero-K is a FREE, multiplatform, open-source RTS game, which aims to be a dynamic, action-packed, hassle-free game, full of clever strategies and constantly moving combat which usually lasts on average 20-30 minutes.</p>
	<p>Some of the more prominent features:</p>
	<ul class="points">
		<li><b>Epic Scale</b> from tiny fleas to huge mechs and gigantic superweapons that wreak havoc - hundreds or thousands of units on the battlefield, all easily viewable with a fully zoomable camera.</li>
		<li><b>Realistic Physics</b> mean each shot is physically simulated realtime - you can actually evade bullets if you micro-manage your units! Hills and terrain affect line of sight and radar coverage, explosions create deformations, etc...</li>
		<!-- <li><b>Natural Balance</b> allows units to depend on their natural characteristics and the benefits of the simulated environment, not contrived special damages to determine their effectiveness.</li> -->
		<li><b>Terraforming</b> - change the terrain: walls, ditches, ramps and more, to provide yourself with an extra tactical advantage.</li>
		<li><b>Unique Abilities</b> make sure units are fun to use! From jump-jets, gravity turrets, mobile shields, burning napalm, air drops, unit morphs - we've got it all and more! You can even control your units directly in First Person Mode!<!--when the wiki/guides get put up these could use some links--></li>
		<li><b>Streamlined Economy</b> - since resources are unlimited, your economy expands gracefully as the game progresses and without boring economic micro-management, allowing you to focus on more important tasks at hand.</li>
		<li><b>Planet Wars</b> lets you take control of your own planet and fight for survival in an ongoing online campaign! <a href="http://planet-wars.eu/">Grab your planet today!</a></li>
	</ul>
	<p>There are a lot more amazing things to tell, but listing them out is boring, try them out by visiting our <a href="download.php">downloads</a> page.</p>
	<p>Zero-K is made by players, for players. All the game developers are active participants in epic wars between robots (not to mention <a href="">chickens</a>). Balance and fun is assured.</p>
</div> <!-- close intro -->

<div id="news" class="mid">
	<h1>News</h1><span class="righty"><img src="http://upload.wikimedia.org/wikipedia/commons/thumb/4/43/Feed-icon.svg/16px-Feed-icon.svg.png" alt="RSS"></img></span> <!--I have no idea how to do rss-->
	<div class="newslette">
		<h1>New Website!</h1>
		<p>The new website for Zero-K players is finally here!</p>
		<p>We're really excited to present this brand-new website for this awesome game. I hope the experienced players enjoy the new layout and look. There is more to come! The main page will be a portal to all of your Zero-K needs; very soon we will start up a "Unit of the Day" which helpfully describes a unit from a large and various collection in the game. Those curious of the new and ongoing development will have easy access to the revision logs, as well as an up to date list of our top players.</p>
		<p>Since Zero-K is a community project, we would gladly welcome any help or feedback. If you can't model, or can't code, there are still ways for you to help! The wiki can be filled with helpful information, guides can be written to help newcomers, text can be translated into your native language.</p>
		<br /><b>09/10/09</b> - maackey
		
	</div> <!-- close newslette -->
</div> <!-- close news -->

<?include("footer.inc");?>

<!-- lulz... look at the award I got! :P
<p>
<a href="http://validator.w3.org/check?uri=referer"><img src="http://www.w3.org/Icons/valid-xhtml10-blue" alt="Valid XHTML 1.0 Transitional" height="31" width="88" /></a>
</p>
-->

