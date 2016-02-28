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

// Don't run until intro music and video are fully loaded.
var mediaReady = 0;
var introVideo = document.getElementById('introVideo');
var titleMusic = musicPlaylist.audio;

var killIntro = _.once(function(){
	introVideo.parentNode.removeChild(introVideo);
});
introVideo.addEventListener('ended', killIntro);
introVideo.addEventListener('click', killIntro);

var playMedia = function playMedia(){

	mediaReady++;
	if (mediaReady < 2)
		return;

	titleMusic.removeEventListener('suspend', playMedia);
	introVideo.play();
	Settings.playTitleMusic && titleMusic.play();

	// A hack to make it cut to the menu at the right place in the title song.
	// Make it actually track how far into the song it is.
	setTimeout(killIntro, 9200);

	// Only render the main app after the video started playing or it may flicker still.
	setTimeout(runApp, 500);
}

// Fallback in case media failed to play.
setTimeout(runApp, 15000);
setTimeout(killIntro, 15000);

titleMusic.addEventListener('suspend', playMedia);
introVideo.addEventListener('suspend', playMedia);
