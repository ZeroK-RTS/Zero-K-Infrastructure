<?
include('config.php');
?>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Strict//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd">
<html xmlns="http://www.w3.org/1999/xhtml" xml:lang="en" lang="en">

<head>
<title><? echo $title; ?></title>

<style type="text/css">
body {
	background-color: #2e3335;
	padding: 00px;
	margin-top: 0px;
	margin-right: 50px;
	margin-bottom: 50px;
	margin-left: 50px;
	font-family: Verdana, Arial, Helvetica, sans-serif;
}

h1
{
    margin-left: 50px;
}

.wdb_images
{
	margin-left: 35px;
}


.wdb_item
{
	color:#222222;
	width: 628px;
	margin-left: 20px;
	margin-right: 20px;
	margin-bottom: 30px;
	margin-top: 30px;
	padding: 0px;
	background-color: #434a4d;
	background-image:url(panel_bg.png);
	background-repeat:repeat-y;
    font-size: 16px;
}

.wdb_description a:link
{
   color: #e7ab0a;
   border: none;
}


.wdb_item form 
{
	display: inline; 
	font-size: 16px;
}

.wdb_item img
{
    border: 0px;
	padding: 00px;
	margin: 0px;
	margin-bottom: -3px;
    background-color: transparent;
}

.wdb_wname
{
margin-bottom: 10px;
margin-left: 20px;
font-size: 22px; 
}

