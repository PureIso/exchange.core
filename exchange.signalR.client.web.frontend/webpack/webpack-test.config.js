const path = require('path');
const webpack = require('webpack');
const TsconfigPathsPlugin = require('tsconfig-paths-webpack-plugin');
const buildPath = path.resolve(__dirname, "../build_e2e/");

module.exports = {
    mode: 'development',
    resolve: {
        extensions: ['.ts', '.tsx', '.js', '.jsx'],
        plugins: [new TsconfigPathsPlugin({ configFile: path.join(__dirname, "../tsconfig.json") })]
    },
    devtool: 'inline-source-map',
    devServer: {
        host: '0.0.0.0',
        port: 9000,
        public: `frontend`,
        publicPath: '/',
        compress: false,
        contentBase: buildPath,
        historyApiFallback: true,
        host: process.env.HOST,
        hot: true,
        inline: true,
        stats: "minimal",
        watchOptions: {
            aggregateTimeout: 300,
            poll: 1000,
            ignored: /node_modules/,
        },
        disableHostCheck: true
    },
    module: {
        rules: [
            {
                test: /\.tsx?$/,
                use: [
                    'awesome-typescript-loader',
                    'angular2-template-loader',
                    'angular-router-loader'
                ],
                exclude: [/node_modules/]
            },
            {
                test: /\.(html)$/,
                use: ['html-loader']
            },
            {
                test: /\.css$/,
                use: ["css-loader"]
            },
            {
                test: /\.(scss|sass)$/,
                use: [
                    "to-string-loader", "css-loader", "sass-loader"]
            },
            {
                test: /\.less$/,
                use: ["css-loader", "less-loader"]
            },
        ]
    },
    plugins: []
}