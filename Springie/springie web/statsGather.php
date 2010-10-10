<?
	require_once("globals.php");
		

	
	switch ($_GET['a']) {
		case "addplayer":
			if (!validate()) exit ("FAILED 2 auth failed");
			updatePlayer($_GET['name'], $_GET['ip'], $_GET['rank']);
		break;

		case "removeplayer":
			if (!validate()) exit ("FAILED 2 auth failed");
			updatePlayer($_GET['name'], $_GET['ip'], $_GET['rank']);		
		break;
		
		
		case "joinplayer": 
			if (!validate()) exit ("FAILED 2 auth failed");
			updatePlayer($_GET['name'], $_GET['ip'], $_GET['rank']);
		break;	
		
		case "register":
			$id = updatePlayer($_GET['name'], -1, 0);
			$res = mysql_query("SELECT playerId FROM Autohosts AS a JOIN Players AS p ON a.playerId = p.id WHERE p.name = '$_GET[name]' AND password<>''");
			if (mysql_num_rows($res) > 0) {
				exit ("FAILED 1 already registered");	
			} else {
				$pas = md5(time().microtime());
				mysql_query("REPLACE INTO Autohosts (playerId, password) VALUES ($id, '$pas')");
				echo $pas;
				exit;
			}
		break;
		
		
		// login, map, mod, title, start, duration
		// player[]
		// each player[] = param1| param2 | param3
		// param0 = name
		// param1 = ip
		// param2 = spectator
		// param3 = onVictoryTeam
		// param4 = aliveTillEnd
		// param5 = dropTime
		// param6 = leaveTime
		// param7 = side
		// param8 = loseTime (time when player lost)
		// param9 = allyNumber
		// param10 = rank
		case "battle":
			if (!validate()) exit("FAILED 2 auth failed");
			$id = updatePlayer($_GET['login'], -1, 0);
			$map = getMapId($_GET['map']);
			$mod = getModId($_GET['mod']);
			
			mysql_query("INSERT INTO Games (playerId, title, mapId, modId, start, duration, players) VALUES ($id, '$_GET[title]', $map, $mod, $_GET[start], $_GET[duration], ".count($_GET[player]).")");
			
			$battle_id = mysql_insert_id();
			
			$plcount = 0;
			
			for ($i = 0; $i < count($_GET[player]); ++$i) {
				$s = explode("|", $_GET['player'][$i]);
				$pid = updatePlayer($s[0], $s[1], $s[10]);
				
				if ($s[10] == 0 || $s[10] == "")  { // doesnt have rank set in data, check database for latest rank
					$s[10] = mysql_result(mysql_query("SELECT rank FROM Players WHERE id = $pid"),0,0);
				}
				
				mysql_query("REPLACE INTO Games2players (gameId, playerId, spectator, victoryTeam, aliveTillEnd, dropTime, leaveTime, side, loseTime, allyNumber, rank) VALUES ($battle_id, $pid, $s[2], $s[3], $s[4], $s[5], $s[6], '$s[7]', $s[8], $s[9], $s[10])");
				if ($s[2] == 0) $plcount++;
			}
	
			mysql_query("UPDATE Games SET players=$plcount WHERE id = $battle_id"); // player count excludes spectators
			
			calculateBattleElo($battle_id);
		break;
	}

?>