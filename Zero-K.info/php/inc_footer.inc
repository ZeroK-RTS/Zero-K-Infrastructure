<div id="footer" class="border">
	
	<span class="abs left cdark">Site by maackey</span> 
	<div class="center">
		<a href="#credlist" onclick="show('credits')" class="button">Credits</a> | 
		<a>Contact</a> | 
		<a>Terms of Use</a> | 
		<a href="#copyright" onclick="show('copyright')" class="button">Copyright</a>
	</div>
	
	<!-- Legal
	-->
</div><!close footer>

	<script type="text/javascript">
		function hide(a){document.getElementById(a).style.display="none"}
		function show(a){document.getElementById(a).style.display="block"}
	</script>
	
	<div id="copyright" class="border hide blocker">
		<a id="copyright">copyright stuff:</a>
		<p>CA uses artwork (models, textures) donated from various sources under open-source licenses, particularly the <a href="">GNU GPL</a> and the various <a href="">Creative Commons</a> licenses. These may be copied and edited and used in derivative works according to the terms of their respective licenses. CA also uses Public Domain (PD) material.</p>
		<p>The main project and all Lua code contained within is released under the terms of the GNU General Public License.</p>
		<a onclick="hide('copyright')" class="button">close</a>
	</div><!close copyright>
	
	<div id="credits" class="hide blocker border">
		<a id="credlist" target="_new" href="credits.txt">Full list of Credits</a>
		<marquee height="650px" direction="up" scrollamount="3">
		<pre class="credits"><?php include("credits.txt"); ?></pre>
		</marquee>
		<a onclick="hide('credits')" class="button">close</a>
	</div><!close credits>
	
	<div id="terms" class="border hide">
		terms of use
	</div><!close terms>
	
	<div id="contact" class="border hide">
		contact us
	</div><!close contact>
</div><!close blocker>
