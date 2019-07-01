
	var sermonOffset = 12;

	function getMoreSermons($) {
		console.log("loading sermons. offset: " + sermonOffset);
		$(".list-footer a").hide();
		$("#loading-animation").show();
		$.ajax( {
			url: "/api/sermonfeed?speaker=kyle+idleman",
			data: {loadmore: sermonOffset},
			async: true,
			success: function(html) {
				$("#loading-animation").hide();
				if (html != "") {
					$('#series-list').append(html);
					sermonOffset += 12;
					$(".list-footer a").show();
				}
			}

		});
	}
