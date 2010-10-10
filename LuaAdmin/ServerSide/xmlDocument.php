<?php

class xmlDocument
{
	var $document = false;
	var $root = false;
	var $xml = false;
	
  function __construct() {
		
	
	}

  function __destruct() {
  }
  
  function createDocument( $dtdText, $rootName )
  {
  	if ( $document != false )
  	{
  		die("ERROR: XML Document already created! Use a new one!");
  	}
  	
  	if ( $dtdText != false )
  	{
			$dtd = DOMImplementation::createDocumentType($dtdText);		
				// create a new XML document
			$this->document = DOMImplementation::createDocument( "", "", $dtd ); //'1.0');
  	}
  	else
  	{
  		$this->document = DOMImplementation::createDocument( ); //'1.0');
  	}
  	
  	// create root node
		$this->root = $this->document->createElement($rootName);
		$this->root = $this->document->appendChild($this->root);
		
//		$this->document->doctype = new DOMDocumentType;
  }

	////////////////////////////////////////
	//PUBLIC /////////////////////////////////////////////////
	////////////////////////////////////////

	function createWidgets()
	{
		$this->createDocument( "root [<!ELEMENT Widget ANY><!ATTLIST Widget ID ID #REQUIRED>]", "root" );
	}
	
	function createFiles()
	{
		$this->createDocument( "root [<!ELEMENT File ANY><!ATTLIST File ID ID #REQUIRED>]", "root" );
	}
	
	function createMods()
	{
		$this->createDocument( "root [<!ELEMENT Mod ANY><!ATTLIST Mod ID ID #REQUIRED>]", "root" );
	}
	
	function createModWidgets()
	{
		$this->createDocument( "root [<!ELEMENT ModWidget ANY><!ATTLIST ModWidget ID ID #REQUIRED>]", "root" );
	}
	
	function createCategories()
	{
		$this->createDocument( "root [<!ELEMENT Category ANY><!ATTLIST Category ID ID #REQUIRED>]", "root" );
	}
	
	function createRatings()
	{
		$this->createDocument( "root [<!ELEMENT Rating ANY><!ATTLIST Rating ID ID #REQUIRED>]", "root" );
	}
	
	function createComments()
	{
		$this->createDocument( "root [<!ELEMENT Comment ANY>]", "root" );
	}
	
	function createInstallations()
	{
		$this->createDocument( "root [<!ELEMENT Installations ANY>]", "root" );
	}
	
	function createActivations()
	{
		$this->createDocument( "root [<!ELEMENT Activations ANY>]", "root" );
	}
	
	function addMysqlResult( $dbresult, $entityName )
	{
		if ( $this->document == false )
		{
			die("ERROR: XML Document has to be created first!");
		}
		// process one row at a time
		while($row = mysql_fetch_assoc($dbresult)) {
			// add node for each row
		  $occ = $this->document->createElement($entityName);
		  $occ = $this->root->appendChild($occ);

		  // add a child node for each field
		  foreach ($row as $fieldname => $fieldvalue) {
				if ( strtoupper($fieldname) == "ID" )
				{
					$attr = $this->document->createAttribute("ID");
					$attr = $occ->appendChild( $attr );
					$occ->setIdAttribute("ID", true );
					
					
			    $value = $this->document->createTextNode($fieldvalue);
			    $value = $attr->appendChild($value);
				}
				else
				{
			    $child = $this->document->createElement($fieldname);
			    $child = $occ->appendChild($child);
							
			    $value = $this->document->createTextNode($fieldvalue);
			    $value = $child->appendChild($value);
				}
		  } // foreach
		} // while
	}
	
	function getXml()
	{
	//	$xml = "<!DOCTYPE " . "root" . " [<!ATTLIST Widget ID ID #REQUIRED>]>";
		////echo "Generating XML...";
		// get completed xml document
		return $this->document->saveXML();
	}
}


?>