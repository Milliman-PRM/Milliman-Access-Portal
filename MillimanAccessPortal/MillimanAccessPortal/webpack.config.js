const path = require('path');
const webpack = require('webpack');
const CopyWebpackPlugin = require('copy-webpack-plugin');
const ExtractTextPlugin = require('extract-text-webpack-plugin');

module.exports = {
  entry: {
    'account-settings': './src/ts/account-settings.ts',
    'client-admin': './src/ts/client-admin.ts',
    'content-access-admin': './src/ts/content-access-admin.ts',
    'login': './src/ts/login.ts',
    'hosted-content': './src/ts/hosted-content.ts',
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
