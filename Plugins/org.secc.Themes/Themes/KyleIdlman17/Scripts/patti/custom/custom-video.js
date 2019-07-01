jQuery(document).ready(function() {

	// VIDEO PLAYER //
	jQuery('.del_video[id^="video_jplayer_"]').each( function() { 	
	
		var $div = jQuery(this);
		var token = $div.data('token');
		var settingObj = window['dt_video_' + token];	
	
		jQuery("#video_jplayer_"+settingObj.post_id+"").jPlayer({
			ready: function (event) {
				jQuery(this).jPlayer("setMedia", { 
					m4v:""+settingObj.mp4_item+"",
					ogv:""+settingObj.ogv_item+"",
					poster: ""+settingObj.vposter+""
				});
			},
			play: function() { // To avoid multiple jPlayers playing together.
				jQuery(this).jPlayer("pauseOthers");
			},		
			swfPath: ""+settingObj.spath+"",
			supplied: "m4v, ogv, all",
			cssSelectorAncestor: "#jp_video_container_"+settingObj.post_id+"",
			smoothPlayBar: true
		});
	});	
});