module.exports = function (config) {
    config.set({
        frameworks: ['jasmine'],
        files: [
            "wwwroot/lib/jquery/jquery.min.js",
            "wwwroot/lib/jquery-ui/jquery-ui.min.js",
            "wwwroot/semantic-ui/dist/semantic.js",
            "wwwroot/lib/momentjs/moment.min.js",
            "wwwroot/js/!(site).js",
            "wwwroot/js/tests/*.spec.js"
        ],
        exclude: [
            "wwwroot/js/site.js",
            "wwwroot/js/site.min.js"
        ],
        reporters: ['progress'],
        port: 9876, // karma web server port
        colors: true,
        logLevel: config.LOG_INFO,
        browsers: ['ChromeHeadless'],
        autoWatch: false,
        // singleRun: false, // Karma captures browsers, runs the tests and exits
        singleRun: true,
        concurrency: Infinity
    });
}