<html>
  <head>
    <title>Get a torrent for download</title>
  </head>
  <body>
		Search for map and mod torrents uploaded via <a href='http://www.licho.eu/CaDownloader.exe'>CaDownloader</a>
		<b>WARNING YOU NOW NEED PROPER CLIENT TO DOWNLOAD, NORMAL TORRENT CLIENTS WONT WORK. Use CaDownloader or SpringLobby or any other client of this system</b>
		<br/>
		<br/>
    <form action="search.php" method="get">
      <input type="text" name="q" value="<?=$_GET["q"]?>">
      <input type="submit" value="Search">
    </form>
  </body>
</html>

<?
	$q = $_GET["q"];
	if ($q == "") exit();
	$fname = "torrents.txt";
	$f = fopen($fname,"r+");
	@$t = fread($f, filesize($fname));
	fclose($f);
	$lines = split("\n", $t);

	$qw = split(" ",strtolower($q));
	
	for ($i=0;$i<count($lines);$i++) {
		$lar = split("\|",$lines[$i]);
		$line_down = strtolower($lar[0]);
		$found = true;
		for ($j=0; $j<count($qw);$j++) {
			$word = strtolower($qw[$j]);
			if ($word!="" && strstr($line_down,$word) == "") {
				$found=false;
				break;
			}
		}
		if ($found) {
			echo "<a href='torrents/$lar[1].torrent'>$lar[0]</a><br/>";
		}
	}
?>