const gulp = require('gulp');
const browserSync = require('browser-sync');
const del = require('del');
const pngquant = require('imagemin-pngquant');
const lintReporter = require('jshint-stylish');
const args = require('yargs').argv;
const $ = require('gulp-load-plugins')();
const map = require('map-stream');
const beep = require('beepbeep');

const SRC_PATH = "src";
const ERROR_LEVELS = ['verbose', 'info', 'warning', 'debug', 'error'];
const CONFIG = loadConfig();



/**
 * Build Modes
 *
 * Build Modes are used to set options for various tasks that are run.
 * For example, we might want to output expanded css files during development, and compressed css for production
 */
const BUILD_MODE_DEV = "dev";
const BUILD_MODE_DIST = "dist";

/**
 * The buildMode variable is used to set the current build mode (see above section on Build Modes).
 * See the enable-dist task for more information.
 *
 * default: BUILD_MODE_DEV
 */
var buildMode = BUILD_MODE_DEV;

/**
 * The proxyServer is our instance of Browser Sync, which is responsible for reloading browser windows
 * when files change. It is initialized in the browser-sync task
 */
var proxyServer = null;

/**
 * The logLevel variable sets the level at which logging to the console occurs. 
 * For instance, if the errorLevel is set to 'error', then anything marked 'error', 'warning', or 'info'
 * will be logged, and antying marked 'verbose' or 'debug' will NOT be logged.
 *
 * The logLevel is set based on the --logLevel command line option, and defaults to 'error'
 */
var logLevel = args.logLevel || 'error';

/**
 * The combinedCSS array will store a list of css files that are processed by the minify task. 
 * Then the ____ task will use this list to minify any additional css files and copy them to 
 * the output folder.
 * Used only when buildMode == BUILD_MODE_DIST
 */
var combinedCSS = [];


/**
 *  ROOT TASKS (Called from the command line)
 *
 *	The set of tasks below define the base tasks that we call from the command line.
 *	These tasks aggregate the other tasks defined below to form various build chains.
 *
 *	Included Tasks:
 * 
 *  - watch (default)
 *	- build
 *
 */


// set the default task to the 'watch' task. The default task is run when you run gulp with no other arguments.
gulp.task('default', ['watch']);


/**
 * WATCH
 *
 * The watch task is responsible for watching for file changes and performing basic builds in response. 
 * This includes compiling less to css, and handling any browser refreshes that might be needed.
 *
 * Note that this task, while used for development, is dependent on less-dist. This is on purpose. 
 * The first time that less is run, we need to be sure that the clean task is run first (ie. there needs to be
 * a dependancy on clean the first time). The less-dist task provides this. Subsequently, when watch detects
 * file changes, it will call the less-dev task, to avoid cleaning out all compiled stylesheets.
 */
gulp.task('watch', ['clean', 'browser-sync', 'less-dist', 'lint-js'], function() {
	buildMode = BUILD_MODE_DEV;
	gulp.watch(SRC_PATH + "/Styles/**/*.less", ['less-dev']);
	gulp.watch(SRC_PATH + "/**/*.+(html|aspx|Master)").on('change', proxyServer.reload);
	gulp.watch(SRC_PATH + "/Scripts/**/*.js", ['lint-js']).on('change', proxyServer.reload);
});


/**
 * BUILD
 *
 * The build task is responsible for doing a complete build of the site for a push to the LIVE site.
 * This includes copying all necessary files to the dist folder, compiling less, compressing images, 
 * minifying and concatinating scripts, etc.
 * Running this task will result in a complete production ready copy of the site in the project's dist/ folder.
 *
 * usage: gulp build --[staging|live|local]
 * args:
 *			--staging   - Perform a build for the staging server
 *			--live 		- Perform a bulid for the live server
 *			--local 	- Perform a build for your local machine (this is probably never used)
 *
 */
gulp.task('build', ['enable-dist', 'clean', 'copy-files', 'compress-images', 'less-dist', 'minify', 'extra-css']);



/* ========= END ROOT TASKS ========= */


/** 
 * ENABLE-DIST
 *
 * Used by the build task, this task toggles the buildMode variable to the "dist" build mode.
 *
 */
gulp.task('enable-dist', function() {
	buildMode = BUILD_MODE_DIST;
});


