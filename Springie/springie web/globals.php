<?
if (!@mysql_connect("localhost", "springie", "s12321")) exit("FAILED 0");
if (!@mysql_select_db("springie")) exit("FAILED 0");


	function updatePlayer($name, $ip, $rank) {
		if ($rank == "") $rank = 0;
		if ($name != "") {
			$res = mysql_query ("SELECT id FROM Players WHERE name='$name'");
			if (mysql_num_rows($res) > 0) {
				$id = mysql_result($res,0,0);
				if ($rank > 0) mysql_query("UPDATE Players SET rank=$rank, lastSeen = ".time()." WHERE id = $id"); 
				else mysql_query("UPDATE Players SET lastSeen = ".time()." WHERE id = $id");
			} else {
				mysql_query("INSERT INTO Players (name, lastSeen, rank) VALUES ('$name', ".time().", $rank)");
				$id = mysql_insert_id();
			}
			if ($ip!="" && $ip != "255.255.255.255") {
				if ($ip == -1) $ip = ip2long($_SERVER['REMOTE_ADDR']); else $ip = ip2long($ip);
				mysql_query("REPLACE INTO Players2ip (playerId, ip) VALUES ($id, $ip)");
			}
			return $id;		
		} else return 0;
	}
	
	
	function validate() {
		$u = $_SERVER['QUERY_STRING'];
		$b = explode("&hash=", $u);
		$res = mysql_query("SELECT password FROM Autohosts AS a JOIN Players AS p ON a.playerId = p.id WHERE p.name = '$_GET[login]'");

		  $fh = fopen("/tmp/iiiii", 'w');
		  fwrite($fh, $u);
		  fwrite($fh, "\n");
		  fwrite($fh, $_GET[login]);
		  fwrite($fh, "\n");
		  fwrite($fh, $_GET[hash]);
		  fwrite($fh, "\n");

		if (mysql_num_rows($res)>0) {
			$db_pas = mysql_result($res,0,0);
			if ($_GET[hash] == md5(urldecode($b[0]).$db_pas)) return 1;

		} else return 0;
		return 0;
	}
	
function makeFilter($nam, $param) {
	$pole = explode(" ", $param);
	$res = "";
	for ($i = 0; $i < count($pole); $i++) {
		if ($i != 0) $res .= " AND";
		$res.= " $nam LIKE '%".$pole[$i]."%'";
	}
	return $res;
}

function filterPlayers($p) {
	$res = mysql_query ("SELECT id FROM Players WHERE name='$p'");
	if (mysql_num_rows($res) > 0) {
		$id = mysql_result($res,0,0);
		$ret[] = $id;
		return $ret;
	}
	$res = mysql_query ("SELECT id FROM Players WHERE ".makeFilter("name", $p));
	if (mysql_num_rows($res) >0) {
		while (($row=mysql_fetch_row($res))) {
			$ret[] = $row[0];
		}
	} 
	return $ret;
}


function glue($ar, $start) {
	$ret = "";
	for ($i =$start; $i<count($ar); ++$i) {
		$ret.= $ar[$i];
	}
	return $ret;
}


function timeDiff($t) 
{
	$t = floor($t / 60);
	$v = $t % 60;
	$ret = $v."m";
	$t = floor($t/60);
	$v = $t % 24;
	if ($v >0) $ret = $v."h ".$ret;
    $t = floor($t/24);
    if ($t > 0) $ret = $t."d ".$ret;
    return $ret;
}

function getPlayerName($playerId)
  {
  $q = "SELECT name FROM Players WHERE `id`=$playerId";
  if (!$r = mysql_query($q)) exit (mysql_error() . "<br/>On line " . __LINE__);
  return mysql_result($r,0,0);
  }
function getModName($modId)
  {
  $q = "SELECT name FROM Mods WHERE `id`=$modId";
  if (!$r = mysql_query($q)) exit (mysql_error() . "<br/>On line " . __LINE__);
  return mysql_result($r,0,0);
  }
function getMapName($mapId)
  {
  $q = "SELECT name FROM Maps WHERE `id`=$mapId";
  if (!$r = mysql_query($q)) exit (mysql_error() . "<br/>On line " . __LINE__);
  return mysql_result($r,0,0);
  }

	function getModId($name) {
		$res= mysql_query("SELECT id FROM Mods WHERE name='$name'");
		if (mysql_num_rows($res)>0) {
			return mysql_result($res,0,0);
		} else {
			mysql_query("INSERT INTO Mods (name) VALUES ('$name')");
			return mysql_insert_id();
		}
	}
	
	function getMapId($name) {
		$res= mysql_query("SELECT id FROM Maps WHERE name='$name'");
		if (mysql_num_rows($res)>0) {
			return mysql_result($res,0,0);
		} else {
			mysql_query("INSERT INTO Maps (name) VALUES ('$name')");
			return mysql_insert_id();
		}
	}
	function getPlayerId($name) {
		$res= mysql_query("SELECT id FROM Players WHERE name='$name'");
		if (mysql_num_rows($res)>0) {
			return mysql_result($res,0,0);
		} else {
			return -1;
		}
	}

function getPlayerElo($playerId)
{
  $q = "SELECT elo FROM Players WHERE `id`=$playerId";
  if (!$r = mysql_query($q)) exit (mysql_error() . "<br/>On line " . __LINE__);
  return mysql_result($r,0,0);
}

