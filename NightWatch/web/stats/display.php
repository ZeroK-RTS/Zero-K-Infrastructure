<?
	$stamp = $_GET["stamp"];
	$ext = $_GET["ext"];
	if (!isset($stamp)) $stamp=time();
	$fname = date("Y-m-d-", $stamp);
	$today = date("Y-m-d-", time());
	if ($fname != $today && file_exists($fname.$ext.".png")) {
		header('Content-type: image/png');	
		readfile($fname.$ext.".png");
		return;
	}
	for ($i = 0; $i < 24; ++$i) {
		if ($i<10) $fn = $fname."0".$i.".$ext"; else $fn = $fname.$i.".$ext";
		if (file_exists($fn)) {
			$f = fopen($fn,"r");
			$t = fread($f, filesize($fn));
			fclose($f);
			
			$lines = split("\n", $t);
			for ($j =0; $j < count($lines); $j++) {
				$args = split("\|", $lines[$j]);
				$mod = $args[0];
				if (strstr($mod, "Complete Annihilation")!="") $mod = "Complete Annihilation";
				else if (strstr($mod, "Balanced Annihilation") != "") $mod = "Balanced Annihilation";
				else if (strstr($mod, "1944") != "") $mod = "S:1944";
				else if (strstr($mod, "XTA") != "") $mod = "XTA";
				else if (strstr($mod, "Gundam") != "") $mod = "Gundam";
				else if (strstr($mod, "Evolution RTS") != "") $mod = "Evolution RTS";
				else if (strstr($mod, "NOTA") != "") $mod = "NOTA";
				else if (strstr($mod, "Kernel Panic") != "") $mod = "Kernel Panic";
				else if (strstr($mod, "Tired Annihilation") != "") $mod = "Tired Annihilation";
				else if (strstr($mod, "Expand and Exterminate") != "") $mod = "Expand and Exterminate";
				else if (strstr($mod, "LLTA") != "") $mod = "LLTA";
				else if (strstr($mod, "Absolute Annihilation") != "") $mod = "Absolute Annihilation";
				else if (strstr($mod, "Star Wars") != "") $mod = "Star Wars";
				else if (strstr($mod, "COM Shooter") != "") $mod = "COM Shooter";
				else if (strstr($mod, "KuroTA") != "") $mod = "KuroTA";
				else if (strstr($mod, "Final Frontier") != "") $mod = "Final Frontier";
				else if (strstr($mod, "Fibre") != "") $mod = "Fibre";
				else if (strstr($mod, "Epic Legions") != "") $mod = "Epic Legions";
				else if (strstr($mod, "Ultimate Annihilation") != "") $mod = "Ultimate Annihilation";

				$cnt = $args[1];
				
				$a = intval($sums["$mod"]);
				$b = intval($cnt);
				$c = $a+$b;
				$sums["$mod"] = intval($c);
			}
		}
	}

require("piegraph.class.php");

if (count($sums) <=0) exit("");

$cnt=0;
foreach ($sums as $modname => $count) {
		if ($count > 0 && $modname!="") {
		$vals[$cnt] = $count;
		$vars[$cnt] = $modname."($count)";
		$cnt++;
		}
}
$pie = new PieGraph(400, 400, $vals);
$pie->setLegends($vars);
$pie->set3dHeight(15);
if ($fname != $today) $pie->display($fname.$ext.".png");
else $pie->display();


?>
