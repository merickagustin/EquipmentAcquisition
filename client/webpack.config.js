const path = require('path');

module.exports = {
  entry: {
    nav: './nav/index.tsx',
    'menu-admin': './menu-admin/index.tsx',
    // Named *-admin, not e.g. 'vendors' — an entry named 'vendors' would collide
    // with the splitChunks cacheGroup below, which already outputs to vendors/app.js.
    'vendors-admin': './vendors-admin/index.tsx',
    'departments-admin': './departments-admin/index.tsx',
    'equipment-categories-admin': './equipment-categories-admin/index.tsx',
  },
  output: {
    path: path.resolve(__dirname, 'dist'),
    filename: '[name]/app.js',
    clean: true,
  },
  optimization: {
    // A single, deterministically-named vendor chunk (not per-entry auto-hashed names) —
    // Razor references it with a plain <script> tag, no HtmlWebpackPlugin/manifest needed.
    splitChunks: {
      cacheGroups: {
        vendors: { test: /[\\/]node_modules[\\/]/, name: 'vendors', chunks: 'all' },
      },
    },
  },
  resolve: {
    extensions: ['.tsx', '.ts', '.js'],
  },
  module: {
    rules: [
      {
        test: /\.tsx?$/,
        use: 'ts-loader',
        exclude: /node_modules/,
      },
    ],
  },
  devtool: 'source-map',
};
