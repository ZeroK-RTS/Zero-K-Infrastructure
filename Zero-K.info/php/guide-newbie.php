<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" xml:lang="en" lang="en">
<head>
	<title>Zero-K - Newbie Guide</title>
<?php include("inc_head.inc"); ?>
</head>

<?php include("inc_plainbg.inc"); ?>

<body id="body">
	<div id="wrapper">
<!-------------------------------------------------------------- -->
<?php include("inc_menu.inc"); ?>
<!-------------------------------------------------------------- -->
<?php
include("inc_guide_newbie.inc");
/*
$data = file_get_contents("http://trac.caspring.org/wiki/Terraforming");
$start = stripos($data, "<hr />");
$html = substr($data, $start, strlen($data));
$start = stripos($html, "<p>");
$end = stripos($html, "<h3>");
$html = substr($html, 0, $end);
$html = substr($html, $start, strlen($html));
echo $html;
/**/
?>
<!-------------------------------------------------------------- -->
<?php include("inc_footer.inc"); ?>
<!-------------------------------------------------------------- -->
	</div><!close wrapper>
</body>
</html>