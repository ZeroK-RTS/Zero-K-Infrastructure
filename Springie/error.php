<?
	$file = fopen("./errors.txt", "aw");

	$data = "==========\r\n".date("j.n.Y  H:i:s")."\r\n";
	$data.= "From: ".$_SERVER["REMOTE_ADDR"]."\r\n";
	$data.= "User: ".$_GET["username"]."\r\n";
	$data.= "Springie: ".$_GET["springie"]."\r\n";
  $data.= $_GET["moreinfo"]."\r\n";
  $data.= $_GET["exception"]."\r\n\r\n";

	fwrite($file, $data);
?>