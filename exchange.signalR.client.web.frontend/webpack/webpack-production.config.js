const path = require("path");
const HtmlWebpackPlugin = require("html-webpack-plugin");
const webpack = require("webpack");
const MiniCssExtractPlugin = require("mini-css-extract-plugin");
const CopyWebpackPlugin = require("copy-webpack-plugin");
const { CleanWebpackPlugin } = require("clean-webpack-plugin");
const TsConfigPathsPlugin = require("tsconfig-paths-webpack-plugin");
const buildPath = path.resolve(__dirname, "../build/");
const tsconfigFile = path.join(__dirname, "../tsconfig.json");

//The directory path that should be cleaned on build
let pathToClean = ["build"];
let cleanOptions = {
    root: buildPath,
    verbose: true,
    dry: false
};
module.exports = {
    mode: "production",
    /**Modules webpack need to use to begin building its internal*/
    entry: {
        main: "./src/main.ts",
        polyfills: "./src/polyfills.ts"
    },
    /**Where to emit the bundles it creates and how to name the files */
    output: {
        path: buildPath,
        filename: "js/[name].[contenthash].js",
        chunkFilename: "js/[name].[contenthash].js"
    },
    resolve: {
        extensions: [".js", ".ts", ".tsx", ".jsx"],
        modules: ["node_modules"],
        plugins: [new TsConfigPathsPlugin({ configFile: tsconfigFile })]
    },
    devServer: {
        contentBase: buildPath,
        compress: true,
        historyApiFallback: true,
        port: 9000,
        stats: "minimal",
        host: process.env.HOST
    },
    devtool: "source-map",
    module: {
        rules: [
            {
                test: /\.ts$/,
                use: [
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
                test: /\.css$/,
                use: ['to-string-loader', MiniCssExtractPlugin.loader,
                    {
                        loader: "css-loader",
                        options: {
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
                            sourceMap: true
                        }
                    },
                    {
                        loader: "sass-loader",
                        options: {
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
                            sourceMap: true
                        }
                    },
                    {
                        loader: "less-loader",
                        options: {
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
        noEmitOnErrors: true,
        splitChunks: {
            cacheGroups: {
                default: false,
                vendors: false,
                // vendor chunk
                vendor: {
                    // name of the chunk
                    name: 'vendor',
                    // async + async chunks
                    chunks: 'all',
                    // import file path containing node_modules
                    test: /node_modules/,
                    // priority
                    priority: 20
                },
                // common chunk
                common: {
                    name: 'common',
                    minChunks: 2,
                    chunks: 'all',
                    priority: 10,
                    reuseExistingChunk: true,
                    enforce: true
                }
            }
        }
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
        new CopyWebpackPlugin({
            patterns: [
                { from: "**/*.jpg", to: "img/[name].[ext]" },
                { from: "**/*.ico", to: "img/[name].[ext]" },
                { from: '**/src/electron/*', to: '[name].[ext]' }
            ],
        }),
        // new webpack.HashedModuleIdsPlugin(),
        new CleanWebpackPlugin(cleanOptions)
    ]
};
