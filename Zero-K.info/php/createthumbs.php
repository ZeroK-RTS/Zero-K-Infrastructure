<?php
include("SimpleImage.php");
$image = new SimpleImage();
$images = scandir('img/screenshots');
for ($i = 2; $i <= count($images); $i++)
{
	$picname = $images[$i];
	$picext = substr($images[$i], strlen($images[$i])-4);
	if ( $picext == ".png" or $picext == ".jpg" )
	{
		$image->load("img/screenshots/$picname");
		$image->resizeToWidth(250);
		$image->save("img/screenshots/thumb_$picname");
		//echo "<a href='img/screenshots/$picname'><img src='img/screenshots/thumb_$picname' /></img></a>\n";
	}
}
?>
