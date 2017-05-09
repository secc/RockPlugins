/* global ga */

var BREAKPOINTS = {
	'extraSmall' : 480,
	'small' : 768,
	'medium' : 992,
	'large': 1200
};

var DEVICE_SIZES = {
	phone: {min: BREAKPOINTS.extraSmall, max: (BREAKPOINTS.small - 1)},
	tablet: {min: BREAKPOINTS.small, max: (BREAKPOINTS.medium - 1)},
	desktop: {min: BREAKPOINTS.medium, max: (BREAKPOINTS.large - 1)},
	largeDesktop: {min: BREAKPOINTS.large, max: 10000}
};


/* exported TrackEvent */
function TrackEvent(category, action, label, url) {
	try {
		ga('send', 'event', category, action, label, {'hitCallback:':
			function() {
				if (url) {
					document.location.href = url;
				}
			}
		});
		return true;
	} catch(e) {
		//Do nothing, it's just a track event.
	}
}


/* exported formatPhone */
function formatPhone(phone) {
	var code;
	var exch;
	var ext;
	phone = phone.replace(/\D/g, '');


	if (phone.length === 7) {
		code = "";
		exch = phone.substring(0, 3) + "-";
		ext = phone.substring(3);
	} else if (phone.length >= 10) {
		code = "(" + phone.substring(0, 3) + ") ";
		exch = phone.substring(3, 6) + "-";
		ext = phone.substring(6, 10);
	} else {
		code = phone;
		exch = "";
		ext = "";
	}

	return code + exch + ext;
}

/* exported slugify */
function slugify(text)
{
	if (!text) { return ""; }

	return text.toString().toLowerCase()
		.replace(/\s+/g, '-')           // Replace spaces with -
		.replace(/[^\w\-]+/g, '')       // Remove all non-word chars
		.replace(/\-\-+/g, '-')         // Replace multiple - with single -
		.replace(/^-+/, '')             // Trim - from start of text
		.replace(/-+$/, '');            // Trim - from end of text
}

/* exported isPhone */
function isPhone() {
	if ($(window).width() <= DEVICE_SIZES.phone.max) {
		return true;
	}
	return false;
}

/* exported isTablet */
function isTablet() {
	if ($(window).width() >= DEVICE_SIZES.tablet.min && $(window).width() <= DEVICE_SIZES.tablet.max) {
		return true;
	}
	return false;
}

/* exported isDesktop */
function isDesktop() {
	if ($(window).width() >= DEVICE_SIZES.desktop.min) {
		return true;
	}
	return false;
}

/* exported resizeFrame */
function resizeFrame(frameSelector) {
	var domain = document.domain;
	if (domain.indexOf('southeastchristian.org') >= 0) {
		document.domain = 'southeastchristian.org';
		var $frame = $(frameSelector);
		var height = $frame.prop('contentWindow').document.body.scrollHeight;
		var width = $frame.prop('contentWindow').document.body.scrollWidth;

		$frame.css('height', height + "px");
		$frame.css('width', width + "px");
	}
}

/* exported watchInputs */
function watchInputs(frameSelector, inputsSelector) {
	var domain = document.domain;
	if (domain.indexOf('southeastchristian.org') >= 0) {
		document.domain = 'southeastchristian.org';
		var $frame = $(frameSelector);
		var $inputs = $(inputsSelector, $frame.contents());

		if ($inputs.length > 0) {
			$inputs.click(function() {
				setTimeout(function() {
					resizeFrame(frameSelector);
				}, 1000);
			});
		}
	}
}

/* exported openWindow */
function openWindow(url, name) {
	name = name || "_blank";
	TrackEvent("New Window", name, url);
	window.open(url, name);
}

function bindAccountMenu(el) {
	el.click(function() {
		var $AccountMenu = $("#AccountMenu > nav");

		$("#LocationsMenu").removeClass("active");
		$(".locations-toggle").removeClass("active");
		$(this).toggleClass("active");
		$AccountMenu.toggleClass("active");

	});

	$(document).click(function(event) {
		var $AccountMenuButton = $(".account-menu-toggle");
		if(!$(event.target).closest('.account-menu-toggle').length) {
	        if($AccountMenuButton.hasClass("active")) {
			$("#AccountMenu > nav").removeClass("active");
		  	  $AccountMenuButton.removeClass("active");
	        }
	    }
	});
}
