const path = require('path');
const webpack = require('webpack');
const HtmlWebpackPlugin = require("html-webpack-plugin");
const TsconfigPathsPlugin = require('tsconfig-paths-webpack-plugin');
const buildPath = path.resolve(__dirname, "../../exchange.signalR.client.web.frontend/wwwroot/");
const tsconfigFile = path.join(__dirname, "../tsconfig.json");

module.exports = {
    mode: "development",
    /**Modules webpack need to use to begin building its internal*/
    entry: {
        main: "./src/main.ts",
        polyfills: "./src/polyfills.ts"
    },
    /**Where to emit the bundles it creates and how to name the files */
    output: {
        path: buildPath,
        filename: "js/[name].js",
        chunkFilename: "js/[name].js"
    },
    resolve: {
        extensions: ['.ts', '.tsx', '.js', '.jsx'],
        plugins: [new TsconfigPathsPlugin({ configFile: tsconfigFile })]
    },
    devtool: 'inline-source-map',
    module: {
        rules: [
            {
                test: /\.tsx?/,
                exclude: /node_modules/,
                use: [
                    {
                        loader: 'ts-loader',
                        options: {
                            // disable type checker - we will use it in fork plugin
                            transpileOnly: true
                        },
                    }
                ]
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
    plugins: [
        new HtmlWebpackPlugin({
            template: path.resolve(__dirname, "../src/index.html"),
            filename: "index.html",
            showErrors: true,
            path: buildPath,
            hash: true
        })
    ]
}