.wdb_wname a:link { color: #e7ab0a; text-decoration: none; }
.wdb_wname a:visited { color: #e7ab0a; text-decoration: none; }
.wdb_wname a:focus { color: #e7ab0a; text-decoration: none; }
.wdb_wname a:hover { color: #e7ab0a; text-decoration: none; }
.wdb_wname a:active { color: #e7ab0a; text-decoration: none; }



.wdb_toolbar
{
	margin: 10px;
	margin-top: 20px;
}

.wdb_description
{
	text-align: justify;
	background-image:url(desc_bg.png);
	background-repeat: repeat-y;
	padding: 10px;
	margin-top: 3px;
	font-size:smaller;
}

.wdb_fieldLabelText
{
   font-weight : bold;
}

.wdb_sortline
{
	font-size: 16px;
	margin: 0px;
	margin-left: 20px;
}

.wdb_descHeader
{
	margin-top: 15px;
}

.wdb_content
{
     margin-left: 20px;
     margin-right: 20px;
     margin-bottom: 0px;
}

.wdb_content table
{
	margin-top: 10px;
}

</style>

</head>

<body>
<?php
//TODO:
//-Description-HTML-Sandbox
//-WidgetIcons
 
  function node2Array( $widget )
  {
  	$arr = array();
  	$arr['Id'] = $widget->attributes->getNamedItem("ID")->nodeValue;
  	
  	for( $k=0; $k < $widget->childNodes->length; $k++ )
		{
			$node = $widget->childNodes->item( $k );
			
			$arr[$node->nodeName] = $node->nodeValue;
		}
		return $arr;
  }  
  
  function DomDoc2Array( $doc, $sort )
  {
  	//get root element
		$root = $doc->getElementsByTagName("root")->item(0);
	
		for( $i=0; $i < $root->childNodes->length; $i++ )
		{
			$widget = $root->childNodes->item( $i );
			
			$arr = node2Array( $widget );
			
			if ( $sort == "n" )
			{
				$widgetArray[$arr['Name']] = $arr;
				ksort( $widgetArray, SORT_STRING );
			}
			else if ( $sort == "dl" )
			{
				$widgetArray[$arr['DownloadCount']] = $arr;
				krsort( $widgetArray, SORT_NUMERIC );
			}
			else if ( $sort == "dpd" )
			{
				$widgetArray[$arr['DownsPerDay']] = $arr;
				krsort( $widgetArray, SORT_NUMERIC );
			}
			else if ( $sort == "d" )
			{
				$widgetArray[$arr['Entry']] = $arr;
				krsort( $widgetArray);
			}
			else
			{
				die("Illegal option!");
			}
		}
		return $widgetArray;
  }
   
  $sort = "n"; //default sort by name
  if ( isset( $_GET['s'] ) )
  {
	  $sort = $_GET['s'];
  }
  
  $targetUrl = $serverUrl . "/lua_manager.php";
  //$targetUrl = $_SERVER["SERVER_NAME"] . $_SERVER["REQUEST_URI"];
	

	$doc = new DOMDocument();
	$doc->load($targetUrl . "?m=0"); //getOverview
	
	//echo "<img margin-left: 20px align=left width=80 src='SpringIconmkII.png' />";
	echo "<h1>" . $title . "</h1>";
	//iterate all widgets
	
	//Sorting Selector
	echo "<div class='wdb_item'>";
    echo "<img src='panel_top.png' />";
 	echo "<div class='wdb_sortline'>";
    echo "Sorting:";
    echo "<span class='wdb_sortline'>";
	
    echo "<form action=" . $_SERVER['PHP_SELF'] . " method='get'>";
    echo "<select name='s' size='1'>";
	echo "<option value='n' "; if ( $sort == "n" ) { echo "selected"; } echo ">Name</option>";
	echo "<option value='d' "; if ( $sort == "d" ) { echo "selected"; } echo ">Entry-Date</option>";
	echo "<option value='dl' "; if ( $sort == "dl" ) { echo "selected"; } echo ">Downloads</option>";
	echo "<option value='dpd' "; if ( $sort == "dpd" ) { echo "selected"; } echo ">Popularity</option>";
	echo "<input value='Refresh' type='submit'>";
    echo "</form>";
	echo "</span>";
	echo "</div>";
    echo "<img src='panel_bottom.png' />";
	echo "</div>";
	//END OF Sorting Selector
	
	$widgetArray = DomDoc2Array( $doc, $sort );	
	foreach ($widgetArray as $widget)
	{
		echo "\n";
		echo "<div class='wdb_item'>\n";
		echo "\t<img src='panel_top.png' />\n";
		echo "\t<div class='wdb_wname'><a href='" . $_SERVER['PHP_SELF'] . '#' . $widget["NameId"] . "' name='" . $widget["NameId"] . "'>" . $widget["Name"] . "</a></div>\n";
		echo "\t<img src='panel_sep.png' />\n";
		echo "\t<div class='wdb_content'>\n";
		
		echo "\t<table width='585' border='0'>\n";
		
		echo "\t\t<tr>\n";
		echo "\t\t\t<td class='wdb_tablabel' width='134'><span class='wdb_fieldLabelText'>Author:</span></td>\n";
		echo "\t\t\t<td width='441'>" . $widget["Author"] . "</td>\n";
		echo "\t\t</tr>\n";
		
		echo "\t\t<tr>\n";
		echo "\t\t\t<td class='wdb_tablabel'><span class='wdb_fieldLabelText'>Mods:</span></td>\n";
		echo "\t\t\t<td>" . $widget["Mods"] . "</td>\n";
		echo "\t\t</tr>\n";
		
		echo "\t\t<tr>\n";
		echo "\t\t\t<td class='wdb_tablabel'><span class='wdb_fieldLabelText'>Latest Version:</span></td>\n";
		echo "\t\t\t<td>" . $widget["Version"] . "</td>\n";
		echo "\t\t</tr>\n";
		
		echo "\t\t<tr>\n";
		echo "\t\t\t<td class='wdb_tablabel'><span class='wdb_fieldLabelText'>Downloads:</span></td>\n";
		echo "\t\t\t<td>" . $widget["DownloadCount"] . "&nbsp(" . number_format( $widget["DownsPerDay"], 2 ) . " D/d)" . "</td>\n";
		echo "\t\t</tr>\n";

		echo "\t\t<tr>\n";
		echo "\t\t\t<td class='wdb_tablabel'><span class='wdb_fieldLabelText'>Entry-Date:</span></td>\n";
		echo "\t\t\t<td>" . $widget["Entry"] . "</td>\n";
		echo "\t\t</tr>\n";
		
		echo "\t</table>\n";
		
		echo "\t<div class='wdb_descHeader'>\n";
		echo "\t<span class='wdb_fieldLabelText'>Description: </span>\n";
		echo "\t</div>\n";
		
		echo "\t<div class='wdb_description'>\n";
		echo $widget["Description"];
		echo "\t</div>\n";
		
		
		echo "\t<div class='wdb_toolbar'>\n";
		echo "\t\t<a href=\"" . $serverUrl . "/lua_manager.php?m=10&id=" . $widget['Id'] . "\" title='Download widget as ZIP file'><img src='download.png' /></a>\n";
		
		echo "\t\t<span class='wdb_images'>";
		$imgdoc = new DOMDocument();
		$imgdoc->load($targetUrl . "?m=4&id=" . $widget["NameId"]); 
		$imgroot = $imgdoc->getElementsByTagName("root")->item(0);
		for( $k=0; $k < $imgroot->childNodes->length; $k++ )
		{
			$img = $imgroot->childNodes->item( $k );
			$imgarr = node2Array( $img );
			
			echo "\t\t\t<a href=\"" . $imgarr["Url"] . "\" target='_blank' title='Show Image'><img src='img_icon.png'/></a>\n";
		}

		echo "\t\t</span>";
		
		echo "\t</div>\n";
		echo "\t</div>\n"; //end of content div
		echo "\t<img src='panel_bottom.png' />\n";
		echo "\t</div>\n";	//end of item div
		
//print files
/*		$filedoc = new DOMDocument();
		//echo "A: " . $targetUrl . "?m=1&id=" . $luaId;
		$filedoc->load($targetUrl . "?m=1&id=" . $luaId );
		$fileroot = $filedoc->getElementsByTagName("root")->item(0);
		for( $j=0; $j < $fileroot->childNodes->length; $j++ )
		{
			$f = $fileroot->childNodes->item( $j );
			$filearr = node2Array( $f );
			
			echo "<a href=\"" . $filearr["Url"] . "\">" . $filearr["LocalPath"] . "</a>";
			echo "<br/>";
		}
*/
	}

?>
</body>

</html>

