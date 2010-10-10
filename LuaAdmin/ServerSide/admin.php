<?php

include('config.php');
include('dbConnection.php');
include('xmlDocument.php');
include('sanitizer.class.php');

$dbConnect = new dbConnection;
$xmlDoc = new xmlDocument;
$san = new HTML_Sanitizer;
	
$userId = $dbConnect->checkLogin( $_GET['lname'], $_GET['pwstr'] );

if ( !isset($_GET['pwstr'] ) || !isset($_GET['lname']) || ( $userId == false ) )
{
	die("away");
}

function mkdir_recursive($pathname, $mode)
{
    is_dir(dirname($pathname)) || mkdir_recursive(dirname($pathname), $mode);
    return is_dir($pathname) || @mkdir($pathname, $mode);
}

function fixDirName( $dir )
{
	if ( $dir[strlen($dir)-1] != '/' )
	{
		$dir = $dir . "/";
	}
	
	return $dir;
}

//e.g. from = "/files", to = "/files/myFiles/Images", deletes the folders images and myfiles if empty, path is always relative to work dir!
function rmdir_fromTo($from, $to )
{
	$to = trim($to, '/');
	$from = trim($from, '/');
	
	if ( (!is_dir($to)) || (!is_dir($from)) || ( strlen($to) <= strlen($from) ) )
	{
		return;
	}
	
	
	if ( rmdir( $to ) )
	{
		//try next level
		$boom = explode( "/" , $to );
		
		$oneUp = "";
		for( $i=0; $i < ( count($boom) - 1); $i++ ) //skip last element
		{
			$oneUp = $oneUp . "/" . $boom[$i];
		}
		
		rmdir_fromTo($from, $oneUp );
	}	
}

function deleteFilesByLuaId( $id )
{
	global $dbConnect;
	$dbresult = $dbConnect->getFilesByLua( $id );
	
	while($row = mysql_fetch_assoc($dbresult)) 
	{
		deleteFile( $row['Id'] );
	}
	
	rmdir("files/" . $id );
}

function deleteFile( $id )
{
	global $dbConnect;
	$dbresult = $dbConnect->getFile( $id );
	
	$file = mysql_fetch_assoc( $dbresult );
	
	$filePath = "files/" . $file['LuaId'] . "/" . $file['LocalPath'];
	unlink( $filePath );
	
	rmdir_fromTo( "files/", dirname($filePath) );
}

function deleteImage( $id )
{
	global $dbConnect;
	$dbresult = $dbConnect->getImage( $id );
	
	
	while( $row = mysql_fetch_assoc( $dbresult ) )
	{
		$filename = array_shift(explode('?', basename($row['Url'] )));	
		
		$fullpath = "images/" . $filename;
		unlink( $fullpath );
	}	
}

function deleteNameFiles( $nameId )
{
	global $dbConnect;
	$dbresult = $dbConnect->getLuasByNameId( $nameId );
	while($row = mysql_fetch_assoc($dbresult)) 
	{
		deleteFilesByLuaId( $row['Id'] );
	}
	
	$dbresult = $dbConnect->getImagesByNameId( $nameId );
	while($row = mysql_fetch_assoc($dbresult)) 
	{
		deleteImage( $row['Id'] );
	}
	
	//delete thumbnail
	unlink( "thumbnails/" . $nameId );
}

ini_set('display_errors', 1);
set_time_limit(3600); //Timeout limit 1 hour (could be a big file or slow con)

if ( $_GET['m'] == NULL )
{
		die("No mode given!");
}


//echo "<br/>Mode: "; echo $_GET['m'];

