// Portfolio Gallery Slider
jQuery(window).load(function() {	

	jQuery('.portfolio-slider[id^="owl-slider-"]').each( function() { 	

		var $div = jQuery(this);
		var token = $div.data('token');
		var bool = true;

		var settingObj = window['dt_slider_' + token];	
		if(settingObj.slider_speed == 'false') {
			bool = false;
		}		

		jQuery("#owl-slider-"+settingObj.id+"").owlCarousel({
			autoHeight : true,
			items : 1,
			nav: true,
			navText: [
				  "<i class='fa fa-angle-left'></i>",
				  "<i class='fa fa-angle-right'></i>"
				  ],				
			rewind: true,
			autoplay: bool,
			rtl: false,
			autoplayTimeout: settingObj.slider_speed,	
			lazyLoad: settingObj.lazyload,		
			autoplayHoverPause: true,
			dots: true,
			smartSpeed: 800,
			onInitialized: owlcallback,
			onChanged: owlcallback
		});		

		function owlcallback(event){
		    var items     = event.item.count;     // Number of items
		    var item      = event.item.index;     // Position of the current item
		    jQuery('.slider-nav-'+settingObj.id+'').text(""+(item+1)+"/" + items+"");

		}	

	});
});