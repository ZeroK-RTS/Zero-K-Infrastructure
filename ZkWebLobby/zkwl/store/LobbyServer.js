'use strict'

var _ = require('lodash');
var Reflux = require('reflux');
var Zkl = require('store/Zkl.js');
var Log = require('act/Log.js');
var Settings = require('store/Settings.js');
var ServerCommon = require('store/LobbyServerCommon.js');

var SpringLobbyServer = require('store/SpringLobbyServer.js');
var ZkLobbyServer = require('store/ZkLobbyServer.js');

module.exports = function(){ return Reflux.createStore({

	listenables: require('act/LobbyServer.js'),

	init: function(){
		this.underlyingStore = null;
		this.state = ServerCommon.getClearState();

		window.on_lobby_message = this.message;
		window.on_connection_closed = function(requested){
			this.state.connection = ServerCommon.ConnectionState.DISCONNECTED;
			this.triggerSync();
		}

		if (Settings.autoConnect && Settings.name && Settings.password)
			this.connect();
	},
	getInitialState: function(){
		return this.underlyingStore && this.underlyingStore.getInitialState() ||
			this.state;
	},
	triggerSync: function(){
		this.trigger(this.state);
	},

	underlyingStoreUpdate: function(state){
		this.state = state;
		this.triggerSync();
	},

	// Action listeners.

	connect: function(){
		this.connectInternal(null);
	},
	disconnect: function(){
		Zkl.disconnect();
		this.state.connection = ServerCommon.ConnectionState.DISCONNECTED;
		if (this.underlyingStore) {
			this.stopListeningTo(this.underlyingStore);
			this.underlyingStore.dispose();
			this.underlyingStore = null;
		}
		this.triggerSync();
	},
	register: function(name, password, email){
		if (this.state.connection !== ServerCommon.ConnectionState.DISCONNECTED)
			this.disconnect();
		this.connectInternal({ name: name, password: password, email: email });
	},
	sendRaw: function(data){
		Zkl.sendLobbyMessage(data);
	},

	// Other methods.

	connectInternal: function(registering){
		if (this.state.connection !== ServerCommon.ConnectionState.DISCONNECTED)
			this.disconnect();

		var host = Settings.lobbyServer.split(':')[0] ||
			Settings.useZkServer && 'lobby.zero-k.info' || 'lobby.springrts.com';
		var port = Settings.lobbyServer.split(':')[1] || '8200';
		Zkl.connect(host, port);

		this.state = ServerCommon.getClearState();
		this.state.socket = this.socket;
		this.state.connection = ServerCommon.ConnectionState.CONNECTING;
		this.state.registering = registering;
		this.triggerSync();
	},
	message: function(msg){
		if (this.underlyingStore === null) {
			if (msg.match(/^TASServer/)) {
				this.underlyingStore = new SpringLobbyServer();
			} else if (msg.match(/^Welcome {/)) {
				this.underlyingStore = new ZkLobbyServer();
			} else {
				Log.errorBox('Unsupported server protocol\nUnrecognized welcome message: ' + msg);
				this.disconnect();
				return;
			}

			_.extend(this.underlyingStore, this.state);
			this.listenTo(this.underlyingStore, this.underlyingStoreUpdate);
		}
		this.underlyingStore.message(msg);
	},
})};
