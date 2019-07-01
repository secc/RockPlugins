// Flickr 
jQuery(document).ready(function(){						
	jQuery('#flickr').jflickrfeed({
		limit: dt_flickr.number,
		qstrings: {
			id: ''+dt_flickr.id+'',
			tags: ''+dt_flickr.tags+'',
		},
		itemTemplate: 
		'<li>' +
			'<a rel="prettyPhoto[pp_gal]" href="{{image_b}}"><img src="{{image_s}}" alt="{{title}}" /></a>' +
		'</li>' 
	}, function(data) {
		jQuery('#flickr a').prettyPhoto({animation_speed:'normal',theme:'light_square',slideshow:3000, autoplay_slideshow: false});
	});								
});