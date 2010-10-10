<?php
require("globals.php");

$auth = $_GET['auth'];
if ($auth != 'supertajne') exit("invalid auth");

$from = $_GET['from'];
$to = $_GET['to'];

$fi = getPlayerId($from);
if ($fi <=0) exit("invalid from");

$ti = getPlayerId($to);

if ($ti <=0) exit("invalid to");

mysql_query("UPDATE Games2players SET playerId = $fi WHERE playerId = $ti");
mysql_query("DELETE FROM Players2ip WHERE playerId = $ti");
mysql_query("DELETE FROM Players WHERE id = $ti");

mysql_query("UPDATE Players SET name='$to' WHERE id = $fi");



?>
