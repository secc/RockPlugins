// Teams Slider
jQuery(document).ready(function() {	

	jQuery('.teams-slider[id^="owl-teams"]').each( function() { 	

		var $div = jQuery(this);
		var token = $div.data('token');
		var bool = true;

		var settingObj = window['dt_teams_' + token];	
		if((settingObj.team_speed == '') || (settingObj.team_speed == 'false')) {
			bool = false;
		}

		jQuery("#owl-teams-"+settingObj.id+"").owlCarousel({
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
			autoplayTimeout: settingObj.team_speed,
			autoplayHoverPause: true,
			dots: true,
			smartSpeed: 800
		});

	});
});