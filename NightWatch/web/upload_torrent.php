<!-- <html>
  <head>
    <title>Upload map torrent</title>
  </head>
  <body>
    <form action="upload_torrent.php" method="post" enctype="multipart/form-data">
      <input type="file" name="fupload">
      <input type="submit" value="Nahrát">
    </form>
  </body>
</html>
 -->
<?php
if (isset($_FILES['fupload']))
  {
	
		$slozka = "./torrents"; 
    $cil = $slozka . "/" .$_FILES['fupload']['name']; 
    $nazev_souboru = $_FILES['fupload']['tmp_name']; 
		$copy = move_uploaded_file($nazev_souboru, $cil) 
      or die ("File upload failed"); 
		chmod ($cil, 0644); 
  }
?>