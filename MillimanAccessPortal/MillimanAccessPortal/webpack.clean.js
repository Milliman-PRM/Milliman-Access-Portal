const CleanWebpackPlugin = require('clean-webpack-plugin');

module.exports = {
  plugins: [
    new CleanWebpackPlugin([
      'src/js',
      'Views',
      'wwwroot/css',
      'wwwroot/images',
      'wwwroot/js',
      'wwwroot/favicon.ico',
    ]),
  ],
};
