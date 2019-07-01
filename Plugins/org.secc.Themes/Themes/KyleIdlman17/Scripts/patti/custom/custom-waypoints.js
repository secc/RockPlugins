jQuery.logThis = function( text ){
    if( (window['console'] !== undefined) ){
        console.log( text );
    }
}

// Counting Numbers
function counts() {
	if(!( /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent) )) {	
		jQuery('.counter-wrapper').waypoint(function() {
			jQuery('.counter-number').countTo();	
		}, 
		{ 
			offset: '90%',
			triggerOnce: true
		});
	}

	else {
		jQuery('.counter-number').countTo();	
	}
}

jQuery(window).load(function() {
	// Fixes some Waypoint issues
	jQuery('body').waypoint(function() {
		jQuery.logThis('ready to go');
	}, 
	{ 
		triggerOnce: true
	});
});

jQuery(document).ready(function() {
	counts();
});