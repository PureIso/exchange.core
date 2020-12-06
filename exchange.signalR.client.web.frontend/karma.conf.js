module.exports = function (config) {
    const tests = ['./src/**/*spec.ts'];

    config.set({
        singleRun: true,
        frameworks: ['jasmine', 'webpack'],
        files: tests,
        preprocessors: {
            [tests]: ['webpack']
        },
        webpack: webpackConfig(),
        webpackMiddleware: {
            noInfo: true
        },
        colors: true,
        browsers: ['ChromeHeadless'],
    });
};

function webpackConfig() {
    const config = require('./webpack/webpack-test.config.js');
    delete config.context;
    delete config.entry;
    delete config.output;
    return config;
}