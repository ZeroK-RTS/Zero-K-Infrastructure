<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" xml:lang="en" lang="en">
<head>
	<title>Zero-K - Pics</title>
<?php include("inc_head.inc"); ?>
</head>

<style type="text/css" > html { background: #888 url("") no-repeat scroll 50% 0; }</style>

<body>
	<div id="wrapper">
<!-------------------------------------------------------------- -->
<?php include("inc_menu.inc"); ?>
<!-------------------------------------------------------------- -->

<?php
$images = scandir('img/screenshots');
$thumbs = scandir('img/screenshots_thumb');
for ($i = 2; $i <= count($thumbs); $i++)
{
	$thext = substr($thumbs[$i], strlen($thumbs[$i])-4);
	if ( $thext == ".png" or $thext == ".jpg" )
	{
		for ($j = 2; $j <= count($images); $j++)
		{
			if ($thumbs[$i]==$images[$j])
			{
				echo "<a href='img/screenshots/$images[$j]'><img src='img/screenshots_thumb/$thumbs[$i]' class='' width='200' height='200'></img></a>\n";
				break;
			}
		}
	}
}
?>

<!-------------------------------------------------------------- -->
<?php include("inc_footer.inc"); ?>
<!-------------------------------------------------------------- -->
</div><!close wrapper>
</body>
</html>