switch ( $_GET['m'] ) {
case 0:
    $dbresult = $dbConnect->getNames();
    $xmlDoc->createWidgets();
    $xmlDoc->addMysqlResult( $dbresult, 'Widget' );
    break;
case 1:
	if ( !isset( $_GET['name'] ) )
	{
		die("invalid parameter");
	}

	$name = $_GET['name'];
    $dbresult = $dbConnect->addName($name, $userId );
    die("Added");
    break;
case 2:
	if ( !isset($_GET['n']) ||  !isset( $_GET['a'] ) || !isset( $_GET['mo'] ) || !isset( $_GET['id'] ) || !isset( $_GET['h'] ) || !isset( $_GET['c'] ) /*|| !isset( $_GET['d'] )*/ )
	{
		die("invalid parameter");
	}

	$id = $_GET['id'];
	$name = $_GET['n'];
	$author = $_GET['a'];
	$mods = $_GET['mo'];
	$hidden = $_GET['h'];
	$category = $_GET['c'];
	$description = $san->sanitize( $_POST['d'] ); //$_GET['d'];

    $dbresult = $dbConnect->updateName( $id, $name, $author, $mods, $description, $userId, $hidden, $category );
    die("Updated");
    break;
case 3:
	if ( !isset( $_GET['v'] ) )
	{
		die("invalid parameter");
	}

	$ver = $_GET['v'];
	$nameId = $_GET['nId'];
		
    $dbresult = $dbConnect->addLuaVersion( $ver, $nameId, $userId  );
    echo $dbresult;
    die($dbresult);
    break;
case 4:
	if ( !isset( $_GET['c'] ) || !isset( $_GET['a'] ) || !isset( $_GET['id'] ) )
	{
		die("invalid parameter");
	}

	$id = $_GET['id'];
	$changelog = $_GET['c'];
	$active = $_GET['a'];

    $dbresult = $dbConnect->updateLuaVersion( $id, $changelog, $active, $userId);
    die("Updated");
    break;
case 5:
//add lua file
		if ( !isset( $_GET['lid'] ) || !isset( $_GET['l'] ) )
		{
			die("invalid parameter");
		}
		$id = intval( $_GET['lid'] );
		$local = $_GET['l'];
		
		if ( $local[0] != '/' )
		{
			$local = "/" . $local;
		}
		$pathPart = "files/" . $id . $local;
		
		mkdir_recursive( "files/" . $id . "/" . dirname($local), 0777 );
		
		$url = $luaFilesBaseUrl . "/" . $pathPart;
		if (move_uploaded_file($_FILES['file']['tmp_name'], $pathPart )) {
		} else {
		}

		$md5 = md5_file($pathPart );

		$dbConnect->addLuaFile( $local, $url, $id, $md5, $userId );
		die("Uploaded");
		break;
case 6:
//upload image
		if ( !isset( $_GET['nid'] )  )
		{
			die("invalid parameter");
		}
		$id = intval( $_GET['nid'] );
		
		if ( $dbConnect->checkNameRight( $id, $userId ) )
		{
			$pathPart = "images/" . $id . "_" . basename($_FILES['file']['name']);
	
			if (move_uploaded_file($_FILES['file']['tmp_name'], $pathPart )) 
			{
				$url = $luaFilesBaseUrl . "/" . $pathPart;
			
				$dbConnect->addImage( $id, $url );
				die("Uploaded");
			} else 
			{
				die("Error");
			}
		}
		die("Error");
		break;
case 7:
		//update thumbnail
		$id = intval( $_GET['lid'] );
		
		if ( $dbConnect->checkNameRight( $id, $userId ) )
		{
			$pathPart = "thumbnails/" . $id;
	
			if (move_uploaded_file($_FILES['file']['tmp_name'], $pathPart )) 
			{
				die("Uploaded");
			} else 
			{
				die("Error");
			}
		}
		else
		{
			die("Error");
		}
		break;
case 8:
		//get User Id
		die($userId);
		break;
case 9:
//get ALL Luas
		$dbresult = $dbConnect->getAllLuas(false, NULL, NULL);

    $xmlDoc->createWidgets();
    $xmlDoc->addMysqlResult( $dbresult, 'Widget' );
    
 //   $dbConnect->logAction( 3 );
    break;
    		
//deleters
case 10:
		$id = intval( $_GET['id'] );
		if ( $dbConnect->checkFileRight( $id, $userId ) )
		{
			deleteFile( $id );
			$dbConnect->deleteFile($id);
			die("Deleted");	
		}
		die("ERROR");
		break;
case 11:
		$id = intval( $_GET['id'] );		
		if ( $dbConnect->checkLuaRight( $id, $userId ) )
		{
			deleteFilesByLuaId( $id );
			$dbConnect->deleteLua($id);
			die("Deleted");
		}
		die("ERROR");
		break;
case 12:
		$id = intval( $_GET['id'] );
		
		if ( $dbConnect->checkImageRight( $id, $userId ) )
		{
			deleteImage($id);
			$dbConnect->deleteImage($id);
			die("Deleted");
		}
		die("ERROR");
		break;
case 13:
		$id = intval( $_GET['id'] );
		
		if ( $dbConnect->checkNameRight( $id, $userId ) )
		{
			deleteNameFiles( $id );
			$dbConnect->deleteName($id );
			die("Deleted");
		}
		die("ERROR");
		break;
case 14:
	//get overView (latest widgets only) with in-active widgets
	$dbresult = $dbConnect->getOverview(false);
	$xmlDoc->createWidgets();
	$xmlDoc->addMysqlResult( $dbresult, 'Widget' );
	break;
case 15:
	//Add Mod
	if ( !isset( $_GET['name'] ) ) { die("invalid parameter"); }

	$name = $_GET['name'];
	$dbresult = $dbConnect->addMod($name, $userId );
	die("Added");
	break;
case 16:
	//Add ModWidget
	if ( ( !isset( $_GET['n'] ) )  ) {  die("invalid parameter"); }
	
	$name = $_GET['n'];
	$modId = $_GET['id'];
	$dbresult = $dbConnect->addModWidget($name, $modId);
	die("Added");
	break;	
case 17:
	//Remove Mod
	$id = intval( $_GET['id'] );
		
	if ( $dbConnect->checkModRight( $id, $userId ) )
	{		
		$dbConnect->deleteMod($id );
		die("Deleted");
	}
	die("ERROR");
case 18:
	//Remove ModWidget
	$id = intval( $_GET['id'] );
		
	if ( $dbConnect->checkModWidgetRight( $id, $userId ) )
	{		
		$dbConnect->deleteModWidgetById($id );
		die("Deleted");
	}
	die("ERROR");
case 19:
	//UpdateModWidget
	if ( !isset( $_GET['n'] ) || !isset( $_GET['d'] ) ) { die("invalid parameter"); }

	$id = $_GET['id'];
	$name = $_GET['n'];
	$description = $san->sanitize( $_GET['d'] );

    $dbresult = $dbConnect->updateModWidgetInfo( $id, $name, $description );
    die("Updated");
    break;
case 20:
	//UpdateMod
	if ( !isset( $_GET['n'] ) || !isset( $_GET['o'] ) ) { die("invalid parameter"); }

	$id = $_GET['id'];
	$name = $_GET['n'];
	$oderfilename = $san->sanitize( $_GET['o'] );

    $dbresult = $dbConnect->updateMod( $id, $name, $oderfilename );
    die("Updated");
    break;
case 21:
	//Remove Category
	
	$id = intval( $_GET['id'] );
	
	if ( $dbConnect->checkCategoryRight( $id, $userId ) )
	{		
		$dbConnect->deleteCategory( $id );
	}
	die("Removed!");
case 22:
	//Add Category
	if ( ( !isset( $_GET['n'] ) )  ) {  die("invalid parameter"); }
	
	$name = $_GET['n'];
	$dbresult = $dbConnect->addCategory($name, $userId);
	die("Added");
	break;	
default:
	die("Unknown mode!");
	break;
}


$xml = $xmlDoc->getXml();
echo $xml;

?>