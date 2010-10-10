<?php
//include('config.php');
//this class requires the class DbConfig to be defined
//include config.php before including dbConnection

class dbConnection
{
	var $dbConfig;
	var $db_link = false;
	
	function __construct() {
		$this->dbConfig = new DbConfig;
  		$this->connect( $this->dbConfig->host, $this->dbConfig->user, $this->dbConfig->pass, $this->dbConfig->dbname );
	}

		
	function __destruct() {
       $this->closeDb();
	}
   
	function fix_for_mysql($value){
		if (get_magic_quotes_gpc())
			$value = stripslashes($value);
		$value = mysql_real_escape_string($value);
		return $value;
	}

	function connect($host,$user,$pass,$dbname)
	{
		$this->db_link = @mysql_connect($host,$user,$pass) or die ("Could not connect to DB!");
		@mysql_select_db($dbname) or die ("Could not select DB!");
	}
	
	function closeDb()
	{
		if ( $db_link != false )
		{
			mysql_close($this->db_link);
		}
	}
	
	function query( $query )
	{
		$dbresult = mysql_query( $query, $this->db_link);
		return $dbresult;
	}
	
	function checkLogin( $name, $pw )
	{
		$query = "select * from Users where Name = '" . $this->fix_for_mysql($name) . "' and PasswdMd5 = '" . $this->fix_for_mysql($pw) . "';";
		$dbresult = $this->query( $query );
		
		if ( mysql_num_rows( $dbresult ) == 1 )
		{
			$user = mysql_fetch_assoc($dbresult);
			$id = $user['Id'];
			if ( $user['Admin'] == 1 )
			{
				$id = "-" . $id;
			}
			return $id;
		}
		
		return false;
	}
	
	
	////////////////////////////////////////
	//PUBLIC /////////////////////////////////////////////////
	////////////////////////////////////////
	///DELETE
	function deleteMod( $id )
	{
		$this->deleteModWidgetsByModId( $id );
		
		$query = "delete from Mods where Id = " . $this->fix_for_mysql($id);
		return $this->query( $query );
	}
	
	function deleteModWidgetsByModId( $id )
	{
		$query = "delete from ModWidgets where ModId = " . $this->fix_for_mysql($id);
		return $this->query( $query );
	}
	
	function deleteModWidgetById( $id )
	{
		$query = "delete from ModWidgets where Id = " . $this->fix_for_mysql($id);
		return $this->query( $query );
	}
	
	function addMod($name, $ownerId)
	{
		$ownerId = abs($ownerId);
		$query = "insert into Mods (Abbreviation, OwnerId) VALUES  ('" . $this->fix_for_mysql($name) . "', '" . $this->fix_for_mysql($ownerId) . "')";
		return $this->query( $query );
	}
	
	function updateMod($id, $name, $orderFilename )
	{
		$query = "update Mods set Abbreviation = '" . $this->fix_for_mysql($name) . "', OrderConfigFilename='" . $this->fix_for_mysql($orderFilename) . "' where Id = " . $this->fix_for_mysql($id);
		return $this->query( $query );
	}
	
	function getModById($id)
	{
		$query = "select * from Mods where Id ='" . $this->fix_for_mysql($id) . "'";
		return $this->query( $query );
	}
	
	function getModWidgetById($id)
	{
		$query = "select * from ModWidgets where Id ='" . $this->fix_for_mysql($id) . "'";
		return $this->query( $query );
	}
	
	function addModWidget($name, $modId)
	{
		$query = "insert into ModWidgets (HeaderName, ModId, Description) VALUES  ('" . $this->fix_for_mysql($name) . "', '" . $this->fix_for_mysql($modId) . "', '" . $this->fix_for_mysql($description) . "')";
		return $this->query( $query );
	}
	
	function updateModWidgetInfo($id, $name, $description )
	{
		$query = "update ModWidgets set HeaderName = '" . $this->fix_for_mysql($name) . "', Description='" . $this->fix_for_mysql($description) . "' where Id = " . $this->fix_for_mysql($id);
		return $this->query( $query );
	}
	
	
	
	
	
	
	
	
	
	function deleteFile( $id )
	{
		$query = "delete from Files where Id = " . $this->fix_for_mysql($id);
		return $this->query( $query );
	}
	
