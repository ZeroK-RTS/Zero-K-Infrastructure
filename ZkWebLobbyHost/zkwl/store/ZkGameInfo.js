'use strict'

var _ = require('lodash');
var Reflux = require('reflux');
var Zkl = require('store/Zkl.js');

// This is based on the scroll size used by zk site.
// See https://github.com/ZeroK-RTS/Zero-K-Infrastructure/blob/master/Zero-K.info/AppCode/Global.cs#L41
var mapSearchPageSize = 40;

function getMapThumbnail(name) {
	return 'http://zero-k.info/Resources/' + name.replace(/ /g, '_') + '.thumbnail.jpg';
}

var API = CefWrapperAPI;
module.exports = function(){ return Reflux.createStore({

	listenables: require('act/GameInfo.js'),

	init: function(){
		this.state = {
			games: {},
			maps: {},
			engines: [],
			currentOperation: null,
			mapSearchResult: [], // null means search in progress
			springSettings: {},
		};
		_.extend(this, {
			mapSearchQuery: {},
			mapSearchPages: 0,
			mapSearchInProgress: false,
		});

		window.on_spring_scanner_work = function(work){
			// Ignore unitsync work items because by the time we get to them we already
			// have the info and it just keeps pointlessly trying to upload in a loop.
			// There's a corener case where ContentService is unreachable and ZKL is trying
			// to build the cache from scratch, but that should be very rare.
			if (work && !work.WorkName.match('UnitSync')) {
				this.setState({
					currentOperation: 'Scanning game resources ' +
						Math.round(work.WorkDone / work.WorkTotal * 100) + '%'
				});
			} else {
				this.setState({ currentOperation: null });
			}
		}.bind(this);

		// The cost of querying the full list is trivial so we just do that.
		var springScannerChange = function(item){
			if (item.ResourceType === 0)
				this.loadMaps();
			else if (item.ResourceType === 1)
				this.loadGames();
		}.bind(this);
		window.on_spring_scanner_add = springScannerChange;
		window.on_spring_scanner_remove = springScannerChange;

		this.loadEngines();
		this.loadGames();
		this.loadMaps();
	},
	getInitialState: function(){
		return this.state;
	},
	setState: function(s){
		_.merge(this.state, s);
		this.trigger(this.state);
	},
	
	loadEngines: function(){
		Zkl.getEngines(function(engines){
			this.setState({ engines: engines });
		}.bind(this));
	},
	loadGames: function(){
		Zkl.getMods(function(mods){
			this.setState({
				games: _.reduce(mods, function(acc, mod){
					acc[mod.InternalName] = { local: true };
					return acc;
				}, {})
			});
		}.bind(this));
	},
	loadMaps: function(){
		Zkl.getMaps(function(maps){
			this.setState({
				maps: _.reduce(maps, function(acc, map){
					acc[map.InternalName] = {
						thumbnail: getMapThumbnail(map.InternalName),
						local: true,
					};
					return acc;
				}, {})
			});
		}.bind(this));
	},
})};
