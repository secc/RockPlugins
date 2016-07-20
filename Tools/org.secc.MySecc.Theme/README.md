# My SECC Rock Theme

> A [RockRMS](http://www.rockrms.com) Theme for the My SECC External Website.

## Installation

This project is built with [node.js](https://nodejs.org/) packages. The installation instructions assume that you have node.js installed, as well as the [npm package manager](https://www.npmsjs.com).

1. Clone the entire Rock repo
2. Install [gulp](http://gulpjs.com)  and/or [bower](https://bower.io) if you do not already have them installed.
``` 
npm install --global gulp-cli
npm install --global bower
```
2. In the `Tools/org.secc.MySecc.Theme` folder, install the node dependencies and the bower dependencies.
```
npm install
bower install
```
3. This project uses a customized version of the [useref](https://github.com/jonkemp/useref) package to allow a transformation of output css and js paths. This is necessary to include `<%#ResolveRockUrl(...) %>` style css/js tags.
Copy the `useref/` folder from the root of this project to the `node_modules/gulp-useref/node_modules` folder, overwriting the existing useref module there.
