const path = require('path');
const webpack = require('webpack');
const TsconfigPathsPlugin = require('tsconfig-paths-webpack-plugin');

module.exports = {
    mode: 'development',
    resolve: {
        extensions: ['.ts', '.tsx', '.js', '.jsx'],
        plugins: [new TsconfigPathsPlugin({ configFile: path.join(__dirname, "../tsconfig.json") })]
    },
    devtool: 'inline-source-map',
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