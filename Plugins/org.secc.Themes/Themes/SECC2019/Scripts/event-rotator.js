$(document).ready(function() {
	$(".event .event-header, .event .event-details").click(function() {
		var $event = $(this).parent(".event");
		if ($event) {
			var link = $event.data("link");
			if (link) {
				window.location.href = link;
			}
		}
	});


});
