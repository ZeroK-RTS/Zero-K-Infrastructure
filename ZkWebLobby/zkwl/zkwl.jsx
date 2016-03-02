'use strict'

require('index.html');
require('style/main.sass');

var _ = require('lodash');
var React = require('react');
var ReactDOM = require('react-dom');
var App = require('weblobby/comp/App.jsx');
var Zkl = require('store/Zkl.js');
var Settings = require('store/Settings.js');

// Disable the default context menu on most things if running in the wrapper.
Zkl && document.addEventListener('contextmenu', function(evt){
	if (evt.button === 2 && !(evt.target instanceof HTMLInputElement)) {
		evt.preventDefault();
	}
});

window.echo = console.log.bind(console); // faster to write than console.log

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
var pastebin = new (require('weblobby/store/LogPastebin.js'))();

var runApp = _.once(function(){
	ReactDOM.render(<App
		serverStore={lobbyServer}
		gameInfoStore={gameInfo}
		processStore={process}
		chatStore={chatStore}
		currentBattleStore={currentBattle}
		logPastebinStore={pastebin}
	/>, document.getElementById('main'));

	var overlay = document.getElementById('loadingOverlay');
	overlay.parentNode.removeChild(overlay);
});

var introVideo = document.getElementById('introVideo');

var killIntro = _.once(function(){
	introVideo.parentNode.removeChild(introVideo);
});
introVideo.addEventListener('ended', killIntro);
introVideo.addEventListener('click', killIntro);
introVideo.addEventListener('playing', runApp);
// Give the video time to load. I would use canplay/canplaythrough/suspend if
// Chrome deigned to actually fire them.
setTimeout(function(){ introVideo.play(); }, 800);

var audioTime = function audioTime(){
	if (musicPlaylist.audio.currentTime > 8.5) {
		killIntro();
		musicPlaylist.audio.removeEventListener(audioTime);
	}
};
musicPlaylist.audio.addEventListener('timeupdate', audioTime);

// Fallback in case media failed to play.
setTimeout(runApp, 15000);
setTimeout(killIntro, 15000);
