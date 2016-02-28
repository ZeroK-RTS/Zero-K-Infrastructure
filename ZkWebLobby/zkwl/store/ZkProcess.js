'use strict'

var _ = require('lodash');
var Zkl = require('store/Zkl.js');
var Reflux = require('reflux');
var Log = require('act/Log.js');

// See Shared/PlasmaDownloader/PlasmaDownloader.cs:16
var zklDownloadTypes = ['game', 'map', 'mission', 'game', 'unknown', 'replay'];

module.exports = function(gameInfoStore){ return Reflux.createStore({

	listenables: require('act/Process.js'),

	init: function(){
		this.state = {
			springRunning: false,
			downloads: {},
			currentProcess: null,
		};
		window.on_spring_exit = function(crashed){
			this.setState({ springRunning: false });
		}.bind(this);
		window.on_downloads_change = function(downloads){
			this.setState({
				downloads: _.reduce(downloads, function(acc, dl){
					if (!dl.IsComplete) {
						acc[dl.Name] = {
							name: dl.Name,
							type: zklDownloadTypes[dl.TypeOfResource],
							downloaded: dl.TotalProgress,
							total: dl.TotalLength,
						};
					}
					return acc;
				}, {})
			});
		}.bind(this);
	},
	getInitialState: function(){
		return this.state;
	},
	setState: function(s){
		_.merge(this.state, s);
		this.trigger(this.state);
	},

	downloadEngine: Zkl.downloadEngine,
	downloadGame: Zkl.downloadMod,
	downloadMap: Zkl.downloadMap,

	launchSpringScript: function(ver, script){
		Zkl.startSpringScript(ver, this.scriptify(script), function(err){
			if (err === null)
				this.setState({ springRunning: true });
			else
				Log.errorBox('Could not launch Spring engine: ' + err);
		}.bind(this));
	},

	scriptify: function scriptify(obj, tab){
		tab = tab || '';
		return _.map(obj, function(val, key){
			if (typeof val === 'object')
				return tab  + '[' + key + '] {\n' + scriptify(val, tab+'\t') + tab + '}';
			else if (typeof val === 'boolean')
				return tab + key + ' = ' + (val ? '1' : '0') + ';';
			else
				return tab + key + ' = ' + val + ';';
		}).join('\n') + '\n';
	},

	cancelDownload: function(name){
	},
	killSpring: function(){
	},
})};
