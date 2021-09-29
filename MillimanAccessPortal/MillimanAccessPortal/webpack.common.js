const path = require('path');
const webpack = require('webpack');
const CopyWebpackPlugin = require('copy-webpack-plugin');
const SpriteLoaderPlugin = require('svg-sprite-loader/plugin');
const HtmlWebpackPlugin = require('html-webpack-plugin');

module.exports = {
  entry: {
    'account-settings': './src/ts/react/account-settings/index.tsx',
    'authorized-content': './src/ts/react/authorized-content/index.tsx',
    'client-access-review': './src/ts/react/client-access-review/index.tsx',
    'client-admin': './src/ts/react/client-admin/index.tsx',
    'content-access-admin': './src/ts/react/content-access-admin/index.tsx',
    'content-disclaimer': './src/ts/content-disclaimer.ts',
    'content-wrapper': './src/ts/react/authorized-content/content-wrapper.tsx',
    'content-publishing': './src/ts/react/content-publishing/index.tsx',
    'create-initial-user': './src/ts/react/create-initial-user/index.tsx',
    'enable-account': './src/ts/react/enable-account/index.tsx',
    'file-drop': './src/ts/react/file-drop/index.tsx',
    'forgot-password': './src/ts/react/forgot-password/index.tsx',
    'login': './src/ts/react/login/index.tsx',
    'login-step-two': './src/ts/react/login-step-two/index.tsx',
    'message': './src/ts/message.ts',
    'reset-password': './src/ts/react/reset-password/index.tsx',
    'system-admin': './src/ts/react/system-admin/index.tsx',
    'update-user-agreement': './src/ts/react/system-admin/update-user-agreement/index.tsx',
    'user-agreement': './src/ts/user-agreement.ts',
    'user-agreement-read-only': './src/ts/user-agreement-read-only.ts',
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
      minify: false,
    }),
    new HtmlWebpackPlugin({
      filename: path.resolve(__dirname, 'Views', 'Account', 'ForgotPassword.cshtml'),
      template: path.resolve(__dirname, 'ViewTemplates', 'Account', 'ForgotPassword.cshtml.template'),
      inject: false,
      chunks: [ 'commons', 'forgot-password' ],
      minify: false,
    }),
    new HtmlWebpackPlugin({
      filename: path.resolve(__dirname, 'Views', 'Account', 'ResetPassword.cshtml'),
      template: path.resolve(__dirname, 'ViewTemplates', 'Account', 'ResetPassword.cshtml.template'),
      inject: false,
      chunks: [ 'commons', 'reset-password' ],
      minify: false,
    }),
    new HtmlWebpackPlugin({
      filename: path.resolve(__dirname, 'Views', 'Account', 'EnableAccount.cshtml'),
      template: path.resolve(__dirname, 'ViewTemplates', 'Account', 'EnableAccount.cshtml.template'),
      inject: false,
      chunks: [ 'commons', 'enable-account' ],
      minify: false,
    }),
    new HtmlWebpackPlugin({
      filename: path.resolve(__dirname, 'Views', 'Account', 'AccountSettings.cshtml'),
      template: path.resolve(__dirname, 'ViewTemplates', 'Account', 'AccountSettings.cshtml.template'),
      inject: false,
      chunks: [ 'commons', 'account-settings' ],
      minify: false,
    }),
    new HtmlWebpackPlugin({
      filename: path.resolve(__dirname, 'Views', 'ClientAccessReview', 'Index.cshtml'),
      template: path.resolve(__dirname, 'ViewTemplates', 'ClientAccessReview', 'Index.cshtml.template'),
      inject: false,
      chunks: ['commons', 'client-access-review'],
      minify: false,
    }),
    new HtmlWebpackPlugin({
      filename: path.resolve(__dirname, 'Views', 'ClientAdmin', 'Index.cshtml'),
      template: path.resolve(__dirname, 'ViewTemplates', 'ClientAdmin', 'Index.cshtml.template'),
      inject: false,
      chunks: ['commons', 'client-admin'],
      minify: false,
    }),
    new HtmlWebpackPlugin({
      filename: path.resolve(__dirname, 'Views', 'ContentAccessAdmin', 'Index.cshtml'),
      template: path.resolve(__dirname, 'ViewTemplates', 'ContentAccessAdmin', 'Index.cshtml.template'),
      inject: false,
      chunks: [ 'commons', 'content-access-admin' ],
      minify: false,
    }),
    new HtmlWebpackPlugin({
      filename: path.resolve(__dirname, 'Views', 'ContentPublishing', 'Index.cshtml'),
      template: path.resolve(__dirname, 'ViewTemplates', 'ContentPublishing', 'Index.cshtml.template'),
      inject: false,
      chunks: [ 'commons', 'content-publishing' ],
      minify: false,
    }),
    new HtmlWebpackPlugin({
      filename: path.resolve(__dirname, 'Views', 'FileDrop', 'Index.cshtml'),
      template: path.resolve(__dirname, 'ViewTemplates', 'FileDrop', 'Index.cshtml.template'),
      inject: false,
      chunks: ['commons', 'file-drop'],
      minify: false,
    }),
    new HtmlWebpackPlugin({
      filename: path.resolve(__dirname, 'Views', 'AuthorizedContent', 'Index.cshtml'),
      template: path.resolve(__dirname, 'ViewTemplates', 'AuthorizedContent', 'Index.cshtml.template'),
      inject: false,
      chunks: [ 'commons', 'authorized-content' ],
      minify: false,
    }),
    new HtmlWebpackPlugin({
      filename: path.resolve(__dirname, 'Views', 'AuthorizedContent', 'ContentDisclaimer.cshtml'),
      template: path.resolve(__dirname, 'ViewTemplates', 'AuthorizedContent', 'ContentDisclaimer.cshtml.template'),
      inject: false,
      chunks: [ 'commons', 'content-disclaimer' ],
      minify: false,
    }),
    new HtmlWebpackPlugin({
      filename: path.resolve(__dirname, 'Views', 'AuthorizedContent', 'ContentWrapper.cshtml'),
      template: path.resolve(__dirname, 'ViewTemplates', 'AuthorizedContent', 'ContentWrapper.cshtml.template'),
      inject: false,
      chunks: [ 'commons', 'content-wrapper' ],
      minify: false,
    }),
    new HtmlWebpackPlugin({
        filename: path.resolve(__dirname, 'Views', 'Shared', 'UserMessage.cshtml'),
        template: path.resolve(__dirname, 'ViewTemplates', 'Shared', 'UserMessage.cshtml.template'),
        inject: false,
        chunks: ['commons', 'message'],
        minify: false,
      }),
    new HtmlWebpackPlugin({
      filename: path.resolve(__dirname, 'Views', 'Account', 'Login.cshtml'),
      template: path.resolve(__dirname, 'ViewTemplates', 'Account', 'Login.cshtml.template'),
      inject: false,
      chunks: [ 'commons', 'login' ],
      minify: false,
    }),
    new HtmlWebpackPlugin({
      filename: path.resolve(__dirname, 'Views', 'Account', 'LoginStepTwo.cshtml'),
      template: path.resolve(__dirname, 'ViewTemplates', 'Account', 'LoginStepTwo.cshtml.template'),
      inject: false,
      chunks: [ 'commons', 'login-step-two' ],
      minify: false,
    }),
    new HtmlWebpackPlugin({
      filename: path.resolve(__dirname, 'Views', 'SystemAdmin', 'Index.cshtml'),
      template: path.resolve(__dirname, 'ViewTemplates', 'SystemAdmin', 'Index.cshtml.template'),
      inject: false,
      chunks: [ 'commons', 'system-admin' ],
      minify: false,
    }),
    new HtmlWebpackPlugin({
      filename: path.resolve(__dirname, 'Views', 'SystemAdmin', 'UpdateUserAgreement.cshtml'),
      template: path.resolve(__dirname, 'ViewTemplates', 'SystemAdmin', 'UpdateUserAgreement.cshtml.template'),
      inject: false,
      chunks: ['commons', 'update-user-agreement'],
      minify: false,
    }),
    new HtmlWebpackPlugin({
      filename: path.resolve(__dirname, 'Views', 'Account', 'UserAgreement.cshtml'),
      template: path.resolve(__dirname, 'ViewTemplates', 'Account', 'UserAgreement.cshtml.template'),
      inject: false,
      chunks: ['commons', 'user-agreement'],
      minify: false,
    }),
    new HtmlWebpackPlugin({
      filename: path.resolve(__dirname, 'Views', 'Account', 'UserAgreementReadOnly.cshtml'),
      template: path.resolve(__dirname, 'ViewTemplates', 'Account', 'UserAgreementReadOnly.cshtml.template'),
      inject: false,
      chunks: ['commons', 'user-agreement-read-only'],
      minify: false,
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

