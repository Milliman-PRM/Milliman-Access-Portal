const path = require('path');
const webpack = require('webpack');
const CopyWebpackPlugin = require('copy-webpack-plugin');
const ExtractTextPlugin = require('extract-text-webpack-plugin');

module.exports = {
  entry: {
    'account-settings': './src/js/account-settings.js',
    'client-admin': './src/js/client-admin.js',
    'content-access-admin': './src/js/content-access-admin.js',
    'login': './src/js/login.js',
    'hosted-content': './src/js/hosted-content.js',
  },
  module: {
    rules: [
      {
        test: /\.css$/,
        use: [
          { loader: 'style-loader' },
          { loader: 'css-loader' },
        ],
      },
      {
        test: /\.less$/,
        use: [
          { loader: 'style-loader' },
          { loader: 'css-loader' },
          { loader: 'less-loader' },
        ],
      },
      {
        test: /\.s[ac]ss$/,
        use: [
          { loader: 'style-loader' },
          { loader: 'css-loader' },
          { loader: 'sass-loader' },
        ],
      },
    ],
  },
  output: {
    path: path.resolve(__dirname, 'wwwroot', 'js'),
    filename: '[name].bundle.js',
  },
  plugins: [
    new CopyWebpackPlugin([
      {
        from: 'src/images',
        to: '../images',
      },
      {
        from: 'src/favicon.ico',
        to: '../favicon.ico',
      },
    ]),
    new webpack.ProvidePlugin({
      $: 'jquery',
      jQuery: 'jquery',
    }),
  ],
  mode: 'development',
  devtool: 'source-map',
};
