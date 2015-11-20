'use strict'

require('style/Home.sass');
var React = require('react');
var classNames = require('classnames');
var Chat = require('weblobby/comp/Chat.jsx');

module.exports = React.createClass({
	displayName: 'Home',
	getInitialState: function(){
		return {
			currentContent: 'newsfeed',
			chatOpen: false,
			chatDisplay: false
		}
	},
	handleChat: function(){
		// We have to do this because if we do the first render with display: block
		// on the chat div, the entire window will be scrolled to the right where it's
		// supposed to be hidden. If we set display: none in .show, transitions won't work.
		if (!this.state.chatDisplay) {
			this.setState({ chatDisplay: true }, function(){
				this.setState({ chatOpen: true });
			});
		} else {
			this.setState({ chatOpen: !this.state.chatOpen });
		}
		
		new Audio(`sounds/panel_move.wav`).play();
	},
	handleMenu: function(what){
		this.setState({ currentContent: what });
		new Audio(`sounds/button_click.wav`).play();
	},
	renderContent: function(){
		switch (this.state.currentContent) {
			case 'newsfeed':
				return <h1>[newsfeed]</h1>;
			case 'skirmish':
				return <h1>[skirmish]</h1>;
			case 'missions':
				return <h1>[missions]</h1>;
		}
	},
	render: function(){
		return <div id="zkHomeScreen">
			<div id="mainMenu">
				<button>FIND MATCH</button>
				<button className="underbutton">Custom battles</button>
				<button onClick={_.partial(this.handleMenu, 'skirmish')}>SKIRMISH</button>
				<button onClick={_.partial(this.handleMenu, 'missions')}>MISSIONS</button>
			</div>
			<div id="content">{this.renderContent()}</div>
			<button id="chatButton" onClick={this.handleChat}>Chat</button>
			<div className={classNames({
				chatDiv: true,
				show: this.state.chatOpen,
				display: this.state.chatDisplay
			})}>
				<Chat
					serverStore={this.props.serverStore}
					chatStore={this.props.chatStore}
				/>
			</div>
		</div>;
	}
});
