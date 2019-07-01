// Portfolio Gallery Slider
jQuery(window).load(function() {	

	jQuery('.carousel-clients[id^="owl-clients-"]').each( function() { 	

		var $div = jQuery(this);
		var token = $div.data('token');
		var bool = true;

		var settingObj = window['dt_clients_' + token];	
		if(settingObj.clients_speed == 'false') {
			bool = false;
		}

		jQuery("#owl-clients-"+settingObj.id+"").owlCarousel({
			autoHeight : true,
		    responsive:{
		        0:{
		            items:1
		        },
		        480:{
		            items:2
		        },
		        768:{
		            items:3
		        },
		        1024:{
		            items: settingObj.items_per_row
		        }		
		    },
			nav: true,
			rewind: true,
			rtl: false,
			autoplay: bool,
			autoplayTimeout: settingObj.clients_speed,
			autoplayHoverPause: true,
			dots: true,
			smartSpeed: 800			
		});	
	});
});