/**
 * BROWSER-SYNC
 *
 * The browser-sync task is responsible for setting up and starting the browser-sync service, 
 * which will be run in proxy mode to connect to our main development web site.
 */
gulp.task('browser-sync', function() {
	proxyServer = browserSync.create();
	proxyServer.init({
		proxy: CONFIG.src_host,
		xip: false,
		host: CONFIG.src_host,
		port: 8080,
		ghostMode: true
	});
});

/**
 * CLEAN
 *
 * The clean task is responsible for cleaning out (deleting) generated files before doing a build
 * This may include the css folder for development or the dist folder for a production build.
 */
gulp.task('clean', function() {
	var path = [];

	path.push(SRC_PATH + "/Styles/**/*.css");
	if (buildMode == BUILD_MODE_DIST) {
		path.push(CONFIG.dist_path + "/**/*");
	}
	return del.sync(path);
});


/** LESS-DEV and LESS-DIST
 *
 * We define 2 tasks for compiling less stylesheets. Both end up calling the same function, less().
 * Two different tasks are defined, as a production build has a different set of dependencies than
 * a development build.
 * 
 * less()
 * The less() function is responsible for compiling all less stylesheets into output css. 
 * depending on the build mode, it will use different compression and sourcemap settings. 
 *
 * less-dist
 * The less-dist task will ensure that clean is called before the less() compilation.
 * See the watch task for a use case in which this task is used for development.
 *
 * less-dev
 * less-dev has no dependencies, so it will call less() right away.
 */
gulp.task('less-dist', ['clean'], less);
gulp.task('less-dev', less);

function less() {
	var opts = {};
	var outputPath = SRC_PATH + "/Styles";
	var createSourceMaps = (buildMode == BUILD_MODE_DEV);

	opts.paths = [CONFIG.rock_path + "\\Themes\\Spark\\Styles", CONFIG.rock_path + "\\Themes\\Spark\\Styles\\_inc"];
	
	const SRC_FILES = [
		SRC_PATH + "/Styles/**/*.less",
		"!"+SRC_PATH + "/Styles/**/_*.less"
	];

	var stream = gulp.src(SRC_FILES)
		.pipe($.if(createSourceMaps, $.sourcemaps.init()))
		.pipe($.less(opts).on('error', function(error){
			handleError(this, 'error', false, error.message, error.filename, error.line, error.column, error.plugin);
		}))
		.pipe($.autoprefixer())
		.pipe($.if(createSourceMaps, $.sourcemaps.write('./sourcemaps')))
		.pipe(gulp.dest(outputPath));

	if (proxyServer != null) {
		stream.pipe(proxyServer.stream({match: "**/*.css"}));
	}

	return stream;
}


/**
 *	MINIFY
 *
 * The minify task is responsible for minification and concatination of resource files (css, js, etc).
 * It looks for <!--build --> comments in source aspx/html files and performs the appropriate concatination, minification, and renaming.
 * The task is setup to concatinate all <!--build--> blocks, (leaving 3rd party libaries untouched).
 * 
 * Note: this task will not uglify any js files in the /lib/ folder. If you have js (or other) files that you do not want minified/uglified
 * (for example, a 3rd party library that is already minified), make sure it is in a subfolder of /lib. These files will still be concatinated,
 * but they will NOT be minified/uglified.
 */
