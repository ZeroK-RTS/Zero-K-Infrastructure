<div id="main">

<div id="logo"><a href="/"><img src="img/logo_bt.png" width="800" alt="Zero-K"></img></a></div>

<div id="menu"> <!-- This is the css dropdown menu at the top. If you want to add a funny quote or something, they go inside the <span> tags after the text-->
<!-- I *should* also convert the tables to some css stuff, but unless I get a lot of requests for varying stylesheets I'll just leave this for now because its easier -->
	<table>
	<tr>
	<td><ul class="menu">
	<li><a href="download.php">Download</a>
		<ul>
			<li><a href="download.php">Windows</a></li>
			<li><a href="download.php">Linux<span>This will now be the only game you ever play</span></a></li>
			<li><a href="download.php">Mac</a></li>
			<li><a href="http://trac.caspring.org/wiki/SubversionRepository">Sources<span>You awesome, intelligent man-beast, you...</span></a></li>
		</ul>
	</li>
	</ul> </td> 

	<td><ul class="menu">
	<li><a href="http://licho.eu/gallery/ca">Media</a>
		<ul>
			<li><a href="screen.php">Screenshots<span>Take a picture, it lasts longer</span></a></li>
			<li><a href="video.php">Video<span>A video says a thousand pictures</span></a></li>
			<li><a href="http://trac.caspring.org/wiki/ConceptArt">Concept Art</a></li>
			<li><a href="screen.php">Wallpapers<span>When busty, sexy, hawt women just aren't good enough</span></a></li>
		</ul>
	</li>
	</ul> </td>
	
	<td><ul class="menu">
	<li><a href="http://springinfo.info/guides/?p=195">Guide</a>
		<ul>
			<li><a href="http://trac.caspring.org/wiki/CaIntro">Introduction<span>Don't know where to begin? Click here.</span></a></li>
			<li><a href="http://trac.caspring.org/wiki/UnitGuide#LogosformerlyCORE">Logos Units<span>Bwahaha! Crush those lousy Nova!</span></a></li>
			<li><a href="http://trac.caspring.org/wiki/UnitGuide#NovaformerlyARM">Nova Units<span>Bwahaha! Crush those lousy Logos!</span></a></li>
			<li><a href="http://springinfo.info/guides/?p=195">Economy</a></li>
		</ul>
	</li>
	</ul> </td> 
	
	<td><a href="http://springrts.com/wiki/Complete_Annihilation">Wiki</a></td>

	<td><a href="http://springrts.com/phpbb/viewforum.php?f=42">Forums</a></td> 
	
	<td><ul class="menu">
	<li><a href="http://trac.caspring.org/wiki">Trac</a>
		<ul>
			<li><a href="http://trac.caspring.org/wiki">Artwork<span>We will take pretty much anything at the moment D:</span></a></li>
			<li><a href="http://trac.caspring.org/newticket">Bugs?<span>Let us know so we can <b>squash them!</b></span></a></li>
			<li><a href="http://trac.caspring.org/wiki">Code</a></li>
		</ul>
	</li>
	</ul> </td> 
	<td width="100%">
	<!-- search bar, and possibly a login/register input as well -->
	<form method="post" action="">
		<table>
		<tr>
			<td><a href="">Login</a></td> <!--not sure if i want the login to be next to the searchbar or the menu-->
			<!--login will replace the searchbar when clicked (ala moddb)
			<input type="text" size="9" name="nick" id="nick" value="Nickname" onClick="clearinput('nick');" />
			<input type="text" size="9" name="pass" id="pass" value="Password" onClick="clearinput('pass');" />
			-->
			<td width="100%" align="right"><input type="text" name="search" id="search" onclick="clearinput('search');" value="Search" /></td>
			<td><input type="submit" value="Q" /></td>
		</tr>
		</table>
	</form>
	</td>
</tr>
</table>
</div> <!-- close menu -->
