<div id="terratypes" class="border">
	<p>All construction units and Nano Turrets can modify the terrain in Complete Annihilation. Terraforming costs energy, metal and time.
	<h2 id="TerraformTypes">Terraform Types</h2>
	<p>There are 5 terraform commands:
	<ul>
		<li>Ramp
		<li>Level
		<li>Raise
		<li>Smooth
		<li>Restore
	</ul>
<p>Note: <i>Right Click</i> at any time during the terraform will cancel the command (so will mousewheel movement, that's a bug though).
<p>Make sure you have the widget called "Lasso Terraform GUI" enabled or you will not be able to use Terraform.
</div><!close terratypes>

<div id="lasso" class="border">
	<h2 id="Interface:LassoSelection">Interface: Lasso Selection</h2>
	<p>Level, Raise, Smooth and Restore have the same interface for selecting the effected area. They differ in what they do to the terrain.
	<a style="padding:0; border:none" href="http://trac.caspring.org/attachment/wiki/Terraforming/other%20orders.png"><img class="border" src="http://trac.caspring.org/raw-attachment/wiki/Terraforming/other%20orders.png" /></a>
	<p><i>Left click</i> on the ground and hold the mouse to start drawing a line.  Drawing the area selects the area to be terraformed. If the end of the line is close to the start when the left mouse button is released the area inside the loop will be terraformed (like lasso selection), if the ends are far away the drawn line will be terraformed (good for making walls).
	<p><a style="padding:0; border:none" href="http://trac.caspring.org/attachment/wiki/Terraforming/lassoandarea.PNG"><img class="border" src="http://trac.caspring.org/raw-attachment/wiki/Terraforming/lassoandarea.PNG" /></a>
	<a style="padding:0; border:none" href="http://trac.caspring.org/attachment/wiki/Terraforming/chooseheight.png"><img class="border left" src="http://trac.caspring.org/raw-attachment/wiki/Terraforming/chooseheight.png" /></a>
	<p>If you are using Smooth or Restore the command will be given when you release the mouse. If you are using Level or Raise you will have to chose either which height to level to or how much to raise the land. This is done by moving the mouse up/down then clicking when it looks like it is at the desired height.
	<hr />
</div><!close lasso>

<div id="effects" class="border">
	<h3 id="EffectsofTerraformandModifiers">Effects of Terraform and Modifiers</h3><p>
	<p><a style="padding:0; border:none" href="http://trac.caspring.org/attachment/wiki/Terraforming/levelcliff.png"><img class="border left" src="http://trac.caspring.org/raw-attachment/wiki/Terraforming/levelcliff.png" /></a>
	<p>Here are the effects of Level, Raise, Smooth and Restore. The modifiers <i>Alt</i> and <i>Ctrl</i> can be held during the Height Choosing phase of the Level and Raise commands for more height choosing control.
	<p><strong>Level</strong> will move the terrain in the area to exactly the same height, the result terrain will be flat. For example a hill could be flattened.
	<p>Hold <i>Alt</i> to snap to either the starting height, just below water level (so that ships can't pass) or the level just below water level that land units cannot pass (23 deep).
	<p>Hold <i>Ctrl</i> and move the mouse to select the height to be levelled to from terrain by mousing over the terrain.
	<hr />
	<a style="padding:0; border:none" href="http://trac.caspring.org/attachment/wiki/Terraforming/raisecliff.png"><img class="border left" src="http://trac.caspring.org/raw-attachment/wiki/Terraforming/raisecliff.png" /></a>
	<p>Tap <i>Space</i> to cycle through only raise or only level. This limits the terraform to either take away or add terrain instead of a mix of both. Can be toggled during lasso stage.
	<p><strong>Raise</strong> raises or lowers the terrain in the area by the same amount, the relative height of the terrain within the area is not changed.
	<p>Hold <i>Alt</i> to snap to a grid of 30. A 30 high (or deep) wall (or trench) will be impassable to vehicles but passable to bots. A 60 high wall will be impassable to everything except spiders.
	<p>Press <i>Ctrl</i> to reset the height to 0.
	<hr />
	<a style="padding:0; border:none" href="http://trac.caspring.org/attachment/wiki/Terraforming/smooth.PNG"><img class="border left" src="http://trac.caspring.org/raw-attachment/wiki/Terraforming/smooth.PNG" /></a>
	<p><strong>Smooth</strong> smooths the terrain, cliffs become less sharp.
	<hr />
	<a style="padding:0; border:none" href="http://trac.caspring.org/attachment/wiki/Terraforming/restore.PNG"><img class="border left" src="http://trac.caspring.org/raw-attachment/wiki/Terraforming/restore.PNG" /></a>
	<p><strong>Restore</strong> reverts the height of the terrain to what it was at the start of the game.
	<p><p>Tap <i>Space</i> to cycle through only raise or only level. This limits the terraform to either take away or add terrain instead of a mix of both. Toggle during lasso stage.
	<hr />
</div><!close effects>

<div id="advanced" class="border">
	<h2 id="AdvancedAreaSelection">"Advanced" Area Selection</h2>
	<p>For easier terraforming, alt and/or ctrl can be used to modify the way the lasso command is given.
	<p>Hold <i>Alt</i> while clicking to terraform a rectangular area.
	<p>	<a style="padding:0; border:none" href="http://trac.caspring.org/attachment/wiki/Terraforming/rectangle.png"><img class="border" src="http://trac.caspring.org/raw-attachment/wiki/Terraforming/rectangle.png" /></a>
	<p>	<p>Hold <i>Alt</i> and click on a building or unit to create a wall around that unit.
	<p>	<a style="padding:0; border:none" href="http://trac.caspring.org/attachment/wiki/Terraforming/buildingterra.png"><img class="border" src="http://trac.caspring.org/raw-attachment/wiki/Terraforming/buildingterra.png" /></a>
	<p>	<p>Hold <i>Alt</i> + <i>Ctrl</i> while clicking to create a hollow rectangle of terraform
	<p>
	<a style="padding:0; border:none" href="http://trac.caspring.org/attachment/wiki/Terraforming/hollowrectangle.png"><img class="border" src="http://trac.caspring.org/raw-attachment/wiki/Terraforming/hollowrectangle.png" /></a>
	<p>	<p>Hold <i>Ctrl</i> at any time during line drawing to create a straight section of line. Straight an non-straight line can be mixed by presing and releaing Ctrl.
	<p>	<a style="padding:0; border:none" href="http://trac.caspring.org/attachment/wiki/Terraforming/straightcurve.png"><img class="border" src="http://trac.caspring.org/raw-attachment/wiki/Terraforming/straightcurve.png" /></a>
	<p></div><!close advanced>

<div id="ramp" class="border">
	<h3 id="Ramp">Ramp</h3><p>
	<p>	The ramp command interface is different from that of the other terraform commands.
	<p>	<a style="padding:0; border:none" href="http://trac.caspring.org/attachment/wiki/Terraforming/ramps.png"><img class="border" src="http://trac.caspring.org/raw-attachment/wiki/Terraforming/ramps.png" /></a>
	<p>
	First select the Ramp command from the command menu.
	<p>	<a style="padding:0; border:none" href="http://trac.caspring.org/attachment/wiki/Terraforming/ramporder.png"><img class="border" src="http://trac.caspring.org/raw-attachment/wiki/Terraforming/ramporder.png" /></a>
	<p>	<a style="padding:0; border:none" href="http://trac.caspring.org/attachment/wiki/Terraforming/startramp.png"><img class="border right" src="http://trac.caspring.org/raw-attachment/wiki/Terraforming/startramp.png" /></a>
	<i>Left Click</i> on the ground to set the start of the ramp. <strong>If</strong> you want to choose the height of the start of the ramp hold the click and move the mouse up or down. A little green line will appear out of the ground to indicate the height. The default height is the ground height, release the mouse to select the height.
	<p>	<p>Hold <strong>Alt</strong> when choosing the start height to snap to the original ground height or just below water level(helpful for getting hovers out of the water).
	<p>
	Then <i>Left Click</i> on the ground to set the end of the ramp. As with the start of the ramp, you can hold the mouse and move to choose the height. 
	<p>Hold <strong>Alt</strong> when choosing the end height to snap the gradient of the ramp to either upwards bot pathable, upwards vehicle pathable, flat, downwards vehicle pathable, downwards  bot pathable, the original ground height or just below water level .
	<p>	<a style="padding:0; border:none" href="http://trac.caspring.org/attachment/wiki/Terraforming/rampend.png"><img class="border" src="http://trac.caspring.org/raw-attachment/wiki/Terraforming/rampend.png" /></a>
	<p>	Move the mouse up/down (not held down) and click to set the width of the ramp.
	<p>The ramp colour indicates whether all units will be able to use it, if vehicles/tanks will not be able to use it and if only Spiders can use it.
	<p>
	<a style="padding:0; border:none" href="http://trac.caspring.org/attachment/wiki/Terraforming/rampgradient.png"><img class="border" src="http://trac.caspring.org/raw-attachment/wiki/Terraforming/rampgradient.png" /></a>
	<p>	<p>Optional: Tap <i>Space</i> to cycle through only raise or only level. This limits the terraform to either take away or add terrain instead of a mix of both. Can be toggled at any stage during the ramp command.
</div><!close ramp>

<div id="tips" class="border">
	<h3 id="Cost">Cost</h3><p>
	<p>	Terraform by default costs 1 metal, energy and build power to raise or lower 1 heightmap grid, each vertex of the blue grid maps onto a point on the heightmap grid. Due to the sloped sides it will be costly to make a very high spire. Equal volumes of terraform will have the same cost regardless of shape. Cost can be changed with the "terraform cost multiplier" <a class="missing wiki">ModOption?</a>.
	<h3 id="Tips">Tips</h3><p>
	<p>	Buildings cannot be moved vertically. If you want to put a Fusion in a hole for safety the hole will have to be built before the Fusion. 
	<p>Because buildings cannot move vertically they can be used to stop walls being terraformed by the enemy.
	<p>	<a style="padding:0; border:none" href="http://trac.caspring.org/attachment/wiki/Terraforming/reinforcedWall.png"><img class="border" src="http://trac.caspring.org/raw-attachment/wiki/Terraforming/reinforcedWall.png" /></a>
	<p>	Big Berthas, Intimidators and Crabes are very good at knocking down walls.
	<p>Terraform ramp can be used with newtons to fire units at the enemy! 
	<p>	<a style="padding:0; border:none" href="http://trac.caspring.org/attachment/wiki/Terraforming/unitCannon.PNG"><img class="border" src="http://trac.caspring.org/raw-attachment/wiki/Terraforming/unitCannon.PNG" /></a>
	<p>	<p>Terraform commands can be hotkeyed with <b>uikeys.txt</b> (found in Spring folder). Add "bind (keys) (action)" to the end of uikeys.txt. The actions for terraform are: rampground, levelground, raiseground, smoothground and restoreground
	<p>
	<p>For example to bind level to ctrl w: "bind ctrl+w levelground"
	<p>For example to bind raise to p: "bind any+p raiseground"
</div><!close tips>
