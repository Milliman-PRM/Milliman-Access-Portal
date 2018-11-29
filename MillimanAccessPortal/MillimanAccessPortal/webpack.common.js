const path = require('path');
const webpack = require('webpack');
const CopyWebpackPlugin = require('copy-webpack-plugin');
const SpriteLoaderPlugin = require('svg-sprite-loader/plugin');
const HtmlWebpackPlugin = require('html-webpack-plugin');

module.exports = {
  entry: {
    'account-settings': './src/ts/account-settings.tsx',
    'authorized-content': './src/ts/react/authorized-content/index.tsx',
    'client-admin': './src/ts/client-admin.tsx',
    'content-access-admin': './src/ts/content-access-admin/index.tsx',
    'content-publishing': './src/ts/content-publishing/index.tsx',
    'create-initial-user': './src/ts/create-initial-user.ts',
    'enable-account': './src/ts/enable-account.ts',
    'forgot-password': './src/ts/forgot-password.ts',
    'login': './src/ts/login.ts',
    'message': './src/ts/message.ts',
    'reset-password': './src/ts/reset-password.ts',
    'system-admin': './src/ts/react/system-admin/index.tsx',
  },
  output: {
    path: path.resolve(__dirname, 'wwwroot'),
    publicPath: '~/',
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
            },
          },
          {
            loader: 'svgo-loader',
            options: {
              plugins: [
                { removeTitle: true },
                { convertColors: { currentColor: true } },
                { convertPathData: false },
                { cleanupIDs: { remove: false }},
              ],
            },
          },
        ],
        include: path.resolve(__dirname, 'src', 'images'),
      },
      {
        test: /\.tsx?$/,
        use: 'ts-loader',
        include: path.resolve(__dirname, 'src', 'ts'),
      },
    ],
  },
  plugins: [
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
      },
      {
        context: 'ViewTemplates',
        from: '**/*.html',
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
      '.tsx',
      '.ts',
    ],
    symlinks: false,
  },
};
