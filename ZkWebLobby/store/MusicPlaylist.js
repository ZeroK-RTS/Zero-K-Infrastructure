'use strict'

var _ = require('lodash');
var Reflux = require('reflux');
var Settings = require('weblobby/store/Settings.js');

var menuTheme = require('sounds/Rise of the Machines.ogg');

module.exports = function(process){ return Reflux.createStore({
	init: function(){
		this.audio = new Audio(menuTheme);
		this.audio.loop = true;
		this.listenTo(process, this.updateProcess, this.updateProcess);
		this.listenTo(Settings, this.settingChanged);
	},
	updateProcess: function(state){
		if (state.springRunning && !this.audio.paused)
			this.stop();
		else if (!state.springRunning && this.audio.paused && Settings.playTitleMusic)
			this.play();
	},
	settingChanged: function(key){
		if (key == 'playTitleMusic') {
			if (Settings.playTitleMusic)
				this.play();
			else
				this.stop();
		}
	},
	play: function(){
		// Restart from the beginning, sudden unpausing would be jarring.
		this.audio.load(menuTheme);
		this.audio.play();
	},
	stop: function(){
		this.audio.pause();
	},
})};
