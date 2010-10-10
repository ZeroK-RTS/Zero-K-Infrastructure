<?
require_once("globals.php");


$p = $_GET[p];

if (!validate()) {
	exit("FAILED 2 auth failed - delete do_not_delete_me.xml in springie folder and restart!");
}

echo "RESPOND\n";

if ($p == "" || $p=="HELP" ) echo ("* !smurfs <playername> *\n");


if ($p == "") {
	if (count($_GET[users])==0) $p = $_GET[user];
	else {
		echo "--- Smurfs of current game ---\n";
		for ($i = 0; $i<count($_GET[users]); $i++) {
			$spl = explode("|", $_GET[users][$i]);
			echo $spl[0]." --> ".displaySmurf(getPlayerId($spl[0]))."\n";
		}
		
		//echo "\nIf you like !smurfs please support it by voting here http://taspring.clan-sy.com/phpbb/viewtopic.php?t=8662\n";
		exit();
	}
} 


function displaySmurf($id) {
	$ret = "";
	$res = mysql_query("SELECT name, lastSeen, rank FROM Players AS p JOIN Players2ip AS i ON p.id = i.playerId WHERE ip IN (SELECT ip FROM Players2ip WHERE playerId=$id) GROUP BY playerId ORDER BY name");
	$cnt = 0;
	while (($row=mysql_fetch_array($res))) {
		if ($row[rank] == 0) $row[rank] = "?";
		if ($cnt > 0) $ret.= ", ";
		$ret.= $row[name]." (r:$row[rank], ".(timeDiff(time() - $row[lastSeen])).")";
		$cnt++;
	}
	return $ret;
}

$ida = filterPlayers($p);
$id = $ida[0];
if ($id == 0) exit ("No such player found in database.");

echo displaySmurf($id)."\n";
//echo "\nIf you like !smurfs please support it by voting here http://taspring.clan-sy.com/phpbb/viewtopic.php?t=8662\n";


?>