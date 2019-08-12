jQuery(document).ready(function() {	
	// Responsive Navigation 

	var nava = jQuery(".nav-btn"),
		navb = jQuery("#navigation"),
		wind = jQuery(window).width(),	
		winh;
		
		if(wind < 1007) {
			 winh = jQuery(window).outerHeight()
		}
		else {
			winh = jQuery(window).outerHeight() -jQuery('#header').outerHeight()
		}

	// Add classes		
    jQuery(window).resize(function () {
		var nava = jQuery(".nav-btn"),
			navb = jQuery("#navigation"),
			wind = jQuery(window).width(),
			winh;

		if(wind < 1007) {
			 winh = jQuery(window).outerHeight()
		}
		else {
			winh = jQuery(window).outerHeight() -jQuery('#header').outerHeight()
		}
		
		if (wind > 1006) {
			navb.addClass("desktop");
			navb.removeClass("mobile")
		}
		if (wind < 1007) {
			navb.addClass("mobile");
			navb.removeClass("desktop")
		}

		// Nav CSS adjustment for mobile
		if (wind < 1007) {
		jQuery('#navigation.mobile').css({'max-height': winh-150, 'overflow-y': 'scroll'});
		}
		if (wind > 1006) {
			jQuery('#navigation.desktop').css({'overflow': 'visible'});
		}		

    });
			
		if (wind > 1006) {
			navb.addClass("desktop");
			navb.removeClass("mobile")
		}
		if (wind < 1007) {
			navb.addClass("mobile");
			navb.removeClass("desktop")
		}	
		// Nav CSS adjustment for mobile
		if (wind < 1007) {
		jQuery('#navigation.mobile').css({'max-height': winh-150, 'overflow-y': 'scroll'});
		}
		if (wind > 1006) {
			jQuery('#navigation.desktop').css({'overflow': 'visible'});
		}				

	// Click Tweak	
	nava.click(function () {
		if (navb.is(":visible")) {
			navb.slideUp()
		} else {
			navb.slideDown()
		}
	});	
});