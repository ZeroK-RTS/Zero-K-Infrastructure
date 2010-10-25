<?php
$screenshots = scandir('img/screenshots');

$count = count($screenshots);

for($i=0; $i<$count; $i++)
{
	$file = array_shift($screenshots);
	if( strtolower( substr($file, -4) ) == '.jpg' )
	{
		array_push($screenshots, $file);
	}
}
$screenshot = $screenshots[array_rand($screenshots)];
//fixed backgrounds are choppy for firefox at the moment
echo '<style type="text/css" > html { background: #000 url("img/screenshots/'.$screenshot.'") no-repeat scroll 50% 0; }</style>';

?>