gulp.task('minify', ['clean', 'less-dist'], function() {
	const SRC_FILES = [
		SRC_PATH + '/**/*.+(html|aspx|Master)',
		'!' + SRC_PATH + '/bower/**/*.*'
	];

	const JS_FILTER = $.filter(["**/*.js", "!**/lib/**/*.js"], {restore: true});
	const CSS_FILTER = $.filter(["**/*.css", "!**/lib/**/*.css"], {restore: true});


	var opts = {
		transformPath: function(filePath) {
			var index;
			var relPath = null;

			index = filePath.indexOf("~~");
			if (index > -1) {
				relPath = filePath.substring(index+2);

				/*
				 * Add the path for this file to the combinedCSS array
				 * so that it will be skipped if/when the EXTRA CSS task is run.
				 */
				combinedCSS.push("!"+SRC_PATH + relPath);

				return SRC_PATH + relPath;
			}

			index = filePath.indexOf("~");
			if (index > -1) {

				relPath = filePath.substring(index+1);

				return CONFIG.rock_path + relPath;
			}

			return filePath;
		},

		transformOutputPath: function(filePath, buildType, extras, searchPaths) {
			
			var addVersion = filePath.endsWith(".css") ? CONFIG.version_styles : CONFIG.version_scripts;

			if (extras) {
				extras = extras.toLowerCase().replace(/: /g, ":");
				if (extras.indexOf("fingerprint:true") > -1) {
					addVersion = true;
				}
			}

			return '<%# ResolveRockUrl("~~' + filePath + '", ' + addVersion + ') %>';
		}
	}

	var stream = gulp.src(SRC_FILES)
		.pipe($.useref(opts))
		.pipe(JS_FILTER)
		.pipe($.uglify())
		.pipe(JS_FILTER.restore)
		.pipe(CSS_FILTER)
		.pipe($.cleanCss({keepSpecialComments: 0}))
		.pipe(CSS_FILTER.restore)
		.pipe(gulp.dest(CONFIG.dist_path));

	return stream;
});

/**
 *	EXTRA CSS
 *
 *  The Minify task above only handles concatination and minification of css files that are referenced
 *  via <!-- build --> blocks in your html/aspx files. However, you may have additional css files that
 *  are created and referenced within the Rock CMS (such as via the HeaderContent setting on a Rock Page).
 *
 *  This task will minify all css files that are NOT handled by the Minify task above, and place the 
 *  minified version in the dist folder. 
 */
gulp.task('extra-css', ['minify'], function() {
	combinedCSS.splice(0,0,SRC_PATH+"/Styles/**/*.css");
	gulp.src(combinedCSS)
		.pipe($.cleanCss({keepSpecialComments: 0}))
		.pipe(gulp.dest(CONFIG.dist_path + "/Styles"));

});






/**
 * LINT-JS
 *
 * The lint-js task is responsible for running the jshint linter on our javascript
 *
 * This task will only lint files in the /Scripts folder, and will lint all .js files
 * in that folder. As a result, if you use 3rd party librarys (which will often fail linting),
 * or if you have a javascript file that for whatever reason should not be linted (though I
 * can't imagine what a good reason would be), they should go elsewhere (like /lib/)
 */
gulp.task('lint-js', function() {
	const SRC_FILES = [
		SRC_PATH + "/Scripts/**/*.js",
		SRC_PATH + "/**/*.html",
		"!" +SRC_PATH + "/bower/**/*.*"
	];

	gulp.src(SRC_FILES)
		.pipe($.jshint.extract('auto'))
		.pipe($.jshint())
		.pipe($.jshint.reporter(lintReporter, {beep: true, verbose: true}))
		.pipe($.if((buildMode === BUILD_MODE_DIST), $.jshint.reporter('fail')));
});


/**
 *  COMPRESS IMAGES
 *
 *  Responsible for copying all images from SRC_PATH to DIST_PATH
 *  Along the way, it will do the following:
 *  1. Compress some images (jpgs, pngs, gifs, svgs). Note other images will still be copied
 *  2. Get the appropriate set of favicons based on command line arguements
 *		(pass in --staging or --dev for the red staging icon, 
 *		 or --local for the blue local icon. defaults to using the live icon)
 *
 *  Note that this task will handle all favicon files,
 *	including those that aren't images (like browserconfig.xml, etc)
 */
