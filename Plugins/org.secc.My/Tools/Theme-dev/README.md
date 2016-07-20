# My SECC Rock Theme

> A [RockRMS](http://www.rockrms.com) Theme for the My SECC External Website.

This project uses [gulp](http://gulpjs.com) to aid in development of the theme. The gulpfile is setup to watch the theme's .less files and compile them to css in realtime as changes are made. It also uses [browsersync](https://www.browsersync.io/) to allow instant updates of browser windows (without refresh) during development, as well as synced scrolling, form input etc between browsers on multiple devices.

The gulpfile also includes a build task to "compile" the theme for production use. In addition to compiling the theme's .less files to .css, it will combine and minify .css and .js files, and compress images.

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
Copy the `useref/` folder from the root of this project to the `node_modules/gulp-useref/node_modules` folder, overwriting the existing `useref` module there.

## Usage
For development, run gulp from the command line and work on the theme files in the `src` folder
```
gulp
```

To build for distribution to a production webserver:
```
gulp build
```

## Configuration
The project includes a configuration file, `config-default.json`. 

 **DO NOT EDIT THE VALUES IN THE DEFAULT CONFIGURATION FILE!!**

The default configuration file contains default values for all configuration parameters.

If you need to change the default configuration values (and you probably will), make a copy of the `config-default.json` and name it `config-user.json`. Any properties in the user config file will override their corresponding values in the default config file, and any properties that are missing from the user config file will use the value from the default config file.

### options

#### rock_path
Type: `string`
Default: `../../../../../rockrms/RockWeb`

This should be the path to the RockWeb folder in your local Rock installation. Note that if providing a fully qualified windows path, you should escape backslashes in the file path. For example `c:\\DEV\\WebProjects\\rock\\rockrms\\RockWeb`

#### src_host
Type: `string`
Default: `my.rocktheme.secc.org`

This is the hostname of your local site that you will use to test your theme during development.

#### dist_path
Type: `string`
Default: `../../Themes/my-secc`

This is the path (relative to the project folder) to your `RockWeb/Themes` folder where distribution builds of your theme should be output.

#### version_styles
Type: `boolean`
Default: `true`

Wether or not to direct Rock to include version querystring parameters when outputing production ready css link tags. Rock's default is to version css files, so this project's default is to do the same.

#### version_scripts
Type: `boolean`
Default: `true`

Wether or not to direct Rock to include version querystring parameters when outputing production ready javascript script tags. Rock's default is to version js files, so this project's default is to do the same.