	function deleteFilesByLuaId( $id )
	{
		$query = "delete from Files where LuaId = " . $this->fix_for_mysql($id);
		return $this->query( $query );
	}
	
	function deleteImage( $id )
	{
		$query = "delete from Images where Id = " . $this->fix_for_mysql($id);
		return $this->query( $query );
	}
	
	function deleteImagesByNameId( $id )
	{
		$query = "delete from Images where NameId = " . $this->fix_for_mysql($id);
		return $this->query( $query );
	}
	
	function deleteLua( $id )
	{
		$this->deleteFilesByLuaId( $id );
		
		$query = "delete from LUAs where Id = " . $this->fix_for_mysql($id);
		return $this->query( $query );
	}
	
	function deleteLuasByNameId( $id )
	{
		$dbresult = $this->getLuasByNameId( $id );
		
		while($row = mysql_fetch_assoc($dbresult)) 
		{
			$this->deleteLua( $row["Id"] );
		}
	}

	function deleteName( $nameId )
	{
		$this->deleteLuasByNameId( $nameId );
		$this->deleteImagesByNameId( $nameId );
		
		$query = "delete from Names where Id = " . $this->fix_for_mysql($nameId);
		return $this->query( $query );
	}
	
	///END 
	
	///////////// RIGHTS CHECKS
	function checkImageRight( $imageId, $userId )
	{
		$image = $this->getImage($imageId);
		return $this->checkNameRight( $image['NameId'], $userId );
	}
	
	function checkFileRight( $fileId, $userId )
	{
		$dbresult = $this->getFile($fileId);
		$file = mysql_fetch_assoc($dbresult);
		return $this->checkLuaRight( $file['LuaId'], $userId );
	}
	
	function checkLuaRight( $luaId, $userId )
	{
		$nid = $this->getNameIdByLuaId( $luaId );
		return $this->checkNameRight( $nid, $userId );
	}
	
	function checkNameRight( $nameId, $userId )
	{
		$dbresult = $this->getNameById( $nameId );
		$row = mysql_fetch_assoc($dbresult);
		
		if ( ( $userId < 0 ) || ( $row['OwnerId'] == abs($userId) ) )
		{
			return true;
		}		
		die("a");
	}
	///MOD RIGHTS CHECK
	
	function checkModRight( $modId, $userId )
	{
		$dbresult = $this->getModById( $modId );
		$row = mysql_fetch_assoc($dbresult);
		
		if ( ( $userId < 0 ) || ( $row['OwnerId'] == abs($userId) ) )
		{
			return true;
		}		
		die("a");
	}
	
	function checkCategoryRight( $categoryId, $userId )
	{
		$dbresult = $this->getCategoryById( $categoryId );
		$row = mysql_fetch_assoc($dbresult);
		
		if ( ( $userId < 0 ) || ( $row['OwnerId'] == abs($userId) ) )
		{
			return true;
		}		
		die("a");
	}
	
	function checkModWidgetRight( $modWidgetId, $userId )
	{
		$dbresult = $this->getModWidgetById( $modWidgetId );
		$widget = mysql_fetch_assoc($dbresult);
		return $this->checkModRight( $widget['ModId'], $userId );
	}
	
	

	
	function addName($name, $ownerId)
	{
		$ownerId = abs($ownerId);
		$query = "insert into Names (Name, OwnerId) VALUES  ('" . $this->fix_for_mysql($name) . "', '" . $this->fix_for_mysql($ownerId) . "')";
		return $this->query( $query );
	}
	
	function addImage($imageUrl, $nameId)
	{
		//pls do a rights check before calling this function
		$query = "insert into Images (NameId, Url) VALUES  ('" . $this->fix_for_mysql($imageUrl) . "', '" . $this->fix_for_mysql($nameId) . "' )";
		return $this->query( $query );
	}
	

	
	function addLuaVersion($version, $nameId, $userId  )
	{
		if ( !$this->checkNameRight( $nameId, $userId ) ) {
			return;
		}
		
		$query = "insert into LUAs (Version,NameId) VALUES  ('" . $this->fix_for_mysql($version) . "', '" . $this->fix_for_mysql($nameId) . "' )";
		$this->query( $query );
		$newId = mysql_insert_id();
	//	echo "ID:" . $newId;
		return $newId;
	}
	
