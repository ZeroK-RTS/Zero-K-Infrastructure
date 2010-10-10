<?php

include('config.php');
include('dbConnection.php');
include('xmlDocument.php');



ini_set('display_errors', 1);
set_time_limit(3600); //Timeout limit 1 hour (could be a big file or slow con)

if ( $_GET['m'] == NULL )
{
		die("No mode given!");
}

$dbConnect = new dbConnection;
$xmlDoc = new xmlDocument;

class vsp
{
	public $login;
	public $password;
}

function validateSpringAccount( $username, $pw )
{
return true; //remove when new SD (>1.4.1) is out (to activate account verification)

    $param1 = new vsp();
    $param1->login = $username;
    $param1->password = $pw;

    $soap = new SoapClient("http://planet-wars.eu/SpringAuthService.asmx?wsdl");
    
    $res = $soap->VerifySpringAccount( $param1  );
	return $res->VerifySpringAccountResult;
}

function checkUsername( $username, $pw )
{
	global $dbConnect;
	
	if ( $pw != null && !validateSpringAccount( $username, $pw ) )
	{
		return -1;
	}
	
	$curId = $dbConnect->getUserIdByName( $username );
	if ( $curId == -1 )
	{
		$dbConnect->addUser( $username );
		$curId = $dbConnect->getUserIdByName( $username );
	}
	
	return $curId;
}

