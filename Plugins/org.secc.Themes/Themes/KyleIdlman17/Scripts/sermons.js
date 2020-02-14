
	var sermonOffset = 0;

	function getMoreSermons($) {
		console.log("loading sermons. offset: " + sermonOffset);
		$(".list-footer a").hide();
		$("#loading-animation").show();
		$.ajax({
			url: "/api/sermonfeed?speaker=kyle+idleman&offset="+ sermonOffset,
			async: true,
			success: function(data) {

				$.each(data, function(i, series){
		            $('<a></a>', {
						"class": "list-item",
						"href": "/sermons/"+series.slug.toLowerCase(),
					}).append(
						$('<div/>', {
							"class": 'sermon-series',
						}).html(function() {
							var box =  "<img src=\"" + series.image_url + "\" class=\"sermons-img\">";
							box += "<div class=\"sermons-title\">";
							box += series.title;
							box += "</div>";
							box += "<div class=\"sermons-description\">";
							box += series.description;
							box += "</div>";
							box += "<div class=\"sermons-count\">";
							box += series.sermons.length + " sermon";
							if(series.sermons.length > 1){
								box += "s";
							}
							box += "</div>";
							return box;
						})
					).appendTo('#series-list');
				}); // end .each()

				$("#loading-animation").hide();
				sermonOffset += 12;
				$(".list-footer a").show();

			} // end success
		}); // end ajax()
	} // end getMoreSermons()
