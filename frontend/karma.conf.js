module.exports = function configureKarma(config) {
  config.set({
    frameworks: ['jasmine', '@angular-devkit/build-angular'],
    plugins: [
      require('karma-jasmine'),
      require('karma-chrome-launcher'),
      require('karma-jasmine-html-reporter'),
      require('karma-coverage'),
      require('@angular-devkit/build-angular/plugins/karma')
    ],
    customLaunchers: {
      ChromeHeadlessCI: {
        base: 'ChromeHeadless',
        flags: [
          '--no-sandbox',
          '--disable-gpu',
          '--disable-dev-shm-usage',
          '--disable-software-rasterizer'
        ]
      }
    },
    browserDisconnectTolerance: 2,
    browserNoActivityTimeout: 60000,
    captureTimeout: 60000,
    coverageReporter: {
      dir: process.env.FORGEMART_COVERAGE_DIR || require('path').join(__dirname, 'coverage'),
      subdir: '.',
      reporters: [
        { type: 'html' },
        { type: 'text-summary' },
        { type: 'lcovonly' },
        { type: 'json-summary' }
      ],
      check: {
        global: { statements: 80, branches: 70, functions: 80, lines: 80 }
      }
    }
  });
};
