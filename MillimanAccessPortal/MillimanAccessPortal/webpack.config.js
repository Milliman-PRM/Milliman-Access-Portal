const path = require('path');
const webpack = require('webpack');
const CopyWebpackPlugin = require('copy-webpack-plugin');
const ExtractTextPlugin = require('extract-text-webpack-plugin');

module.exports = {
  entry: {
    'account-settings': './src/js/account-settings.ts',
    'client-admin': './src/js/client-admin.ts',
    'content-access-admin': './src/js/content-access-admin.ts',
    'content-publisher': './src/js/content-publisher.ts',
    'login': './src/js/login.ts',
    'hosted-content': './src/js/hosted-content.ts',
    'system-admin': './src/js/system-admin.ts',
  },
  module: {
    rules: [
      {
        test: /\.tsx?$/,
        use: [
          { loader: 'awesome-typescript-loader' },
        ],
      },
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
    publicPath: 'wwwroot/',
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
    new webpack.NamedModulesPlugin(),
  ],
  resolve: {
    extensions: [
      '.webpack.js',
      '.web.js',
      '.ts',
      '.tsx',
      '.js',
    ],
  },
  mode: 'development',
  devtool: 'inline-source-map',
};
