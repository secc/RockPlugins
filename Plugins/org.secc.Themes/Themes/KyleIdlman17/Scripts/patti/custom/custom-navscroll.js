jQuery(document).ready(function() {

	// Header Effect on Scroll

	var op1 = '0.'+dt_styles.header_scroll_opacity+'';
	if(dt_styles.header_scroll_opacity == 100) {
		op1 = ''+dt_styles.header_scroll_opacity+'';
	}

	var def_color = dt_styles.default_color;

	jQuery(".transparent-header").css({"background":"rgba("+dt_styles.header_bg+","+op1+")", "box-shadow": "none"});
	jQuery(".transparent-header ul#mainnav li ul li a").css({"background":"rgba("+dt_styles.header_bg+","+op1+")", "box-shadow": "none"});
	jQuery("#header.solid-header ul#mainnav li ul li a").css({'background': 'rgba('+dt_styles.header_bg+', 1)'});

	if (!jQuery('body').hasClass("home")) {
    	if(jQuery('#header').hasClass('tr-header')) {
    		jQuery('#header').removeClass();
    		jQuery('#header').addClass('transparent-header')
    	}
	}

	if(dt_styles.scrolling_effect != 0) {
		jQuery(window).scroll( function() {
			var value = jQuery(this).scrollTop();
			if ( value > 120 )	{
				jQuery("#header").addClass("scrolled-header");
				jQuery("#header").css({"padding-top": dt_styles.scroll_pt+"px", "padding-bottom": dt_styles.scroll_pb+"px"});
				
				jQuery(".scrolled-header").css({"background":"rgba("+dt_styles.header_bg+", "+op1+")", "box-shadow": "0px 0px 3px rgba(0, 0, 0, 0.3)"});
				jQuery(".no-rgba .scrolled-header").css({"background": def_color});
				jQuery(".logo img").css({"height": "30px", "width": "auto"});
				jQuery(".scrolled-header #mainnav").css({"padding-top": "2px"});
				jQuery(".scrolled-header ul#mainnav li ul li a").css({'background': 'rgba('+dt_styles.header_bg+', '+op1+')'});
				jQuery(".no-rgba .scrolled-header ul#mainnav li ul li a").css({"background": def_color});
				
				jQuery("#header.no-header").addClass("show");
				
				jQuery(".no-csstransforms .no-header").css({"display": "block"});

				if(dt_styles.alternativelogo == 1) {
					jQuery("#header.scrolled-header .logo img").attr("src",""+dt_styles.alternativelogosrc+"");
				}
			
			}
			else {
				jQuery("#header").removeClass("scrolled-header");
				jQuery("#header").css({"padding-top": dt_styles.init_pt+"px", "padding-bottom": dt_styles.init_pb+"px"});
				jQuery(".logo img").css({"width": dt_styles.logo_width, "height": dt_styles.logo_height});
				jQuery("#header #mainnav").css({"padding-top": "10px"});
				jQuery(".home #header.no-header .logo img").css({"height": "30px", "width": "auto"});
				
				jQuery(".transparent-header").css({"background":"rgba("+dt_styles.header_bg+","+op1+")", "box-shadow": "none"});
				jQuery(".tr-header").css({"background":"transparent", "box-shadow": "none"});
				jQuery(".no-rgba .transparent-header").css({"background": def_color});
				jQuery(".solid-header, .no-header").css({"background":"rgb("+dt_styles.header_bg+")", "box-shadow": "none"});
				
				// jQuery("#header.no-header .logo img").css({"height": "30px", "width": "auto"});
				jQuery("#header.no-header").removeClass("show");
				jQuery(".no-csstransforms .no-header").css({"display": "none"});		

				jQuery("#header.solid-header ul#mainnav li ul li a").css({'background': 'rgba('+dt_styles.header_bg+', 1)'});		

				if(dt_styles.alternativelogo == 1) {
					if(dt_styles.svglogo != '') {
						jQuery("#header .logo img").attr("src",""+dt_styles.svglogo+"");
					}
					else {
						jQuery("#header .logo img").attr("src",""+dt_styles.mainlogosrc+"");
					}
					
				}
			}
		});	
	}
});