switch ( $_GET['m'] ) {
case 0:
    $dbresult = $dbConnect->getOverview(true);
    $xmlDoc->createWidgets();
    $xmlDoc->addMysqlResult( $dbresult, 'Widget' );
    break;
case 1:
	$luaId = $_GET['id'];
    $dbresult = $dbConnect->getFilesByLua($luaId);

    $xmlDoc->createFiles();
    $xmlDoc->addMysqlResult( $dbresult, 'File' );
    break;
case 2:
	$luaId = $_GET['id'];
    $dbresult = $dbConnect->getLuaById($luaId);

    $xmlDoc->createWidgets();
    $xmlDoc->addMysqlResult( $dbresult, 'Widget' );
    break;
case 3:
//get all active luas
	$dbresult = $dbConnect->getAllLuas(true, NULL, NULL);

    $xmlDoc->createWidgets();
    $xmlDoc->addMysqlResult( $dbresult, 'Widget' );
    
    $dbConnect->logAction( 3 );
    break;
case 4:  //get images by nameid
	$nameId = $_GET['id'];
    $dbresult = $dbConnect->getImagesByNameId($nameId);

    $xmlDoc->createFiles();
    $xmlDoc->addMysqlResult( $dbresult, 'File' );
    break;
case 5:
	$luaId = intval( $_GET['id'] );
	$dbresult = $dbConnect->incDownloadCount( $luaId );
    $dbConnect->logAction( 5 );
	die("Counted!");
case 6:
	$nameId = $_GET['id'];
    $dbresult = $dbConnect->getLuasByNameId($nameId);

    $xmlDoc->createWidgets();
    $xmlDoc->addMysqlResult( $dbresult, 'Widget' );
    break;

//Zipped download
//http://spring.vsync.de/luaManager/lua_manager.php?m=10&id=50
case 10:
	if ( !isset( $_GET['id'] ) ) {
		die("No ID!");
		break;
	}
	
	$luaId = intval( $_GET['id'] );
	$zip = new ZipArchive;
		
	$tmpfname = tempnam("/tmp", "zipsadfewrwer");
		
    if ($zip->open( $tmpfname, ZIPARCHIVE::OVERWRITE)!==TRUE) {
    	exit("dcannot open <" . $tmpfname . ">\n");
	}
    
    $dbresult = $dbConnect->getLuaById($luaId);
	$name = "";
	while($row = mysql_fetch_assoc($dbresult)) {
		$name = $row['Name'];
	}
		

	$dbresult = $dbConnect->getFilesByLua($luaId);
	while($row = mysql_fetch_assoc($dbresult)) {
		$localP = $row['LocalPath'];
		$localP = trim( $localP, "/\\" );
		$zip->addFromString( $localP, file_get_contents( $row['Url'] ) );
	}
	$zip->close();

	  
	header('Pragma: public');
	header('Content-type: application/zip');
	header('Content-length: ' . filesize($tmpfname));
	header('Content-Disposition: attachment; filename="'. $name .'.zip"');
	readfile($tmpfname); 
		
	unlink($tmpfname);
		
	$dbresult = $dbConnect->incDownloadCount( $luaId );
	exit; 
case 11:
//get all mods for widget activation
	$dbresult = $dbConnect->getActivationModList();

    $xmlDoc->createMods();
    $xmlDoc->addMysqlResult( $dbresult, 'Mod' );
    break;
case 12:
//get all mod widgets
	$dbresult = $dbConnect->getModWidgetsAll();

    $xmlDoc->createModWidgets();
    $xmlDoc->addMysqlResult( $dbresult, 'ModWidget' );
    break;
	
case 13:
	//get  mod widgets by modId
	if ( !isset( $_GET['id'] ) ) {
		die("No ID!");
		break;
	}
	
	$modId = $_GET['id'];
	$dbresult = $dbConnect->getModWidgets($modId );

    $xmlDoc->createModWidgets();
    $xmlDoc->addMysqlResult( $dbresult, 'ModWidget' );
    break;
case 14:
	//add widget rating
	if ( !isset( $_GET['id'] ) || !isset( $_GET['uname'] ) || !isset( $_GET['pw'] ) || !isset( $_GET['r'] ) ) {
		die("Missing parameter!");
		break;
	}
	
	$nameId = $_GET['id'];
	$username = $_GET['uname'];
	$pw = $_GET['pw'];
	$rating = $_GET['r'];
	
	$userId = checkUsername( $username, $pw);
	if ( $userId < 0 )
	{
		die("Rejected!");
	}
	
	$dbresult = $dbConnect->addRating($nameId, $userId, $rating );

	die("Rated!");
    break;
case 15:
	//add widget comment
	if ( !isset( $_GET['id'] ) || !isset( $_GET['uname'] ) || !isset( $_GET['pw'] ) || !isset( $_POST['c'] ) ) {
		die("Missing parameter!");
		break;
	}
	
	$nameId = $_GET['id'];
	$username = $_GET['uname'];
	$pw = $_GET['pw'];
	$comment = $_POST['c'];

	$userId = checkUsername( $username, $pw );
	if ( $userId < 0 )
	{
		die("Rejected!");
	}
	
	$dbresult = $dbConnect->addComment($nameId, $userId, $comment );

	die("Commented!");
    break;
case 16:  
	//get comments
	$nameId = $_GET['id'];
    $dbresult = $dbConnect->getCommentsByNameId($nameId);

    $xmlDoc->createComments();
    $xmlDoc->addMysqlResult( $dbresult, 'Comment' );
    break;
case 17:
//get files for multiple lua ids, they come comma separated
	if ( !isset($_POST['ids']) ) 
	{
		$luaIds = $_GET['ids'];
	}
	else
	{
		$luaIds = $_POST['ids'];
	}
	$luaIdsArr = explode(",", $luaIds);
    $dbresult = $dbConnect->getFilesByLuas($luaIdsArr);

    $xmlDoc->createFiles();
    $xmlDoc->addMysqlResult( $dbresult, 'File' );
    break;
case 18:
	//get profile installation by userId
	if ( !isset( $_GET['uname'] ) || !isset( $_GET['pw'] ) ) {
		die("Missing parameter!");
		break;
	}

	$username = $_GET['uname'];
	$pw = $_GET['pw'];

	$userId = checkUsername( $username, $pw );
	if ( $userId < 0 )
	{
		die("Rejected!");
	}
	
    $dbresult = $dbConnect->getProfileInstallations($userId);

    $xmlDoc->createInstallations();
    $xmlDoc->addMysqlResult( $dbresult, 'Inst' );
    break;
case 19:
	//get profile activation by userId and modId
	if ( !isset( $_GET['uname'] ) || !isset( $_GET['pw'] ) || !isset( $_GET['modId'] ) ) {
		die("Missing parameter!");
		break;
	}
	
	$username = $_GET['uname'];
	$pw = $_GET['pw'];
	$modId = $_GET['modId'];

	$userId = checkUsername( $username, $pw );
	if ( $userId < 0 )
	{
		die("Rejected!");
	}

    $dbresult = $dbConnect->getProfileActivations($userId, $modId);

    $xmlDoc->createActivations();
    $xmlDoc->addMysqlResult( $dbresult, 'Act' );
    break;
case 20:
//get all categroies
	$dbresult = $dbConnect->getCategories();

    $xmlDoc->createCategories();
    $xmlDoc->addMysqlResult( $dbresult, 'Category' );
    break;
case 21:
//get user's rating for name id
	if ( !isset( $_GET['pw'] ) || !isset( $_GET['uname'] ) || !isset( $_GET['id'] )  ) {
		die("Missing parameter!");
		break;
	}
	
	$username = $_GET['uname'];
	$pw = $_GET['pw'];
	$nameId = $_GET['id'];
	
	$userId = checkUsername( $username, $pw );
	if ( $userId < 0 )
	{
		die("Rejected!");
	}

	$dbresult = $dbConnect->getSingleRating($nameId, $userId);
	
    $xmlDoc->createRatings();
    $xmlDoc->addMysqlResult( $dbresult, 'Rating' );
    break;	
case 25:
	//set profile installation
	if ( !isset( $_POST['ids'] ) || !isset( $_GET['uname'] ) || !isset( $_GET['pw'] )  ) {
		die("Missing parameter!");
		break;
	}
	
	$username = $_GET['uname'];
	$pw = $_GET['pw'];
	$nameIds = $_POST['ids'];
	
	$userId = checkUsername( $username, $pw );
	if ( $userId < 0 )
	{
		die("Rejected!");
	}

	$nameIdArr = explode(",", $nameIds );
    $dbConnect->setProfileInstallation($userId, $nameIdArr);
	die("Saved!");
	break;
case 26:
	//set profile activation
	if ( !isset( $_POST['names'] ) || !isset( $_GET['uname'] ) || !isset( $_GET['pw'] ) || !isset( $_GET['modId'] ) ) {
		die("Missing parameter!");
		break;
	}
	
	$username = $_GET['uname'];
	$pw = $_GET['pw'];
	$names = $_POST['names'];
	$modId = $_GET['modId'];
	
	$userId = checkUsername( $username, $pw );
	if ( $userId < 0 )
	{
		die("Rejected!");
	}

	$namesArr = explode(",", $names );
    $dbConnect->setProfileActivation($userId, $modId, $namesArr);
	die("Saved!");
	break;
default:
		die("Unknown mode!");
		break;
}


$xml = $xmlDoc->getXml();
echo $xml;

/*
function my_dir($dir){
    $arfdn = explode('/', $dir);
    return end($arfdn);
}

function mkdir_recursive($pathname, $mode)
{
    is_dir(dirname($pathname)) || mkdir_recursive(dirname($pathname), $mode);
    return is_dir($pathname) || @mkdir($pathname, $mode);
}

function download ($file_source, $file_target)
{
  // Preparations
  $file_source = str_replace(' ', '%20', html_entity_decode($file_source)); // fix url format
  if (file_exists($file_target)) { chmod($file_target, 0644); } // add write permission

  // Begin transfer
  if (($rh = fopen($file_source, 'r')) === FALSE) { echo "error1 ".$file_source; return false; } // fopen() handles
  if (($wh = fopen($file_target, 'wb')) === FALSE) { echo "error2"; return false; } // error messages.
  while (!feof($rh))
  {
    // unable to write to file, possibly because the harddrive has filled up
    if (fwrite($wh, fread($rh, 1024)) === FALSE) { fclose($rh); fclose($wh); echo "write error"; return false; }
  }

  // Finished without errors
  fclose($rh);
  fclose($wh);
  return true;
}
*/

?>