gulp.task('compress-images', ['clean'], function() {

	//Set options for image compresssion
	const opts = {
		progresssive: true,
		use: [pngquant()]
	};

	//Initially, get all images, including favicons, but not those in bower packages
	var src_files = [
		SRC_PATH+"/**/*.+(jpg|jpeg|png|gif|webp|bmp)",
		//SRC_PATH+"/_/img/icons/favicon/**/*.*",
		"!"+SRC_PATH+"/bower/**/*.*"
	];


	/* Logic for dev/staging/live favicons SECC.ORG. Keeping it here for now in case we want to use it for Rock too */

	//Now, filter the favicon files to include only those we need
	//based on the cli args. This is accomplished by removing those we don't need.
//	if (args.staging || args.dev) {
//		src_files.push("!"+SRC_PATH+"/_/img/icons/favicon/live/**/*.*");
//		src_files.push("!"+SRC_PATH+"/_/img/icons/favicon/*.*");
//	} else if (args.local) {
//		src_files.push("!"+SRC_PATH+"/_/img/icons/favicon/live/**/*.*");
//		src_files.push("!"+SRC_PATH+"/_/img/icons/favicon/staging/**/*.*");
//	} else {
//		src_files.push("!"+SRC_PATH+"/_/img/icons/favicon/staging/**/*.*");
//		src_files.push("!"+SRC_PATH+"/_/img/icons/favicon/*.*");
//	}

	//Setup a couple filters that we'll use to only perform opertations on certain sets of files.
	//one to get only images for compresssion
	//another to get only favicon files
	var filterCompressedImages = $.filter("**/*.+(jpg|jpeg|png|gif)", {restore: true});
	//var filterFavicons = $.filter(SRC_PATH+"/_/img/icons/favicon/**/*.*", {restore: true});


	//We're ready to process the files now, in order:
	// 1. Get all the files
	// 2. Filter only images for compression
	// 3. Compress those images
	// 4. Filter only favicon files
	// 5. Update the favicon output paths
	// 6. output all files to the dist folder
	gulp.src(src_files)
		.pipe(filterCompressedImages)
		.pipe($.imagemin(opts))
		.pipe(filterCompressedImages.restore)
		//.pipe(filterFavicons)
		//.pipe($.rename({dirname: "_/img/icons/favicon"}))
		//.pipe(filterFavicons.restore)
		.pipe(gulp.dest(CONFIG.dist_path));
});


/** 
 *
 * COPY-FILES
 * The copy-files task is reponsible for copying any files that need to wind up in the dist folder but aren't handled by any other processing.
 */
gulp.task('copy-files', ['clean'], function() {
	gulp.src(SRC_PATH+"/Assets/**/*.*", {base: SRC_PATH})
	.pipe(gulp.dest(CONFIG.dist_path));

	gulp.src(SRC_PATH+"/Layouts/**/*.cs", {base: SRC_PATH})
	.pipe(gulp.dest(CONFIG.dist_path));
});


/** ======== HELPER FUNCTIONS ========= */


/**
 * HANDLE ERROR
 * 	
 * This function is responsible for handling and logging errors. 
 */
function handleError(context, level, isFatal, message, file, line, column, plugin) {
	isFatal = (buildMode == BUILD_MODE_DIST) || isFatal || false;

	if (isFatal || showError(level)) {
		beep();

		var output = "";

		output = $.util.colors.white.bgRed(" ", level.toUpperCase(), " ");
		if (plugin) {
			output += $.util.colors.white.bgRed("in plugin: " + plugin + " ");
		}
		$.util.log(output);

		if (file) {
			$.util.log($.util.colors.yellow("File: "), "\t", $.util.colors.underline(file));
		}

		if (line) {
			$.util.log($.util.colors.yellow("Line: "), "\t", $.util.colors.magenta(line));
		}

		if (column) {
			$.util.log($.util.colors.yellow("Column: "), "\t", $.util.colors.magenta(column));
		}

		if (message) {
			$.util.log($.util.colors.yellow("Message: "), message, "\n");
		}
	}

	if (isFatal) {
		process.exit(1);
	} else {
		context.emit('end');
	}
}


/**
 * SHOW ERROR
 *
 * Determines whether or not an error of the given level should be displayed.
 *
 * @param level {string} - the level of the error
 *
 * @return {boolean} - true if the error should be displayed.
 *
 */ 
function showError(level) {
	if (isNaN(level)) {
		level = ERROR_LEVELS.indexOf(level);
	}

	return (level >= ERROR_LEVELS.indexOf(logLevel));

}


/**
 * LOAD CONFIG
 *
 * Loads the configuration files in default.config.json and user.config.json
 * and sets a single set of configuration values.
 */
function loadConfig() {
	var defaultConfig = require('./default.config.json');
	var user = require('./user.config.json');

	//Update the default values, overriding them with
	//values set in the user config.
	for (var key in defaultConfig) {
		if (user.hasOwnProperty(key)) {
			defaultConfig[key] = user[key];
		}
	}

	//Now that the default has been updated with user settings, return it.
	return defaultConfig;
}
