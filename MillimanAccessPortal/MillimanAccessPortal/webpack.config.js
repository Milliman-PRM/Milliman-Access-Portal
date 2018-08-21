const path = require('path');
const webpack = require('webpack');
const CopyWebpackPlugin = require('copy-webpack-plugin');
const ExtractTextPlugin = require('extract-text-webpack-plugin');
const SpriteLoaderPlugin = require('svg-sprite-loader/plugin');

module.exports = {
  entry: {
    'forgot-password': './src/js/forgot-password.js',
    'reset-password': './src/js/reset-password.js',
    'enable-account': './src/js/enable-account.js',
    'account-settings': './src/js/account-settings.js',
    'client-admin': './src/js/client-admin.js',
    'content-access-admin': './src/js/content-access-admin/index.js',
    'content-publishing': './src/js/content-publishing/index.js',
    'authorized-content': './src/js/react/authorized-content/index.js',
    'login': './src/js/login.js',
    'system-admin': './src/js/react/system-admin/index.js',
  },
  module: {
    rules: [
      {
        test: /\.svg$/,
        use: [
          {
            loader: 'svg-sprite-loader',
            options: {
              extract: false
            }
          },
          {
            loader: 'svgo-loader',
            options: {
              plugins: [
                { removeTitle: true },
                { convertColors: { currentColor: true } },
                { convertPathData: false },
                { cleanupIDs: { remove: false }}
              ]
            }
          }
        ]
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
    publicPath: '/wwwroot/',
  },
  plugins: [
    new CopyWebpackPlugin([
      {
        from: 'src/favicon.ico',
        to: '../favicon.ico',
      },
      {
        from: 'src/images/default_content_images/',
        to: '../images/',
        flatten: true,
      },
    ]),
    new webpack.ProvidePlugin({
      $: 'jquery',
      jQuery: 'jquery',
    }),
    new webpack.NamedModulesPlugin(),
    new SpriteLoaderPlugin({
      plainSprite: true
    }),
  ],
  resolve: {
    extensions: [
      '.webpack.js',
      '.web.js',
      '.js',
    ],
  },
  mode: 'development',
  devtool: 'inline-source-map',
};
