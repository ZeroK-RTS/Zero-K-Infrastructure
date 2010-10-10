<?php 
require_once("globals.php");
$r = mysql_query("SELECT name, elo, w FROM Players WHERE lastSeen>".(time()-24*3600*14)." ORDER BY elo DESC LIMIT 0,10");
while ($row = mysql_fetch_array($r)) {
	printf("$row[name]\n", $row[elo]);
}
?>