	function addLuaFile( $localPath, $url, $luaId, $md5, $userId )
	{
		if ( !$this->checkLuaRight( $luaId, $userId ) ) {
			return;
		}
		$query = "insert into Files (LuaId, Url, MD5, LocalPath) VALUES  ('" . $this->fix_for_mysql($luaId) . "', '" . $this->fix_for_mysql($url) . "', '" . $this->fix_for_mysql($md5) . "', '" . $this->fix_for_mysql($localPath) . "' )";
		return $this->query( $query );
	}
	
	
	
	function updateName($id, $name, $author, $mods, $description, $userId, $hidden, $category )
	{
		if ( $category == 0 )
		{
			$category = "NULL";
		}
		
		if ( !$this->checkNameRight( $id, $userId ) ) {
			return;
		}
		$query = "update Names set Name = '" . $this->fix_for_mysql($name) . "', Author = '" . $this->fix_for_mysql($author) . "', Description='" . $this->fix_for_mysql($description) . "', Mods='" . $this->fix_for_mysql($mods) . "', Hidden=" . $this->fix_for_mysql($hidden) . ", CategoryId=" . $this->fix_for_mysql($category) .  " where Id = " . $this->fix_for_mysql($id);
		return $this->query( $query );
	}
	
	function updateLuaVersion( $id, $changelog, $active, $userId )
	{
		if ( !$this->checkLuaRight( $id, $userId ) ) {
			return;
		}
		$query = "update LUAs set Changelog = '" . $this->fix_for_mysql($changelog) . "', Active='" . $this->fix_for_mysql($active) . "' where Id = " . $this->fix_for_mysql($id);
		return $this->query( $query );
	}
	
	
	/////////// CLIENT FUNCTIONS
	
	function getSingleRating($nameId, $userId)
	{
		$query = "select Rating from RatingsWidget where NameId = " . $this->fix_for_mysql($nameId) . " and UserId = " . $this->fix_for_mysql($userId) . ";";
		return $this->query( $query );
	}
	
	function incDownloadCount( $luaId )
	{
		//workaround for compatibility - better would be to give nameId directly
		$nameId = $this->getNameIdByLuaId( $luaId );
		
		$query = "UPDATE Names SET DownloadCount = DownloadCount + 1 WHERE ID = " . $this->fix_for_mysql($nameId);
		return $this->query( $query );
	}
	
	function getNames()
	{
		$query = "select ";
		$query = $query . "Names.Id, Name, Author, Description, Mods, OwnerId, Hidden, CategoryId";
		$query = $query . ", DownloadCount / TIMESTAMPDIFF( DAY, MIN(LUAs.Entry), NOW() ) as DownsPerDay";
		$query = $query . ", (select COUNT(*) From CommentsWidget Where CommentsWidget.NameId = Names.Id) as CommentCount";
		$query = $query . ", (select AVG(rrr.Rating) from RatingsWidget as rrr where rrr.nameId = Names.Id group by rrr.NameId ) as Rating";
		$query = $query . ", (select COUNT(*) From RatingsWidget Where RatingsWidget.NameId = Names.Id) as VoteCount";
		$query = $query . " from Names left join LUAs on Names.Id = LUAs.NameID group By NameId";
		return $this->query( $query );
	}

	function getNameById($id)
	{
		$query = "select ";
		$query = $query . "Names.Id, Name, Author, Description, Mods, OwnerId, Hidden, CategoryId";
		$query = $query . ", DownloadCount / TIMESTAMPDIFF( DAY, MIN(LUAs.Entry), NOW() ) as DownsPerDay";
		$query = $query . ", (select COUNT(*) From CommentsWidget Where CommentsWidget.NameId = Names.Id) as CommentCount";
		$query = $query . ", (select AVG(rrr.Rating) from RatingsWidget as rrr where rrr.nameId = Names.Id group by rrr.NameId ) as Rating";
		$query = $query . ", (select COUNT(*) From RatingsWidget Where RatingsWidget.NameId = Names.Id) as VoteCount";
		$query = $query . " from Names left join LUAs on Names.Id = LUAs.NameID where Names.Id = " . $this->fix_for_mysql($id) . " group By NameId";
//		$query = "select * from Names;
		return $this->query( $query );
	}
	
	function getNameIdByLuaId( $luaId )
	{
		$dbresult = $this->getLuaById( $luaId );
		$lua = mysql_fetch_assoc($dbresult);
		return $lua['NameId'];
	}
	
