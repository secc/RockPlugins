jQuery(document).ready(function() {	

	// special tweaks for non-responsive option
	if( /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent) ) {
		jQuery('.page-template-template-homepage-php .wpb_row .vc_span12 .wpb_row, .page-template-template-blog-php .wpb_row .vc_span12 .wpb_row').css({'width': '100%'});
	}	

	jQuery('#header').css({'position': 'relative'});
	jQuery('.menu-fixer, #header.no-header').css({'display': 'none'});
	
});