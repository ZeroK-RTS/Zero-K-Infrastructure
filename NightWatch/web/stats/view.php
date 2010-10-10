<?php
	$stamp = $_GET["stamp"];
	if (!isset($stamp)) $stamp=time();
	echo "<h2>Accurate player minutes stats (updated every minute)</h2>";
	echo "<b><a href='?stamp=".($stamp-604800)."'>back</a>&nbsp;&nbsp;&nbsp;&nbsp;<a href='?stamp=".($stamp+604800)."'>future</a></b><br/>";
	
	for ( $i = 0; $i < 7; $i++) {
		$t = $stamp - $i*86400;
		echo "<b>".date("j.n.Y", $t)."</b><br/><img src='display.php?stamp=$t&ext=mods'></img><br/><br/>"; //<img src='display.php?stamp=$t&ext=maps'></img>
	}
?>