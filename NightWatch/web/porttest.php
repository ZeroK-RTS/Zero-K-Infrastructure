<?php

$site = $_SERVER["REMOTE_ADDR"];
$port = $_GET["port"];

@$fp = fsockopen($site,$port,$errno,$errstr,5);
if(!$fp)
{
	echo "Cannot connect to that port. Try this: <a href='http://www.google.com/search?q=port+open+forward'>http://www.google.com/search?q=port+open+forward</a>";
}

else{
echo "Connect was successful - no errors on Port ".$port." at ".$site;
fclose($fp);
}

?>