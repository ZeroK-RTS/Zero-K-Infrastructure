<html>
<head>
	<title>Spring Default Keyboard Layout</title>
	<link rel="icon" href="img/favicon.png" />
	<style type="text/css">
	body {background: #eee;}
	div#wrapper
	{
		width: 55em;
		padding: 1em;
		padding-top: 0;
		padding-bottom: 0;
		margin: 0;
		margin-left: auto;
		margin-right: auto;
	}
	table
	{
		background: #666;
		padding: 1em;
		margin: 1em;
		margin-top: 0;
		border: 3px solid #222;
		-moz-border-radius: 5px;
		-webkit-border-radius: 5px;
		border-radius: 5px;
	}
	td
	{
		background: #222;
		color: #eee;
		width: 3em;
		height: 2em;
		border: 2px solid #000;
		-moz-border-radius: 5px;
		-webkit-border-radius: 5px;
		border-radius: 5px;
		
		-moz-box-shadow: 3px 3px 4px #000;
		-webkit-box-shadow: 3px 3px 4px #000;
		box-shadow: 3px 3px 4px #000;
	
		text-align: center;
	}
	td:hover
	{
		background: #111;
		-moz-box-shadow: 1px 1px 2px #222;
		-webkit-box-shadow: 1px 1px 2px #222;
		box-shadow: 1px 1px 2px #222;
	}
	div#info
	{
		background: #999;
		width: 15em;
		height: 10em;
		padding-left: 1em;
		padding-right: 1em;
		margin: 1em;
		border: 2px solid #222;
		overflow: auto;
	}
	div#credits
	{
		position: absolute;
		top: 0px;
		right: 0px;
		padding: 1em;
	}
	div#test
	{
		position: relative;
		background: #999;
		width: 30em;
		height: 10em;
		float: right;
		padding-left: 1em;
		padding-right: 1em;
		margin: 1em;
		border: 2px solid #222;
		overflow: auto;
	}
	</style>
	
	<script language="javascript">
	function clear() { document.getElementById("info").innerHTML = ""; }
	function display(hex, key, s, c, a, m, bind, desc, iter)
	{
		if(!iter) clear();
		document.getElementById("info").innerHTML += s+c+a+m+key+" - "+bind+"<br>";
	}
	</script>

	<?php
		echo "<script language=\"javascript\">\n\tfunction show(a)\n\t{\n\t\tswitch(a)\n\t\t{";
		echo "\n\t\t\tcase 0x000: display(0x000, 'null', '', '', '', '', 'undefined', '', 0); break;";
		$f = fopen("uikeys.txt", "r");
		
		$hex[] = "";
		$key[] = "";
		$shift[] = "";
		$alt[] = "";
		$ctrl[] = "";
		$meta[] = "";
		$i = 0;
		$start = false;
		
		while(!feof($f))
		{
			$line = fgets($f);
			if(preg_match("(Default Bindings:)", $line)) $start = true;
			else
			{
				if($start==false or $line=="//\n" or preg_match("(fakemeta)", $line)) continue;
				if($line=="\n") break;
				
				$keymod = preg_replace('/\s/', '',substr($line, 9, 20));
				//echo "$line";
				if(preg_match('/\bAny+\b/', $keymod))
				{
					$keymod = substr($keymod, 4, strlen($keymod));
				}
				if(preg_match('/\bAlt+\b/', $keymod))
				{
					$alt[$i]="Alt+";
					$keymod = substr($keymod, 4, strlen($keymod));
				}
				if(preg_match('/\bCtrl+\b/', $keymod))
				{
					$ctrl[$i]="Ctrl+";
					$keymod = substr($keymod, 5, strlen($keymod));
				}
				if(preg_match('/\bShift+\b/', $keymod))
				{
					$shift[$i]="Shift+";
					$keymod = substr($keymod, 6, strlen($keymod));
				}
				if(preg_match('/\bMeta+\b/', $keymod))
				{
					$meta[$i]="Meta+";
					$keymod = substr($keymod, 5, strlen($keymod));
				}
				$key[$i] = $keymod;
				$hex[$i] = toHex($key[$i]);
				$bind[$i] = substr($line, 29, -1);
				$fkey[$i] = toKey($key[$i]);
				$i++;
			}
		}
		fclose($f);
		
		for($i=0; $i<sizeof($key); $i++)
		{
			if($hex[$i]==$hex[$i-1]) $iter++;
			else $iter = 0;
			if($hex[$i]==$hex[$i+1]) $break = "";
			else $break = "break;";
			if($hex[$i]) echo "\n\t\t\tcase $hex[$i]: display($hex[$i], '$fkey[$i]', '$shift[$i]', '$ctrl[$i]', '$alt[$i]', '$meta[$i]', '$bind[$i]', '$desc[$i]', $iter); $break";
			else echo "\n\t\t\t//Error\n";
		}
		echo "\n\t\t\tcase 0x134: display(0x134, 'alt', '', '', '', '', 'alt', '', 0); break;";
		echo "\n\t\t\tcase 0x020: display(0x020, 'space', '', '', '', '', 'fakemeta', '', 0); break;";
		//echo "\n\t\t\tcase default: clear(); break;"; //why does this break everything?! :(
		echo "\n\t\t}\n\t}\n\t</script>\n";
	?>
	</head>
<body>
<div id="wrapper">
<?php
function toHex($key)
{
	if(!ctype_alnum($key))
	{
		if( $key=="~") return "0x060"; //"0x07E";
		else if( $key=="[") return "0x05B";
		else if( $key=="]") return "0x05D";
		else if( $key=="\\") return "0x05C";
		else if( $key==";") return "0x03B";
		else if( $key=="'") return "0x000"; //"0x027";
		else if( $key==",") return "0x02C";
		else if( $key==".") return "0x02E";
		else if( $key=="/") return "0x07F";
		else if( $key=="-") return "0x02D";
		else if( $key=="=") return "0x03D";
		else if( $key=="`") return "0x060";
		else if( $key=="+") return "0x02B";
		else if( $key=="numpad+") return "0x10E";
		else if( $key=="numpad-") return "0x10D";
		else return "0x000";
	}
	$f = fopen("uikeys.txt", "r");
	$start = false;
	while(!feof($f))
	{
		$line = fgets($f);
		if(preg_match("(Key Code)", $line))
		{
			fgets($f);
			$start = true;
		}
		else
		{
			if($start==false or $line=="//\n" or $line=="\n") continue;
			if(preg_match("/\b$key\b/i", $line)) return substr($line, -6, 5);
		}
	}
	fclose($f);
	return "0x000";
}

function toKey($hex)
{
	if(ctype_alnum($hex)) return $hex;
	else return "null";
	/*
	if( $hex=="0x07E") return "~";
	else if( $hex=="0x05B") return "[";
	else if( $hex=="0x05D") return "]";
	else if( $hex=="0x05C") return "\\";
	else if( $hex=="0x03B") return ";";
	else if( $hex=="0x027") return "'";
	else if( $hex=="0x02C") return ",";
	else if( $hex=="0x02E") return ".";
	else if( $hex=="0x07F") return "/";
	else if( $hex=="0x02D") return "-";
	else if( $hex=="0x03D") return "=";
	else if( $hex=="0x060") return "`";
	else if( $hex=="0x02B") return "+";
	else if( $hex=="0x10E") return "numpad+";
	else if( $hex=="0x10D") return "numpad-";
		
	$f = fopen("keycodes.txt", "r");
	while(!feof($f))
	{
		$code = fgets($f);
		if(preg_match("/\b$hex\b/i", $code)) return preg_replace('/\s/', '', substr($code, 0, 14));
	}
	fclose($f);
	return "null";
	*/
}
	
function getKeyBind($bind, $list=false)
{
	if($f = fopen("uikeys.txt", "r"))
	{
		$start = false;
		while(!feof($f))
		{
			$line = fgets($f);
			if(preg_match("(Default Bindings:)", $line))
			{
				$start = true;
			}
			else
			{
				if($start==false or $line=="//\n") continue;
				else if($line=="\n") break;
				else if($line=="//  fakemeta          space\n")
				{
					$binding = "fakemeta";
					$key = "space";
					$keymodwhole = "space";
					if($list) echo "$binding = $keymodwhole<br />\n";
					if(preg_match("/\b$bind\b/", $line)) return $key;
					continue;
				}
				$binding = substr($line, 29, -1);
				$keymodwhole = preg_replace("/\s/", '', substr($line, 9, 19));
				$keymod = $keymodwhole;
				if(preg_match('/\bAny+\b/', $keymod))
				{
					$keymod = substr($keymod, 4, strlen($keymod));
					$keymodwhole = $keymod;
				}
				if(preg_match('/\bAlt+\b/', $keymod))
				{
					//$alt[$i]="Alt+";
					$keymod = substr($keymod, 4, strlen($keymod));
				}
				if(preg_match('/\bCtrl+\b/', $keymod))
				{
					//$ctrl[$i]="Ctrl+";
					$keymod = substr($keymod, 5, strlen($keymod));
				}
				if(preg_match('/\bShift+\b/', $keymod))
				{
					//$shift[$i]="Shift+";
					$keymod = substr($keymod, 6, strlen($keymod));
				}
				if(preg_match('/\bMeta+\b/', $keymod))
				{
					//$meta[$i]="Meta+";
					$keymod = substr($keymod, 5, strlen($keymod));
				}
				$key = $keymod;
				if($list) echo "<a href='javascript:show(".toHex($key).")'>$binding</a> = $keymodwhole<br />\n";
				if(preg_match("/\b$bind\b/", $line)) return $key;
			}
		}
		fclose($f);
	}
	return false;
}

function keycode($val, $list=false)
{
	if($f = fopen("uikeys.txt", "r"))
	{
		$start = false;
		while(!feof($f))
		{
			$line = fgets($f);
			if(preg_match("(Key Code)", $line))
			{
				fgets($f); 
				$start = true;
			}
			else
			{
				if($start==false or $line=="//\n" or feof($f)) continue;
				if($list)
				{
					echo preg_replace("/\s/", '',substr($line, 2, strlen($line)-8));
					echo " = ";
					echo substr($line, strlen($line)-6, 5);
					echo "<br />";
				}
				
				if(preg_match("(\b$val\b)", $line)) return preg_replace("/\s/", '', substr($line, strlen($line)-6, 5));
				if(ctype_punct($val)) return "punctuation mark";
			}
		}
		fclose($f);
	}
	return "error";
}

function getDesc($bind)
{
	if($f = fopen("cmdesc.txt", "r"));
	{
		while(!feof($f))
		{
			$line = fgets($f);
			$desc = substr($line, $strlen($desc), strlen($line));
			if(preg_match("/\b$bind\b/", $line)) return $desc;
		}
	}
	fclose($f);
	return false;
}

//---------------------------------------------------------
echo "\n<h1>Spring Default Keyboard Layout</h1>\n<table><tr>\n";
	$layout1 = array(
		array(1=>"Esc", "", "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10", "F11", "F12", ""),
		array(1=>"~", "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "-", "=", "Backspace", ""),
		array(1=>"Tab", "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P", "[", "]", "\\", ""),
		array(1=>"Caps", "A", "S", "D", "F", "G", "H", "J", "K", "L", ";", "'", "Enter", "", ""),
		array(1=>"Shift", "Z", "X", "C", "V", "B", "N", "M", ",", ".", '/', "Shift", "", "", ""),
		array(1=>"Ctrl", "Meta", "Alt", "Space", "Alt", "???", "Ctrl", "blah"),
	);
	
	$layout2 = array(
		array(1=>"Esc", "", "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10", "F11", "F12", ""),
		array(1=>"~", "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "-", "=", "Backspace", ""),
		array(1=>"Tab", "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P", "[", "]", "\\", ""),
		array(1=>"Caps", "A", "S", "D", "F", "G", "H", "J", "K", "L", ";", "'", "Enter", "", ""),
		array(1=>"Shift", "Z", "X", "C", "V", "B", "N", "M", ",", ".", '/', "Shift", "", "", ""),
		array(1=>"Ctrl", "Fn", "Win", "Alt", "Space", "Alt", "???", "Ctrl", "", "", "", "", "", "", ""),
	);
	
	$keyboard = $layout1;
	
	for($j=0; $j<sizeof($keyboard); $j++)
	{
		$rowlength=sizeof($keyboard[0])+1;
		for($i=1; $i<$rowlength; $i++)
		{
			$colspan = '';
			$modifier = '';
			$key = $keyboard[$j][$i];
			$hex = toHex($key);
			if($key=="Shift") $modifier=" shift";
			if($key=="Alt") $modifier=" alt";
			if($key=="Ctrl") $modifier=" ctrl";
			if($key=="Meta") $modifier=" meta";
			
			if($key=="Backspace" or $key=="Tab" or $key=="Caps" or $key=="Shift" or $key=="Enter") 
			{
				$colspan = 'colspan="2"';
				$rowlength-=1;
			}
			else if($key=="Space") 
			{
				$colspan = 'colspan="6"';
				$rowlength-=5;
			}
			echo '<td id="'.$key.'" class="button'.$modifier.'" '.$colspan.' onmouseover="javascript:show('.$hex.')">'.$key.'</td>';
			echo "\n";
		}
		echo "</tr>\n<tr>\n";
	}
echo '</tr></table>';

/**/
echo "\n<div id=\"test\">";
$test = "!";
$val = getKeyBind($test, true);
echo "$test = $val";
echo "</div>";
/**/

?>
<div id="info">
Hover over a key to show its keybinds
</div>
<div id="credits">by: maackey</div>
</body>
</html>
