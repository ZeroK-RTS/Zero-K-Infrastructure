<div id="footer" class="mid">
	<center>
	| 
	<a href="javascript:SwitchContent('credits','contact','terms','copy')">Credits</a> | 
	<a href="javascript:SwitchContent('contact','terms','copy','credits')">Contact</a> | 
	<a href="javascript:SwitchContent('terms','contact','copy','credits')">Terms of Use</a> | 
	<a href="javascript:SwitchContent('copy','contact','terms','credits')">Copyright</a> | 
	<br /><div id="showme" style="display: none;"></div><br /><div id="hideme" style="display: static;">You need javascript to enable the menu switching. (insert link to copyright/terms here)</div>
	</center>

	<div id="credits" style="display: none;">
		<h1>Credits:</h1>
		<!--needs to be fleshed out quite a bit. If you see your name and want to change or add something (like a link to your own personal website as your name), do it.-->
		<h2><a href="javascript:SwitchContent('active','inactive','contrib','')">Active Developers:</a></h2>		
		<div id="active">
			<p><b>maackey</b> - web design, artwork, models</p>
			<p><b>Licho</b> - hosting, master coder</p>
			<p><b>MidKnight</b> - crazy ideas, artwork, models</p>
			<p><b>CarRepairer</b> - PHP guru, GUI</p>
			<p><b>quantum</b></p>
			<p><b>Saktoth</b></p>
			<p><b>jK</b></p>
			<p><b>KingRaptor</b></p>
			<p><b>Google Frog</b></p>
			<p><b>det</b></p>
			<p><b>Lurker </b></p>
		</div><!-- close active -->
	
		<h2><a href="javascript:SwitchContent('inactive','active','contrib','')">Inactive Developers:</a></h2>
		<div id="inactive" style="display: none;">
			<p><b>Inactive ppl</b> - they go here</p>
			
			
		</div><!-- close inactive -->
	
		<h3><a href="javascript:SwitchContent('contrib','inactive','active','')">Contributers:</a></h3>
		<div id="contrib" style="display: none;">
			<p><b>Contributers</b> - they go here</p>
		</div><!-- close contrib -->
	</div> <!-- close credits -->
	
	<div id="contact" style="display: none;">
	<form method="post" action="feedback.php">
		<table>
			<tr><td>Name:</td><td><input type="text" name="user" id="user" onclick="clearinput('user')" value="Name"/></td></tr>
			<tr><td>Email:</td><td><input type="text" name="email" id="email" onclick="clearinput('email')" value="joe@email.com" /></td></tr>
		</table><br />
		<center>
		<input type="radio" checked="checked" name="type" value="comment" /> Comment |
		<input type="radio" name="type" value="bug" /> Bug | 
		<input type="radio" name="type" value="suggestion" /> Suggestion | 
		<input type="radio" name="type" value="other" /> Other
		
		<textarea rows="6" cols="42" name="comment" id="comment" onclick="clearinput('comment')"></textarea>
		<br />
		
		<?php
		require_once('recaptchalib.php');
		$publickey = "123456789123456789"; //needs replacing with real key
		echo recaptcha_get_html($publickey);
		?>
		
		<br />
		<input type="submit" value="Submit" />
		</center>
	</form>
	</div><!-- close contact -->
	
	
	<div id="terms" style="display: none;">
		TERMS OF SERVICE BOOYAH
	</div> <!-- close terms -->
	
	
	<div id="copy" style="display: none;">
		<p>CA uses artwork (models, textures) donated from various sources under open-source licenses, particularly the <a href="">GNU GPL</a> and the various <a href="">Creative Commons</a> licenses. These may be copied and edited and used in derivative works according to the terms of their respective licenses. CA also uses Public Domain (PD) material.</p>
		<p>The main project and all Lua code contained within is released under the terms of the GNU General Public License.</p>
	</div> <!-- close terms -->
	
	<br />
	<center><small> Site design by maackey </small></center>
</div> <!-- close footer -->

</div> <!-- close main -->
	
</body>
</html>
