<?
require_once("globals.php");

$p = $_GET[p];
$l = $_GET[login];




if ($_GET[elorec]=="1") {
	mysql_query("UPDATE Players SET elo=1500, w=1");
	$r = mysql_query("SELECT gameId FROM Games2players WHERE spectator=0  GROUP BY gameId HAVING count(distinct allyNumber)>=2  ORDER BY gameId");
	while($row=mysql_fetch_array($r)) {
		calculateBattleElo($row[0]);
		echo $row[0]." \n";
	}		
}

$getelo=$_GET[getelo];
if (isset($getelo)){
	$r = mysql_query("SELECT elo FROM Players WHERE name='$getelo'");
	if (mysql_num_rows($r)==0) echo "1500";
	else echo mysql_result($r,0,0);
	exit;
}


$welo=$_GET[welo];
if (isset($welo)){
	global $w_max;
	$r = mysql_query("SELECT elo,w FROM Players WHERE name='$welo'");
	if (mysql_num_rows($r)==0) echo "1500|0";
	else echo mysql_result($r,0,0)."|".mysql_result($r,0,1);
	exit;
}


function eloToText($pid)  {
  $r = getPlayerReliability($pid);
  if ($r < 50) return "unknown";
  $el = getPlayerElo($pid);
  if ($el >= 1450 && $el<=1550) return "average";
  else if ($el<=1450 && $el>=1350) return "below average";
  else if ($el>=1550 && $el<=1650) return "above average";
  else if ($el<=1350) return "poor";
  else if ($el>=1650 && $el<=1900) return "good";
  else if ($el>=1900) return "godlike";
}





function displayStats($pid) {
	if ($pid == 0) return;
	$ret = "";
	
	$mod = getModId($_GET[mod]);
	$map = getMapId($_GET[map]);
	
	$total = mysql_result(mysql_query("SELECT count(*) FROM Games2players WHERE playerId = $pid AND spectator=0"),0,0);
	
	$wins = mysql_result(mysql_query("SELECT count(*) FROM Games2players WHERE playerId = $pid AND spectator=0 AND victoryTeam=1 AND aliveTillEnd=1"),0,0);
		
	$quit = mysql_result(mysql_query("SELECT count(*) FROM Games2players WHERE playerId = $pid AND spectator=0 AND (dropTime<>0 OR leaveTime<>0) AND (dropTime < loseTime OR leaveTime < loseTime)"),0,0);
		
	$maptime= mysql_result(mysql_query("SELECT sum(duration) FROM Games WHERE mapId = $map AND id IN (SELECT distinct gameId FROM Games2players WHERE playerId=$pid)"),0,0);
	
	$modtime= mysql_result(mysql_query("SELECT sum(duration) FROM Games WHERE modId = $mod AND id IN (SELECT distinct gameId FROM Games2players WHERE playerId=$pid)"),0,0);
	
	$side= @mysql_result(mysql_query("SELECT side,count(*) AS cnt FROM Games2players WHERE playerId=$pid AND spectator=0 GROUP BY side ORDER BY cnt DESC LIMIT 0,1"),0,0);
	
	@$ret = "total:$total games, skill rating:".eloToText($pid)." (".round(getPlayerElo($pid))."), won:".round(100*$wins/$total)."%, left:".round(100*$quit/$total)."%, maptime:".timeDiff($maptime).", modtime:".timeDiff($modtime).", side:$side\n";
	return $ret;
	
}

if (!validate()) {
	exit("FAILED 2 auth failed - delete do_not_delete_me.xml in springie folder and restart!");
}


$id = updatePlayer($l, "", 0);

if ($p == "" || $p == "help") echo "RESPOND\n* !stats games, !stats maps, !stats player <name> *\n";

$args = explode(" ", $p);

