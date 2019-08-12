jQuery(document).ready(function() {

	// AUDIO PLAYER //
	jQuery('.del_audio[id^="audio_jplayer_"]').each( function() { 	
	
		var $div = jQuery(this);
		var token = $div.data('token');
		var settingObj = window['dt_audio_' + token];	
		
		jQuery("#audio_jplayer_"+settingObj.post_id+"").jPlayer({
			ready: function (event) {
				jQuery(this).jPlayer("setMedia", { 

					mp3: ""+settingObj.mp3_item+"",
					oga: ""+settingObj.ogg_item+""
				});
			},
			play: function() { // To avoid multiple jPlayers playing together.
				jQuery(this).jPlayer("pauseOthers");
			},		
			swfPath: ""+settingObj.spath+"",
			supplied: "mp3, oga, all",
			cssSelectorAncestor: "#jp_container_"+settingObj.post_id+"",
			wmode: "window"	
		});
	});	
});