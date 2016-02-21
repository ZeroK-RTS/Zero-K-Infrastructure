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
