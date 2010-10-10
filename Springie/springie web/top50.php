<html>
<head>
<title>
Top 50
</title>
</head>

<body>
<h2>Top 50 players seen playing past month</h2>
<?php 
require_once("globals.php");



$r = mysql_query("SELECT name, elo, w FROM Players WHERE w>2 AND lastSeen>".(time()-24*3600*14)." ORDER BY elo DESC LIMIT 0,50");
echo "<table border='0'><tr><th></th><th>Name</th><th>Rating</th><th>Reliability/weight (of rating)</th></tr>\n";
$cnt = 1;
while ($row = mysql_fetch_array($r)) {
	printf("<tr><td>$cnt.</td><td>$row[name]</td><td>%d</td><td>%.2f</td></tr>\n", $row[elo],$row[w]);
	$cnt++;
}
echo "</table>";



?>

</body>
</html>