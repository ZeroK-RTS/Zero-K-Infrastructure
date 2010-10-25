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
		if($option!="Wrong") echo "<h1>You chose ".$option."!!</h1> <p>Assume the party escort submission position to recieve your cake";
		else echo "<h1>You're breaking my heart</h1>";
	?>
<!-------------------------------------------------------------- -->
	</div><!close wrapper>
</body>
</html>