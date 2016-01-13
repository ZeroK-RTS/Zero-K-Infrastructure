'use strict'

require('style/Home.sass');
var _ = require('lodash');
var React = require('react');
var Battle = require('weblobby/act/Battle.js');
var ModalWindow = require('weblobby/comp/ModalWindow.jsx');

module.exports = React.createClass({
	displayName: 'Home@zkwl',
	getInitialState: function(){
		return {
			selected: null,
			pickinChickin: null,
		};
	},
	handleSelect: function(sel){
		this.setState({ selected: sel });
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
		Battle.openLocalBattle('Skirmish vs ' + bot, function(){
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
	renderSubmenu: function(){
		switch (this.state.selected) {

		case 'missions':
			return <div className="submenu">
				<button>Tutorial</button>
				<button>Sandbox</button>
				<button>Sunrise Episode 1 - Awakening</button>
				<button>Carrier has Arrived</button>
			</div>;
		case 'skirmish':
			return <div className="submenu">
				<button onClick={_.partial(this.handleSkirmish, 'CAI')}>Play vs AI</button>
				<button onClick={_.partial(this.handlePickChicken, true)}>Play vs Chicken</button>
				<button onClick={_.partial(this.handleSkirmish, null)}>Custom game</button>
			</div>;
		case 'multiplayer':
			return <div className="submenu">
				<button>Matchmaker</button>
				<button onClick={_.partial(this.props.onSelect, 'battlelist')}>Open battles</button>
			</div>;
		default:
			return null;
		}
	},
	render: function(){
		return <div id="zkMainMenu">
		<div className="menu">
			<button onClick={_.partial(this.handleSelect, 'missions')}>Missions</button>
			<button onClick={_.partial(this.handleSelect, 'skirmish')}>Skirmish</button>
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
