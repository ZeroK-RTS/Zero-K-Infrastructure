'use strict'

var React = require('react');
var ReactDOM = require('react-dom');

require('style/main.sass');
var Home = require('comp/Home.jsx');

var server = new (require('weblobby/store/LobbyServer.js'))();
var process = new (require('weblobby/store/Process.js'))();
var chat = new (require('weblobby/store/Chat.js'))(server, process);

ReactDOM.render(<Home
	serverStore={server}
	chatStore={chat}
/>, document.getElementById('main'));
