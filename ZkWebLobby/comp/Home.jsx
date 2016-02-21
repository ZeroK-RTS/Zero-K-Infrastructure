'use strict'

require('style/Home.sass');
var _ = require('lodash');
var React = require('react');
// 'weblobby/act/Battle.js' fails because webpack considers it a different module caching-wise.
// Having to omit weblobby/ is inconsistent, find a way to deal with it.
var Battle = require('act/Battle.js'); 
var ModalWindow = require('weblobby/comp/ModalWindow.jsx');
var Server = require('weblobby/act/LobbyServer.js');
var ConState = require('weblobby/store/LobbyServerCommon.js').ConnectionState;

module.exports = React.createClass({
	displayName: 'Home@zkwl',
	getInitialState: function(){
		return {
			selected: null,
			hideSubmenu: true,
			pickinChickin: null,
		};
	},
	handleSkirmish: function(bot){
		var engine = '100.0';
		var gameInfo = this.props.gameInfoStore.getInitialState();
		// Use the latest version of the game installed.
		// This works by virtue of unitsync filling versions in order.
		var modname = _(gameInfo.games).keys().filter(function(name){
			return !!name.match(/^Zero-K v/);
		}).last();
		if (!modname) {
			Log.infoBox('Downloading updates...');
			Process.downloadGame('zk:stable');
			if (!_.contains(gameInfo.engines, engine))
				Process.downloadEngine(engine);
			this.props.onToggleDownloads();
			return;
		}
		Battle.openLocalBattle('Skirmish' + (bot ? ' vs ' + bot : ''), function(){
			this.setEngine(engine);
			this.setGame(modname);
			this.setMap(_.sample(_.keys(gameInfo.maps)) || '');
			bot && this.addBot({
				team: 2,
				name: 'Enemy',
				type: bot,
			});
		});
	},
	handlePickChicken: function(val){
		this.setState({ pickinChickin: val });
	},
	handleOpenBattlelist: function(){
		if (this.props.serverStore.getInitialState().connection !== ConState.CONNECTED)
			Server.connect();
		else
			this.props.onSelect('battlelist');
	},
	handleSelect: function(sel){
		this.setState({ selected: sel, hideSubmenu: true }, function(){
			_.defer(function(){ this.setState({ hideSubmenu: false }); }.bind(this));
		});
	},
	renderSubmenu: function(){
		var cx = 'submenu' + (this.state.hideSubmenu ? '' : ' show');
		switch (this.state.selected) {

		case 'singleplayer':
			return <div className={cx}>
				<button>Missions</button>
				<button onClick={_.partial(this.handleSkirmish, 'CAI')}>Play vs AI</button>
				<button onClick={_.partial(this.handlePickChicken, true)}>Play vs Chicken</button>
				<button onClick={_.partial(this.handleSkirmish, null)}>Custom game</button>
			</div>;
		case 'multiplayer':
			return <div className={cx}>
				<button>Matchmaker</button>
				<button onClick={this.handleOpenBattlelist}>Open battles</button>
			</div>;
		default:
			return null;
		}
	},
	render: function(){
		return <div id="zkMainMenu">
		<div id="background" />
		<div className="menu">
			<button onClick={_.partial(this.handleSelect, 'singleplayer')}>Singleplayer</button>
			<button onClick={_.partial(this.handleSelect, 'multiplayer')}>Multiplayer</button>
			<button onClick={_.partial(this.props.onSelect, 'settings')}>Settings</button>
			<button onClick={_.partial(this.props.onSelect, 'help')}>Help</button>
			<button>Exit</button>
		</div>
		{this.renderSubmenu()}
		{this.state.pickinChickin && <ModalWindow
				onClose={_.partial(this.handlePickChicken, false)}
				title="Pick difficulty"
		>
			{['Very Easy', 'Easy', 'Normal', 'Hard', 'Very Hard'].map(function(diff){
				return <button
					key={diff}
					onClick={_.partial(this.handleSkirmish, 'Chicken: ' + diff)}
				>
					{diff}
				</button>;
			}.bind(this))}
		</ModalWindow>}
		</div>
	}
});
