// Teams Slider
jQuery(document).ready(function() {

	jQuery('.teams-slider[id^="owl-teams"]').each( function() {

		var $div = jQuery(this);
		var bool = true;

		jQuery("#owl-teams").owlCarousel({
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
		            items: 3
		        }
		    },
			nav: true,
			rewind: true,
			rtl: false,
			autoplay: false,
			autoplayTimeout: "",
			autoplayHoverPause: true,
			dots: true,
			smartSpeed: 800
		});

	});
});