function getPlayerReliability($playerId)
{
  $q = "SELECT w FROM Players WHERE `id`=$playerId";
  if (!$r = mysql_query($q)) exit (mysql_error() . "<br/>On line " . __LINE__);
  return round((mysql_result($r,0,0)-1) * 50);
}

/*	
function calculateBattleElo($bid) {
	if (mysql_result(mysql_query("SELECT count(distinct allyNumber) FROM Games2players WHERE gameId = $bid AND spectator=0"),0,0) != 2) return; // needs 2 teams
	
	$r = mysql_query("SELECT avg(elo), count(*) FROM Players WHERE id IN (SELECT playerId FROM Games2players WHERE gameId=$bid AND spectator=0 AND victoryTeam=0)");
	$loserElo = mysql_result($r, 0, 0);
	$loserCount = mysql_result($r, 0,1);

	
	$r = mysql_query("SELECT avg(elo), count(*) FROM Players WHERE id IN (SELECT playerId FROM Games2players WHERE gameId=$bid AND spectator=0 AND victoryTeam=1)");
	$winnerElo = mysql_result($r, 0, 0);
	$winnerCount = mysql_result($r, 0,1);
		
    $eWin = 1 / (1 + pow(10, ($loserElo - $winnerElo) / 400));
    $eLose = 1 / (1 + pow(10, ($winnerElo - $loserElo) / 400));
	
    $scoreWin = 32 * (1 - $eWin) / $winnerCount;
    $scoreLose = 32 * (0 - $eLose) / $loserCount;

	mysql_query("UPDATE Players SET elo = elo + $scoreLose WHERE id IN (SELECT playerId FROM Games2players WHERE gameId=$bid AND spectator=0 AND victoryTeam=0)");
	mysql_query("UPDATE Players SET elo = elo + $scoreWin WHERE id IN (SELECT playerId FROM Games2players WHERE gameId=$bid AND spectator=0 AND victoryTeam=1)");
}
*/
$w_max = 6;


function calculateBattleElo($bid) {
	//if (mysql_result(mysql_query("SELECT count(distinct allyNumber) FROM Games2players WHERE gameId = $bid AND spectator=0"),0,0) != 2) return; // needs 2 teams
	global $w_max;
	$w_learn_factor = 20; // games to get full w
	
	$q = mysql_query("SELECT id, elo, w, victoryTeam FROM Players p JOIN Games2players g ON p.id = g.playerId AND gameId = $bid AND spectator = 0");

	
	$winnerW = 0;
	$loserW = 0;
	$winnerInvW = 0;
	$loserInvW = 0;
	
	$winnerElo = 1;
	$loserElo =  1;
	$winnerCount = 0;
	$loserCount = 0;
	
	while ($r = mysql_fetch_row($q)) {
		$victory = $r[3];
		$w = $r[2];
		$id = $r[0];
		$elo = $r[1];
		$inv_w = $w_max + 1 - $w;
		
		if ($victory) {
			$winner[$winnerCount] = array($id, $w, $inv_w);
			$winnerW += $w;
			$winnerInvW += $inv_w;
			$winnerElo +=  $w * $elo;
			$winnerCount++;
		} else {
			$loser[$loserCount] = array($id, $w, $inv_w);
			$loserW += $w;
			$loserInvW += $inv_w;
			$loserElo += $w * $elo;
			$loserCount++;
		}
	}
	
	$sumCount = $winnerCount + $loserCount;
	
	if ($winnerW == 0 || $loserW == 0) {
		echo "\nWarning no winners or loser battle $bid\n";
		return;
	}
	
	
	//$winnerElo = $winnerElo / $winnerCount;
	//$loserElo = $loserElo / $loserCount;

	$winnerElo = $winnerElo / $winnerW;
	$loserElo = $loserElo / $loserW;

	
    $eWin = 1 / (1 + pow(10, ($loserElo - $winnerElo) / 400));
    $eLose = 1 / (1 + pow(10, ($winnerElo - $loserElo) / 400));
	
    $scoreWin = sqrt($sumCount/2) * 32 * (1 - $eWin) / $winnerInvW;
    $scoreLose = sqrt($sumCount/2) * 32 * (0 - $eLose) / $loserInvW;
	
	$sumW = $winnerW + $loserW;
	$sumCount = $winnerCount + $loserCount;
	
	foreach ($winner as $p) {
		$id = $p[0];
		$w = $p[1];
		$inv_w = $p[2];
		
		if ($w < $w_max) {
			$w = $w + (($sumW - $w) / ($sumCount - 1)) / $w_learn_factor;
			if ($w > $w_max) $w = $w_max;
		}

		$elo = $scoreWin * $inv_w;
		
		mysql_query("UPDATE Players SET w = $w, elo = elo + $elo WHERE id = $id");
	}

	foreach ($loser as $p) {
		$id = $p[0];
		$w = $p[1];
		$inv_w = $p[2];
		
		if ($w < $w_max) {
			$w = $w + (($sumW - $w) / ($sumCount - 1)) / $w_learn_factor;
			if ($w > $w_max) $w = $w_max;
		}

		$elo = $scoreLose * $inv_w;
		
		mysql_query("UPDATE Players SET w = $w, elo = elo + $elo WHERE id = $id");
	}
}


?>