	function getFilesByLua($luaId)
	{
	//	$query = "select Files.Id, Files.Url, Files.MD5, Files.LocalPath, Files.LuaId from Files where Files.LuaId = " . $this->fix_for_mysql($luaId);
	//	return $this->query( $query );
		return $this->getFiles( $luaId, NULL );
	}
	
	//string should have form like 
	function getFilesByLuas($luaIds)
	{
		return $this->getFiles( NULL, $luaIds );
	}
	
	function getFiles($luaId, $luaIds)
	{
		$query = "select Files.Id, Files.Url, Files.MD5, Files.LocalPath, Files.LuaId from Files where Files.LuaId ";
		
		if ( $luaId != NULL )
		{
			$query = $query . "= " . $this->fix_for_mysql($luaId);
		}
		else
		{
			$query = $query . "in (";
			$count = count($luaIds);
			for ($i = 0; $i < $count; $i++) 
			{
				if ( !is_numeric($luaIds[$i]) )
				{
					//just to be sure
					return NULL;
				}
				
				if ( $i > 0 )
				{
					$query = $query . ",";
				}
				$query = $query . $this->fix_for_mysql($luaIds[$i]);
			}
			$query = $query . ")";
		}
		
		return $this->query( $query );
	}
	
	function getFile($fileId)
	{
		$query = "select Files.Id, Files.Url, Files.MD5, Files.LocalPath, Files.LuaId from Files where Files.Id = " . $this->fix_for_mysql($fileId);
		return $this->query( $query );
	}
	
	function getImagesByNameId($nameId)
	{
		$query = "select Id, NameId, Url from Images where NameId = " . $this->fix_for_mysql($nameId);
		return $this->query( $query );
	}
	
	function getImage($id)
	{
		$query = "select Id, NameId, Url from Images where Id = " . $this->fix_for_mysql($id);
		return $this->query( $query );
	}
	
	function getOverview($activeOnly)
	{ 
		//$query = "select count( Images.Id ) as ImageCount, a.Id, a.DownloadCount, a.Version, a.NameId, a.Changelog, Names.Mods, Names.Name, Names.Description, Names.Author, a.Entry from LUAs a left outer join Images on a.NameId = Images.NameId left join Names on a.NameId = Names.Id where not exists (select * from LUAs b WHERE b.Version > a.Version and a.NameId = b.NameId and b.Active = 1 ) group by Names.Id";
		$query = "select";
		$query = $query . " (select AVG(rrr.Rating) from RatingsWidget as rrr where rrr.nameId = Names.Id group by rrr.NameId ) as Rating";
		$query = $query . ", (select COUNT(*) From RatingsWidget Where RatingsWidget.NameId = Names.Id) as VoteCount";
		$query = $query . ", (select COUNT(*) From CommentsWidget Where CommentsWidget.NameId = Names.Id) as CommentCount";
		$query = $query . ", (select Names.DownloadCount / ( 1 + TIMESTAMPDIFF( DAY, MIN(LUAs.Entry), NOW()) ) from Names as N left join LUAs on N.ID where NameID = a.NameId) as DownsPerDay";
		$query = $query . ", (select count(*) from Images where Images.NameId = a.NameId) as ImageCount";
		$query = $query . ", a.Id, Names.DownloadCount, a.Version, a.NameId, a.Changelog, Names.Mods, Names.Name, Names.Description, Names.Author, Names.Hidden, a.Entry, Names.CategoryId";
		$query = $query . " from LUAs a left join Names on a.NameId = Names.Id where not exists (select * from LUAs b WHERE b.Version > a.Version and a.NameId = b.NameId and b.Active = 1 )";
//		$query = "select a.Id, a.DownloadCount, a.Version, a.NameId, a.Changelog, Names.Mods, Names.Name, Names.Description, Names.Author, a.Entry from LUAs a left join Names on a.NameId = Names.Id where not exists (select * from LUAs b WHERE b.Version > a.Version and a.NameId = b.NameId and b.Active = 1 )";
		
		if ( $activeOnly == true ) 
		{
			$query = $query . " and a.Active = 1";
		}
		
		return $this->query( $query );
	}
	
