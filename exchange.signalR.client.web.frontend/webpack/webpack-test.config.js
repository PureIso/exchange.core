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
                // Mark files inside `@angular/core` as using SystemJS style dynamic imports.
                // Removing this will cause deprecation warnings to appear.
                test: /[\/\\]@angular[\/\\]core[\/\\].+\.js$/,
                parser: { system: true }
            },
            {
                test: /\.tsx?$/,
                loaders: [
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
    plugins: [
        /** 
         * Dealing with the Warning:
         * WARNING in ./node_modules/@angular/core/fesm5/core.js
         * Critical dependency: the request of a dependency is an expression
         */
        new webpack.ContextReplacementPlugin(
            /\@angular(\\|\/)core(\\|\/)fesm5/,
            path.resolve(__dirname, '../src')
        )
    ]
}
