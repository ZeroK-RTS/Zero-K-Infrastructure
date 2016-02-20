'use strict'

// We require Applet before anything that uses localStorage, otherwise init()
// won't set localStorage database path properly.
var Applet = require('weblobby/store/Applet.js');

require('style/main.sass');

var _ = require('lodash');
var React = require('react');
var ReactDOM = require('react-dom');
var App = require('weblobby/comp/App.jsx');

// Create stores.
var gameInfo = new (require('weblobby/store/GameInfo.js'))();
var process = new (require('weblobby/store/Process.js'))(gameInfo);
var sound = new (require('weblobby/store/Sound.js'))();
var lobbyServer = new (require('weblobby/store/LobbyServer.js'))();
var chatStore = new (require('weblobby/store/Chat.js'))(lobbyServer, process);
var afkStatus = new (require('weblobby/store/AfkStatus.js'))(lobbyServer, process);
var currentBattle = new (require('weblobby/store/CurrentBattle.js'))(gameInfo,
	lobbyServer, chatStore, process);

// Back when swl API was a Java applet this function was introduced to avoid
// deadlocks on OpenJDK. It's kept to avoid breaking changes.
window.__java_js_wrapper = _.defer;

window.echo = function(){
	console.log.apply(console, arguments ); //chrome has issue with direct assigning of this function
}

// Disable the default context menu on most things if running in the wrapper.
Applet && document.addEventListener('contextmenu', function(evt){
	if (evt.button === 2 && !(evt.target instanceof HTMLAnchorElement) &&
			!(evt.target instanceof HTMLInputElement)) {
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
