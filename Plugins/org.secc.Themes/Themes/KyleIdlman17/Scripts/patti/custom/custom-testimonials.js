// Testimonials Slider
jQuery(window).load(function() {	

	jQuery('.testimonials-slider[id^="owl-testimonials"]').each( function() { 	

		var $div = jQuery(this);
		var token = $div.data('token');
		var bool = true;

		var settingObj = window['dt_testimonials_' + token];	
		if((settingObj.testimonial_speed == '') || (settingObj.testimonial_speed == 'false')) {
			bool = false;
		}
		jQuery("#owl-testimonials-"+settingObj.id+"").owlCarousel({
			autoHeight : true,
			items : 1,
			nav: true,
			rewind: true,
			rtl: false,
			autoplay: bool,
			autoplayTimeout: settingObj.testimonial_speed,			
			autoplayHoverPause: true,
			dots: true,
			smartSpeed: 800
		});
	});
});