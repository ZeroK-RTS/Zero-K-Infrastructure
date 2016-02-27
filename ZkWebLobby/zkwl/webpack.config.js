var ExtractPlugin = require('extract-text-webpack-plugin');

module.exports = {
	entry: './zkwl.jsx',
	output: {
		path: __dirname + '/build',
		filename: 'zkwl.bundle.js'
	},
	module: {
		loaders: [
			{ test: /\.jsx$/, loader: 'jsx' },
			{ test: /\.sass$/, loader: ExtractPlugin.extract('css!sass?indentedSyntax') },
			{ test: /\.(png|jpg|gif|svg)$/, loader: 'url?limit=10000&name=[path][name].[ext]' },
			{ test: /\.(html|wav|ogg|webm)$/, loader: 'file?name=[path][name].[ext]' },
		]
	},
	resolve: {
		root: __dirname,
		fallback: __dirname + '/node_modules/weblobby'
	},
	plugins: [
		new ExtractPlugin('main.css', { allChunks: true })
	]
};
