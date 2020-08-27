const path = require('path');
const webpack = require('webpack');
const CopyWebpackPlugin = require('copy-webpack-plugin');
const SpriteLoaderPlugin = require('svg-sprite-loader/plugin');
const HtmlWebpackPlugin = require('html-webpack-plugin');

module.exports = {
  entry: {
    'account-settings': './src/ts/react/account-settings/index.tsx',
    'authorized-content': './src/ts/react/authorized-content/index.tsx',
    'client-admin': './src/ts/client-admin.tsx',
    'content-access-admin': './src/ts/react/content-access-admin/index.tsx',
    'content-disclaimer': './src/ts/content-disclaimer.ts',
    'content-wrapper': './src/ts/react/authorized-content/content-wrapper.tsx',
    'content-publishing': './src/ts/react/content-publishing/index.tsx',
    'create-initial-user': './src/ts/create-initial-user.ts',
    'enable-account': './src/ts/enable-account.ts',
    'file-drop': './src/ts/react/file-drop/index.tsx',
    'forgot-password': './src/ts/react/forgot-password/index.tsx',
    'login': './src/ts/react/login/index.tsx',
    'login-step-two': './src/ts/react/login-step-two/index.tsx',
    'message': './src/ts/message.ts',
    'reset-password': './src/ts/react/reset-password/index.tsx',
    'system-admin': './src/ts/react/system-admin/index.tsx',
    'update-user-agreement': './src/ts/update-user-agreement.ts',
    'user-agreement': './src/ts/user-agreement.ts'
  },
  output: {
    path: path.resolve(__dirname, 'wwwroot'),
    publicPath: '/',
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
        enforce: 'pre',
        use: [
          {
            loader: 'tslint-loader',
            options: {
              configFile: 'tslint.json',
              tsConfigFile: 'tsconfig.json',
              failOnHint: true,
            },
          },
        ],
        include: path.resolve(__dirname, 'src', 'ts'),
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
        from: 'src/images/login-hero.jpg',
        to: 'images/login-hero.jpg',
      },
      {
        from: 'src/js/polyfills.min.js',
        to: 'js/polyfills.min.js',
      },
      {
        from: 'src/html/Error/502.html',
        to: 'Error/502.html',
      },
      {
        context: 'ViewTemplates',
        from: '**/*.cshtml',
        to: '../Views/',
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
      filename: path.resolve(__dirname, 'Views', 'FileDrop', 'Index.cshtml'),
      template: path.resolve(__dirname, 'ViewTemplates', 'FileDrop', 'Index.cshtml.template'),
      inject: false,
      chunks: ['commons', 'file-drop'],
    }),
    new HtmlWebpackPlugin({
      filename: path.resolve(__dirname, 'Views', 'AuthorizedContent', 'Index.cshtml'),
      template: path.resolve(__dirname, 'ViewTemplates', 'AuthorizedContent', 'Index.cshtml.template'),
      inject: false,
      chunks: [ 'commons', 'authorized-content' ],
    }),
    new HtmlWebpackPlugin({
      filename: path.resolve(__dirname, 'Views', 'AuthorizedContent', 'ContentDisclaimer.cshtml'),
      template: path.resolve(__dirname, 'ViewTemplates', 'AuthorizedContent', 'ContentDisclaimer.cshtml.template'),
      inject: false,
      chunks: [ 'commons', 'content-disclaimer' ],
    }),
    new HtmlWebpackPlugin({
      filename: path.resolve(__dirname, 'Views', 'AuthorizedContent', 'ContentWrapper.cshtml'),
      template: path.resolve(__dirname, 'ViewTemplates', 'AuthorizedContent', 'ContentWrapper.cshtml.template'),
      inject: false,
      chunks: [ 'commons', 'content-wrapper' ],
    }),
    new HtmlWebpackPlugin({
        filename: path.resolve(__dirname, 'Views', 'Shared', 'UserMessage.cshtml'),
        template: path.resolve(__dirname, 'ViewTemplates', 'Shared', 'UserMessage.cshtml.template'),
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
      filename: path.resolve(__dirname, 'Views', 'Account', 'LoginStepTwo.cshtml'),
      template: path.resolve(__dirname, 'ViewTemplates', 'Account', 'LoginStepTwo.cshtml.template'),
      inject: false,
      chunks: [ 'commons', 'login-step-two' ],
    }),
    new HtmlWebpackPlugin({
      filename: path.resolve(__dirname, 'Views', 'SystemAdmin', 'Index.cshtml'),
      template: path.resolve(__dirname, 'ViewTemplates', 'SystemAdmin', 'Index.cshtml.template'),
      inject: false,
      chunks: [ 'commons', 'system-admin' ],
    }),
    new HtmlWebpackPlugin({
      filename: path.resolve(__dirname, 'Views', 'SystemAdmin', 'UpdateUserAgreement.cshtml'),
      template: path.resolve(__dirname, 'ViewTemplates', 'SystemAdmin', 'UpdateUserAgreement.cshtml.template'),
      inject: false,
      chunks: ['commons', 'update-user-agreement'],
    }),
    new HtmlWebpackPlugin({
      filename: path.resolve(__dirname, 'Views', 'Account', 'UserAgreement.cshtml'),
      template: path.resolve(__dirname, 'ViewTemplates', 'Account', 'UserAgreement.cshtml.template'),
      inject: false,
      chunks: ['commons', 'user-agreement'],
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
  node: {
    net: 'mock',
  },
};