switch ($args[0]) {
	case "games":
	
		$res = mysql_query("SELECT sum(duration) AS totalTime,count(*) AS cnt,avg(players) AS avgplayers FROM Games WHERE playerId = $id");
		@$row=mysql_fetch_array($res);

		echo "== This Springie - $l ==\n";
		echo "Total games: $row[cnt]\n";
		echo "Total in-game time: ".timeDiff($row[totalTime])."\n";
		echo "Average player count in game: ".round($row[avgplayers])."\n";
		
		$res = mysql_query("SELECT sum(duration) AS totalTime,count(*) AS cnt,avg(players) AS avgplayers FROM Games");
		@$row=mysql_fetch_array($res);

		echo "== Global ==\n";
		echo "Total games: $row[cnt]\n";
		echo "Total in-game time: ".timeDiff($row[totalTime])."\n";
		echo "Average player count in game: ".round($row[avgplayers])."\n";
		
	break;
	
	case "leavers":
		echo file_get_contents("http://licho.iamacup.com/leavers.php?p=".urlencode($args[1]));
	break;
	
	
	
	case "maps":
		$res = mysql_query("SELECT name, count(*) as cnt, avg(duration) as avg, sum(duration) AS sum, avg(players) AS plr FROM Games JOIN Maps ON mapId = Maps.id WHERE playerId = $id GROUP BY mapId, playerId ORDER BY sum DESC LIMIT 0,20");

		echo "== Most popular maps on $l ==\n";
		$i = 1;
		while (($row=mysql_fetch_array($res))) {
			echo $i.". $row[name] ($row[cnt] games, for total ".timeDiff($row[sum])." - average game took ".timeDiff($row[avg])." and had ".round($row[plr])." players)\n";
			$i++;
		}
		
		$res = mysql_query("SELECT name, count(*) as cnt, avg(duration) as avg, sum(duration) AS sum, avg(players) AS plr FROM Games JOIN Maps ON mapId = Maps.id GROUP BY mapId ORDER BY sum DESC LIMIT 0,20");

		echo "== Globally most popular maps ==\n";
		$i = 1;
		while (($row=mysql_fetch_array($res))) {
			echo $i.". $row[name] ($row[cnt] games, for total ".timeDiff($row[sum])." - average game took ".timeDiff($row[avg])." and had ".round($row[plr])." players)\n";
			$i++;
		}
	break;
		
	case "player":
		if ($args[1] == "") $args[1] = $_GET[user];

		$ida = filterPlayers(glue($args, 1));
		$pid = $ida[0];
		if ($pid == 0) exit ("this player has no games in database yet\n");
		
			
		$total = mysql_result(mysql_query("SELECT count(*) FROM Games2players WHERE playerId = $pid AND spectator=0"),0,0);
		$wins = mysql_result(mysql_query("SELECT count(*) FROM Games2players WHERE playerId = $pid AND spectator=0 AND victoryTeam=1 AND aliveTillEnd=1"),0,0);
		
		$quit = mysql_result(mysql_query("SELECT count(*) FROM Games2players WHERE playerId = $pid AND spectator=0 AND (dropTime<>0 OR leaveTime<>0) AND (dropTime < loseTime OR leaveTime < loseTime)"),0,0);
		
		
		
		@print(getPlayerName($pid)." - skill rating:".eloToText($pid).", played $total games, has won ".round(100*$wins/$total)."% and quit ".round(100*$quit/$total)."%\n");
			
		$res = mysql_query("SELECT name, sum(duration) AS sum FROM Games JOIN Maps ON mapId = Maps.id AND Games.id IN (SELECT distinct gameId AS cnt FROM Games2players WHERE playerId=$pid) GROUP BY mapId ORDER BY sum DESC LIMIT 0,5 ");
		echo "most played MAPS are:\n";
		$i = 1;
		while (($row=mysql_fetch_array($res))) {
			echo $i.". $row[name] - ".timeDiff($row[sum])."\n";
			$i++;
		}

		$res = mysql_query("SELECT name, sum(duration) AS sum FROM Games JOIN Mods ON modId = Mods.id AND Games.id IN (SELECT distinct gameId AS cnt FROM Games2players WHERE playerId=$pid) GROUP BY modId ORDER BY sum DESC LIMIT 0,5 ");
		echo "most played MODS are:\n";
		$i = 1;
		while (($row=mysql_fetch_array($res))) {
			echo $i.". $row[name] - ".timeDiff($row[sum])."\n";
			$i++;
		}
		
		$res = mysql_query("SELECT count(*) AS cnt, side FROM Games2players WHERE playerId = $pid AND spectator = 0 GROUP BY side ORDER BY cnt DESC LIMIT 0,5");
		echo "most played SIDES are:\n";
		$i = 1;
		while (($row=mysql_fetch_array($res))) {
			echo $i.". $row[side] - $row[cnt] times \n";
			$i++;
		}
	
		
	break;
	
	case "":
		echo "--- Stats for current game ---\n";
		$res = mysql_query("SELECT sum(duration) AS totalTime, avg(duration) AS avgTime, count(*) AS cnt,avg(players) AS avgPlayers FROM Games WHERE modId = ".getModId($_GET[mod])." AND mapId = ".getMapId($_GET[map]));
		@$row=mysql_fetch_array($res);

		echo "Game on this map and mod usually lasts ".timeDiff($row[avgTime])." and is played by ".round($row[avgPlayers])." players \n";
		
		for ($i = 0; $i<count($_GET[users]); $i++) {
			$spl = explode("|", $_GET[users][$i]);
			echo $spl[0]." --> ".displayStats(getPlayerId($spl[0]))."\n";
		}
		
		exit();
		
		
	break;
	
	
	default:
		echo "RESPOND\nuse !stats games, !stats maps, !stats player <name>";
	break;
		
	
}


?>