'use strict'

require('style/main.sass');

var _ = require('lodash');
var React = require('react');
var ReactDOM = require('react-dom');
var App = require('weblobby/comp/App.jsx');

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

window.echo = function(){
	console.log.apply(console, arguments ); //chrome has issue with direct assigning of this function
}

// Disable the default context menu on most things if running in the wrapper.
CefWrapperAPI && document.addEventListener('contextmenu', function(evt){
	if (evt.button === 2 && !(evt.target instanceof HTMLInputElement)) {
		evt.preventDefault();
	}
});

ReactDOM.render(<App
	serverStore={lobbyServer}
	gameInfoStore={gameInfo}
	processStore={process}
	chatStore={chatStore}
	currentBattleStore={currentBattle}
/>, document.getElementById('main'));
