<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" xml:lang="en" lang="en">
<head>
	<title>Zero-K - Poll</title>
<?php include("inc_head.inc"); ?>
</head>

<body>
	<div id="wrapper">
<!-------------------------------------------------------------- -->
<?php include("inc_menu.inc"); ?>
	<?php
		$option = $_GET["option"];
		if($option!="Wrong") echo "<h1>You chose ".$option."!!</h1> <p>congratulations.";
		else echo "yes. yes it is.";
		echo "<h2>A real poll coming soon to a website near you!</h2>";
	?>
<!-------------------------------------------------------------- -->
	</div><!close wrapper>
</body>
</html>
