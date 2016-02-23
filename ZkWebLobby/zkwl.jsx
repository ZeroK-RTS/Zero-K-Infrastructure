'use strict'

require('index.html');
require('style/main.sass');

var _ = require('lodash');
var React = require('react');
var ReactDOM = require('react-dom');
var App = require('weblobby/comp/App.jsx');
var Zkl = require('store/Zkl.js');

document.addEventListener('DOMContentLoaded', function(){

	// Create stores.
	var gameInfo = new (require('store/ZkGameInfo.js'))();
	var process = new (require('store/ZkProcess.js'))(gameInfo);
	var sound = new (require('weblobby/store/Sound.js'))();
	var lobbyServer = new (require('weblobby/store/LobbyServer.js'))();
	var chatStore = new (require('weblobby/store/Chat.js'))(lobbyServer, process);
	var afkStatus = new (require('weblobby/store/AfkStatus.js'))(lobbyServer, process);
	var currentBattle = new (require('weblobby/store/CurrentBattle.js'))(gameInfo,
		lobbyServer, chatStore, process);
	var musicPlaylist = new (require('store/MusicPlaylist.js'))(process);

	// Disable the default context menu on most things if running in the wrapper.
	Zkl && document.addEventListener('contextmenu', function(evt){
		if (evt.button === 2 && !(evt.target instanceof HTMLInputElement)) {
			evt.preventDefault();
		}
	});

	var introVideo = document.getElementById('introVideo');
	var killIntro = function(){
		introVideo.parentNode && introVideo.parentNode.removeChild(introVideo);
	};
	introVideo.addEventListener('ended', killIntro);
	introVideo.addEventListener('click', killIntro);
	// A hack to make it cut to the menu at the right place in the title song.
	// Make it actually track how far into the song it is.
	setTimeout(killIntro, 9800);

	var overlay = document.getElementById('loadingOverlay');
	overlay.parentNode.removeChild(overlay);

	// Only render the main app after the video started playing or it may flicker still.
	setTimeout(function(){
		ReactDOM.render(<App
			serverStore={lobbyServer}
			gameInfoStore={gameInfo}
			processStore={process}
			chatStore={chatStore}
			currentBattleStore={currentBattle}
		/>, document.getElementById('main'));
	}, 500);
});

window.echo = function(){
	console.log.apply(console, arguments ); //chrome has issue with direct assigning of this function
}
