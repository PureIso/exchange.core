const path = require("path");
const HtmlWebpackPlugin = require("html-webpack-plugin");
const webpack = require("webpack");
const MiniCssExtractPlugin = require("mini-css-extract-plugin");
const CopyWebpackPlugin = require("copy-webpack-plugin");
const { CleanWebpackPlugin } = require("clean-webpack-plugin");
const TsConfigPathsPlugin = require("tsconfig-paths-webpack-plugin");

const buildPath = path.resolve(__dirname, "../../exchange.signalR.client.web.backend/wwwroot/");
const tsconfigFile = path.join(__dirname, "../tsconfig.json");
const buildRoot = path.resolve(__dirname, "../../exchange.signalR.client.web.backend/");

//The directory path that should be cleaned on build
let pathToClean = ["wwwroot"];
let cleanOptions = {
    root: buildRoot,
    verbose: true,
    dry: false
};

const envPath = path.resolve(__dirname, "../.env");
var dotenv = require('dotenv').config({path: envPath});

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
        extensions: [".js", ".ts", ".tsx", ".jsx"],
        modules: ["node_modules"],
        plugins: [new TsConfigPathsPlugin({ configFile: tsconfigFile })]
    },
    devServer: {
        contentBase: buildPath,
        historyApiFallback: true,
        port: 9000,
        stats: "minimal",
        host: '0.0.0.0'
    },
    devtool: "inline-source-map",
    module: {
        rules: [
            {
                // Mark files inside `@angular/core` as using SystemJS style dynamic imports.
                // Removing this will cause deprecation warnings to appear.
                test: /[\/\\]@angular[\/\\]core[\/\\].+\.js$/,
                parser: { system: true }
            },
            {
                test: /\.ts$/,
                loaders: [
                    "awesome-typescript-loader",
                    "angular2-template-loader",
                    "angular-router-loader"
                ],
                exclude: [/node_modules/]
            },
            {
                test: /\.(html)$/,
                use: ["html-loader"]
            },
            {
                test: /\.css$/, loaders: ['to-string-loader', MiniCssExtractPlugin.loader,
                    {
                        loader: "css-loader",
                        options: {
                            //minimize: false,
                            sourceMap: true
                        }
                    },]
            },
            {
                test: /\.(sc|sa)ss$/,
                use: [
                    /**returns sass or scss rendered to css to a string*/
                    "to-string-loader",
                    {
                        loader: "css-loader",
                        options: {
                            //minimize: false,
                            sourceMap: true
                        }
                    },
                    {
                        loader: "sass-loader",
                        options: {
                            //minimize: false,
                            sourceMap: true
                        }
                    }
                ]
            },
            {
                test: /\.less$/,
                use: [
                    MiniCssExtractPlugin.loader,
                    {
                        loader: "css-loader",
                        options: {
                            //minimize: false,
                            sourceMap: true
                        }
                    },
                    {
                        loader: "less-loader",
                        options: {
                            //minimize: false,
                            sourceMap: true
                        }
                    }
                ]
            },
            {
                test: /\.(jpg|ico|otf|gif|png|jpe?g)$/,
                use: [
                    {
                        loader: "file-loader",
                        options: {
                            name: "[name].[ext]",
                            outputPath: "img/",
                            publicPath: "img/"
                        }
                    }
                ]
            },
            {
                test: /\.(eot|svg|ttf|woff|woff2)$/,
                use: [
                    {
                        loader: "url-loader",
                        options: {
                            limit: 10000,
                            name: "[name].[ext]",
                            outputPath: "font/",
                            publicPath: "../font/"
                        }
                    }
                ]
            }
        ]
    },
    optimization: {
        noEmitOnErrors: true
    },
    plugins: [
        new HtmlWebpackPlugin({
            template: path.resolve(__dirname, "../src/index.html"),
            filename: "index.html",
            showErrors: true,
            path: buildPath,
            hash: true
        }),
        new MiniCssExtractPlugin({
            filename: "css/[name].css",
            chunkFilename: "css/[id].css"
        }),
        new webpack.DefinePlugin({
            "process.env": JSON.stringify(dotenv.parsed)
        }),
        new CopyWebpackPlugin({
            patterns:[
                { from: "**/*.jpg", to: "img/[name].[ext]" },
                { from: "**/*.ico", to: "img/[name].[ext]" },
                { from: '**/src/electron/*', to: '[name].[ext]' }
            ],
        }),
        new webpack.ContextReplacementPlugin(
            /\@angular(\\|\/)core(\\|\/)fesm2015/,
            path.resolve(__dirname, "../src")
        ),
        new CleanWebpackPlugin()
    ]
};
