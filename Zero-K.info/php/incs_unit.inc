<?php
	$f = fopen("units.txt", "r");
	$i = 0;
	$unit = 2; //ideally changed from day to day; starts from 1, not 0
	$replace = '/\\\n/'; //gah these make no sense! at least it works
	while(!feof($f) and $i!=$unit)
	{
		$line = fgets($f);
		$i++;
	}
	$line = preg_replace($replace, "<p>", $line);
	fclose($f);
	
	$string = explode(" | ", $line);
	$unitname = $string[0];
	$unitpic = $string[1]; 
	$unitdesc = $string[2];
?>

<div id="unit" class="border relative text-left">
	<h2><a>Spotlight: </a></h2>
	<h3><?php echo $unitname; ?></h3>
	<!-- <span class="absright abstop bold button"> <a>&lt;&lt;</a> <a>?</a> <a>&gt;&gt;</a> </span> -->
	<img src="img/unitpics/<?php echo $unitpic; ?>" class="right"></img>
	<p><?php echo $unitdesc; ?>
</div><!close unit>