	function getAllLuas( $activeOnly, $nameId, $luaId )
	{
		$query = "select";
		$query = $query . " (select Names.DownloadCount / ( 1 + TIMESTAMPDIFF( DAY, MIN(LUAs.Entry), NOW()) ) from Names as N left join LUAs on N.ID where NameID = a.NameId) as DownsPerDay";
		$query = $query . ", (select count(*) from Images where Images.NameId = a.NameId) as ImageCount";
		$query = $query . ", (select AVG(rrr.Rating) from RatingsWidget as rrr where rrr.nameId = Names.Id group by rrr.NameId ) as Rating";
		$query = $query . ", (select COUNT(*) From RatingsWidget Where RatingsWidget.NameId = Names.Id) as VoteCount";
		$query = $query . ", (select COUNT(*) From CommentsWidget Where CommentsWidget.NameId = Names.Id) as CommentCount";
		$query = $query . ", a.Active, Names.DownloadCount, a.Id, a.Version, a.NameId, a.Changelog, Names.OwnerId, Names.Mods, Names.Name, Names.Description, Names.Author, Names.Hidden, a.Entry, Names.CategoryId";
		$query = $query . " from LUAs a left join Names on a.NameId = Names.Id"; // 
		
		if ( $activeOnly == true ) 
		{
			$query = $query . " where a.Active = 1";
		}
		else if ( $nameId != NULL )
		{
			$query = $query . " where a.NameId = " . $this->fix_for_mysql($nameId);		
		}
		else if ( $luaId != NULL )
		{
			$query = $query . " where a.Id = " . $this->fix_for_mysql($luaId);
		}
		
		return $this->query( $query );
	}
	
	function getLuaById($luaId)
	{
		return $this->getAllLuas( NULL, NULL, $luaId );
/*		$query = "select";
		$query = $query . " (select Names.DownloadCount / ( 1 + TIMESTAMPDIFF( DAY, MIN(LUAs.Entry), NOW()) ) from Names as N left join LUAs on N.ID where NameID = a.NameId) as DownsPerDay";
		$query = $query . ", (select count(*) from Images where Images.NameId = a.NameId) as ImageCount";
		$query = $query . ", a.Id, Names.DownloadCount, a.Version, a.NameId, a.Changelog, Names.Mods, Names.Name,Names.Description, Names.Author, a.Entry";
		$query = $query . "	from LUAs a left join Names on a.NameId = Names.Id where a.Id = " . $this->fix_for_mysql($luaId);// . " group by Images.NameId"; //. " and a.Active = 1";
		return $this->query( $query );
		*/
	}
	
	function getLuasByNameId($nameId)
	{
		return $this->getAllLuas( NULL, $nameId, NULL );
/*		$query = "select (select Names.DownloadCount / ( 1 + TIMESTAMPDIFF( DAY, MIN(LUAs.Entry), NOW()) ) from Names as N left join LUAs on N.ID where NameID = a.NameId) as DownsPerDay";
		$query = $query . ", (select count(*) from Images where Images.NameId = a.NameId) as ImageCount";
		$query = $query . ", a.Id, Names.DownloadCount, a.Version, a.NameId, a.Changelog, Names.Mods, Names.Name,Names.Description, Names.Author, a.Entry";
		$query = $query . " from LUAs a left join Names on a.NameId = Names.Id where a.NameId = " . $this->fix_for_mysql($nameId); // . " group by Images.NameId";
		return $this->query( $query );
		*/
	}

	
	function logAction( $actionId )
	{
		$query = "insert into Log (Action) VALUES  ('" . $this->fix_for_mysql($actionId) . "')";
		return $this->query( $query );
	}
	
	//Widget Activation stuff
	function getActivationModList()
	{
		$query = "select * from Mods";
		return $this->query( $query );
	}
	
	function getModWidgetsAll()
	{
		$query = "select * from ModWidgets";
		return $this->query( $query );
	}
	
	function getModWidgets($modId)
	{
		$query = "select * from ModWidgets where ModId = " . $this->fix_for_mysql($modId);
		return $this->query( $query );
	}
	
	function addUser( $name )
	{
		$query = "insert into SpringUsers (Username) VALUES ('" . $this->fix_for_mysql($name) . "')";
		$this->query( $query );
	}

	function getUserIdByName( $name )
	{
		$query = "select * from SpringUsers where Username = '" . $this->fix_for_mysql($name) . "'";
		$result = $this->query( $query );
		
		if ( mysql_num_rows( $result) > 0 )
		{
			$user = mysql_fetch_assoc($result);
			return $user['Id'];
		}
		else
		{
			return -1;
		}
	}
	
