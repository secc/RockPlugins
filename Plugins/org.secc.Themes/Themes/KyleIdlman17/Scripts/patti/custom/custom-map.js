jQuery(document).ready(function() {
					
	jQuery('.map-wrapper[id^="delicious_map_"]').each( function() { 

		var $div = jQuery(this);
		var token = $div.data('token');
		var settingObj = window['dt_map_' + token];
		var id = settingObj.id;
		var zoomv = parseInt(settingObj.zoom);

		jQuery("#delicious_map_"+settingObj.id+" .button-map").click(function() {
			var latlng = new google.maps.LatLng(settingObj.latitude,settingObj.longitude);
			var settings = {
				zoom: zoomv,
				center: new google.maps.LatLng(settingObj.latitude,settingObj.longitude), 
				mapTypeId: google.maps.MapTypeId.ROADMAP,
				mapTypeControl: false,
				scrollwheel: false,
				draggable: true,
				navigationControl: false
				};
				
			var map = new google.maps.Map(document.getElementById("google_map_"+settingObj.id+""), settings);
			google.maps.event.addDomListener(window, "resize", function() {
				var center = map.getCenter();
				google.maps.event.trigger(map, "resize");
				map.setCenter(center);
			});
			
			var contentString = '<div class="map-tooltip">'+
				'<h6>'+settingObj.pin_title+'</h6>'+
				'<p>'+settingObj.pin_desc+'</p>'+
				'</div>';
			var infowindow = new google.maps.InfoWindow({
				content: contentString
			});
			
			var companyImage = new google.maps.MarkerImage(settingObj.site_url+'/images/map-pin.png',
				new google.maps.Size(40,70),
				new google.maps.Point(0,0),
				new google.maps.Point(20,55)
			);
			
			var companyPos = new google.maps.LatLng(settingObj.latitude,settingObj.longitude);
			
			var companyMarker = new google.maps.Marker({
				position: companyPos,
				map: map,
				icon: companyImage, 
				zIndex: 3});	
			
			google.maps.event.addListener(companyMarker, 'click', function() {
				infowindow.open(map,companyMarker);
			});

			

	        jQuery("#google_map_"+settingObj.id+"").slideToggle(300, function(){
	                google.maps.event.trigger(map, "resize"); 
	                map.setCenter(latlng);
	            }); 
	        jQuery(this).toggleClass('close-map show-map');
	    });

	});
});		