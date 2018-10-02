const path = require('path');
const webpack = require('webpack');
const CleanWebpackPlugin = require('clean-webpack-plugin');
const CopyWebpackPlugin = require('copy-webpack-plugin');
const SpriteLoaderPlugin = require('svg-sprite-loader/plugin');
const HtmlWebpackPlugin = require('html-webpack-plugin');

module.exports = {
  entry: {
    'account-settings': './src/js/account-settings.js',
    'authorized-content': './src/js/react/authorized-content/index.js',
    'client-admin': './src/js/client-admin.js',
    'content-access-admin': './src/js/content-access-admin/index.js',
    'content-publishing': './src/js/content-publishing/index.js',
    'create-initial-user': './src/js/create-initial-user.js',
    'enable-account': './src/js/enable-account.js',
    'forgot-password': './src/js/forgot-password.js',
    'login': './src/js/login.js',
    'message': './src/js/message.js',
    'reset-password': './src/js/reset-password.js',
    'system-admin': './src/js/react/system-admin/index.js',
  },
  output: {
    path: path.resolve(__dirname, 'wwwroot'),
    filename: 'js/[name].[contenthash].js',
    publicPath: '~',
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
    ],
  },
  plugins: [
    new CleanWebpackPlugin([
      'wwwroot/css',
      'wwwroot/images',
      'wwwroot/js',
      'wwwroot/favicon.ico',
    ]),
    new CopyWebpackPlugin([
      {
        from: 'src/favicon.ico',
        to: 'favicon.ico',
      },
      {
        from: 'src/images/default_content_images/',
        to: 'images/',
        flatten: true,
      },
      {
        context: 'ViewTemplates',
        from: '**/*.cshtml',
        to: '../Views/',
      }
    ]),
    new webpack.ProvidePlugin({
      $: 'jquery',
      jQuery: 'jquery',
    }),
    new webpack.NamedModulesPlugin(),
    new SpriteLoaderPlugin({
      plainSprite: true
    }),
    // Separate plugin instance for each entry point
    new HtmlWebpackPlugin({
      filename: path.resolve(__dirname, 'Views', 'Account', 'CreateInitialUser.cshtml'),
      template: path.resolve(__dirname, 'ViewTemplates', 'Account', 'CreateInitialUser.cshtml.template'),
      inject: false,
      chunks: [ 'commons', 'create-initial-user' ],
    }),
    new HtmlWebpackPlugin({
      filename: path.resolve(__dirname, 'Views', 'Account', 'ForgotPassword.cshtml'),
      template: path.resolve(__dirname, 'ViewTemplates', 'Account', 'ForgotPassword.cshtml.template'),
      inject: false,
      chunks: [ 'commons', 'forgot-password' ],
    }),
    new HtmlWebpackPlugin({
      filename: path.resolve(__dirname, 'Views', 'Account', 'ResetPassword.cshtml'),
      template: path.resolve(__dirname, 'ViewTemplates', 'Account', 'ResetPassword.cshtml.template'),
      inject: false,
      chunks: [ 'commons', 'reset-password' ],
    }),
    new HtmlWebpackPlugin({
      filename: path.resolve(__dirname, 'Views', 'Account', 'EnableAccount.cshtml'),
      template: path.resolve(__dirname, 'ViewTemplates', 'Account', 'EnableAccount.cshtml.template'),
      inject: false,
      chunks: [ 'commons', 'enable-account' ],
    }),
    new HtmlWebpackPlugin({
      filename: path.resolve(__dirname, 'Views', 'Account', 'AccountSettings.cshtml'),
      template: path.resolve(__dirname, 'ViewTemplates', 'Account', 'AccountSettings.cshtml.template'),
      inject: false,
      chunks: [ 'commons', 'account-settings' ],
    }),
    new HtmlWebpackPlugin({
      filename: path.resolve(__dirname, 'Views', 'ClientAdmin', 'Index.cshtml'),
      template: path.resolve(__dirname, 'ViewTemplates', 'ClientAdmin', 'Index.cshtml.template'),
      inject: false,
      chunks: [ 'commons', 'client-admin' ],
    }),
    new HtmlWebpackPlugin({
      filename: path.resolve(__dirname, 'Views', 'ContentAccessAdmin', 'Index.cshtml'),
      template: path.resolve(__dirname, 'ViewTemplates', 'ContentAccessAdmin', 'Index.cshtml.template'),
      inject: false,
      chunks: [ 'commons', 'content-access-admin' ],
    }),
    new HtmlWebpackPlugin({
      filename: path.resolve(__dirname, 'Views', 'ContentPublishing', 'Index.cshtml'),
      template: path.resolve(__dirname, 'ViewTemplates', 'ContentPublishing', 'Index.cshtml.template'),
      inject: false,
      chunks: [ 'commons', 'content-publishing' ],
    }),
    new HtmlWebpackPlugin({
      filename: path.resolve(__dirname, 'Views', 'AuthorizedContent', 'Index.cshtml'),
      template: path.resolve(__dirname, 'ViewTemplates', 'AuthorizedContent', 'Index.cshtml.template'),
      inject: false,
      chunks: [ 'commons', 'authorized-content' ],
    }),
    new HtmlWebpackPlugin({
      filename: path.resolve(__dirname, 'Views', 'Shared', 'Message.cshtml'),
      template: path.resolve(__dirname, 'ViewTemplates', 'Shared', 'Message.cshtml.template'),
      inject: false,
      chunks: ['commons', 'message'],
    }),
    new HtmlWebpackPlugin({
      filename: path.resolve(__dirname, 'Views', 'Account', 'Login.cshtml'),
      template: path.resolve(__dirname, 'ViewTemplates', 'Account', 'Login.cshtml.template'),
      inject: false,
      chunks: [ 'commons', 'login' ],
    }),
    new HtmlWebpackPlugin({
      filename: path.resolve(__dirname, 'Views', 'SystemAdmin', 'Index.cshtml'),
      template: path.resolve(__dirname, 'ViewTemplates', 'SystemAdmin', 'Index.cshtml.template'),
      inject: false,
      chunks: [ 'commons', 'system-admin' ],
    }),
  ],
  resolve: {
    extensions: [
      '.webpack.js',
      '.web.js',
      '.js',
    ],
  },
};