	//Rating	
	function addRating($nameId, $userId, $rating)
	{
		$ownerId = abs($ownerId);
		
		//delete rating if existing
		$query = "delete from RatingsWidget where NameId = '" . $this->fix_for_mysql($nameId) . "' and UserId = '" . $this->fix_for_mysql($userId) . "'";
		$this->query( $query );
		
		//add rating
		$query = "insert into RatingsWidget (NameId, UserId, Rating) VALUES  ('" . $this->fix_for_mysql($nameId) . "', '" . $this->fix_for_mysql($userId) . "', '" . $this->fix_for_mysql($rating) . "')";
		return $this->query( $query );
	}
	
	//Comments	
	function addComment($nameId, $userId, $comment)
	{
		$ownerId = abs($ownerId);
		$query = "insert into CommentsWidget (NameId, UserId, Comment) VALUES  ('" . $this->fix_for_mysql($nameId) . "', '" . $this->fix_for_mysql($userId) . "', '" . $this->fix_for_mysql($comment) . "')";
		return $this->query( $query );
	}
	
	function getCommentsByNameId($nameId)
	{
		$query = "select Comment, CommentsWidget.Entry, SpringUsers.Username from CommentsWidget left join SpringUsers on SpringUsers.Id = CommentsWidget.UserId where NameId = " . $this->fix_for_mysql($nameId);
		return $this->query( $query );
	}
	
	//Profiles
	function getProfileInstallations($userId)
	{
		$query = "select NameId from ProfileInstalls where UserId = '" . $userId . "';";
		return $this->query($query);
	}
	
	function getProfileActivations($userId, $modId)
	{
		$query = "select WidgetName from ProfileActivates where UserId = '" . $userId . "' and ModId = " . $modId . ";";
		return $this->query($query);
	}
	
	function addProfileActivation($userId, $modId, $widgetName )
	{
		$query = "insert into ProfileActivates (UserId, ModId, WidgetName) VALUES  (" . $this->fix_for_mysql($userId) . ", " . $this->fix_for_mysql($modId) . ", '" .$this->fix_for_mysql($widgetName) . "');";
		return $this->query($query);
	}

	function addProfileInstallation($userId, $nameId )
	{
		$query = "insert into ProfileInstalls (UserId, NameId) VALUES  (" . $this->fix_for_mysql($userId) . ", " . $this->fix_for_mysql($nameId) . ");";
		return $this->query($query);
	}
	
	function setProfileInstallation($userId, $nameIds )
	{
		//delete all from user
		$query = "delete from ProfileInstalls where UserId = " . $userId . ";";
		$this->query($query);
		
		//add new installations
		for ($i = 0; $i < count($nameIds); $i++) 
		{
			$this->addProfileInstallation($userId, $nameIds[$i]);
		}
	}
	
	function setProfileActivation($userId, $modId, $widgetNames )
	{
		//delete all from user
		$query = "delete from ProfileActivates where UserId = " . $userId . " and ModId = " . $modId . ";";
		$this->query($query);
		
		echo count($widgetNames);
		
		//add new Activation
		for ($i = 0; $i < count($widgetNames); $i++) 
		{
			$this->addProfileActivation($userId, $modId, $widgetNames[$i]);
		}
	}
	
	//Categories
	function getCategories()
	{
		$query = "select * from Categories;";
		return $this->query($query);
	}
	
	function getCategoryById($id)
	{
		$query = "select * from Categories where Id ='" . $this->fix_for_mysql($id) . "'";
		return $this->query( $query );
	}

	function addCategory($name,$userId)
	{
		$ownerId = abs($userId);
		$query = "insert into Categories (Name,OwnerId) VALUES  ('" . $this->fix_for_mysql($name) . "', " . $this->fix_for_mysql($ownerId) . " );";
		return $this->query($query);
	}

	function deleteCategory($id)
	{
		$query = "delete from Categories where ID = " . $this->fix_for_mysql($id) . ";";
		if ( $this->query($query) )
		{
			$query = "update Names set CategoryId = NULL where CategoryId = " . $this->fix_for_mysql($id) . ";";
			$this->query($query);
		}
		else
		{
			return false;
		}		
	}
}

?>