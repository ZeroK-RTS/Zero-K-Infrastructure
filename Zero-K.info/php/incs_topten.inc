<div id="topten" class="border">
	<h2><a>Top Players</a></h2>
	<table>
	<tr><th width="30" align="left">#</th> <th width="200" align="left">Name</th> <th>ELO</th></tr>
	<?php echo "coming (again) soon :)";
		/* did the location of globals.php change?
		require_once("../../licho/web/springie/globals.php");
		$r = mysql_query("SELECT name, elo, w FROM Players WHERE lastSeen>".(time()-24*3600*31)." ORDER BY elo DESC LIMIT 0,10");
		$cnt = 1;
		while ($row = mysql_fetch_array($r)) 
		{
			printf("<tr><td>$cnt.</td><td>$row[name]</td><td>%d</td></tr>\n", $row[elo]);
			$cnt++;
		}
		/**/
	?>
	</table>
</div><!close topten>
