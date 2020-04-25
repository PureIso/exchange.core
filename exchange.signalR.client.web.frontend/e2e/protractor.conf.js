const webpack = require('webpack');
const WebpackDevServer = require('webpack-dev-server');
const config = require('../webpack/webpack-development.config.js');
const { SpecReporter } = require('jasmine-spec-reporter');

const server = new WebpackDevServer(webpack(config));

exports.config = {
    specs: [
        './src/**.e2e-spec.ts'
    ],
    exclude: [],

    baseUrl: 'http://localhost:9000',
    framework: 'jasmine',
    seleniumAddress: 'http://localhost:4444/wd/hub',
    allScriptsTimeout: 110000,
    jasmineNodeOpts: {
        showTiming: true,
        showColors: true,
        isVerbose: false,
        includeStackTrace: false,
        defaultTimeoutInterval: 400000
    },
    capabilities: {
        'directConnect': true,
        'browserName': 'chrome',
        chromeOptions: {
            args: ["--headless", "--disable-gpu", "--window-size=800x600"]
        }
    },
    noGlobals: true,
    beforeLaunch: () => {
        console.log("Before starting");
        return new Promise((resolve, reject) => {
            server.listen(9000, 'localhost', function (err) {
                if (err) {
                    console.log('webpack dev server error: ', err)
                };
                resolve();
            }).on('error', (error) => {
                console.log('dev server error ', error)
                reject(error);
            });
        })
    },
    onPrepare: () => {
        let globals = require('protractor');
        let browser = globals.browser;
        browser.ignoreSynchronization = true;
        browser.baseUrl = 'http://localhost:9000';
        browser.manage().timeouts().implicitlyWait(5000);

        require('ts-node').register({
            project: require('path').join(__dirname, './tsconfig.e2e.json')
        });
        jasmine.getEnv().addReporter(new SpecReporter({
            spec: {
                displayStacktrace: true,
                displayFailuresSummary: true,
                displayFailuredSpec: true,
                displaySuiteNumber: true,
                displaySpecDuration: true
            }
        }));
    }
};