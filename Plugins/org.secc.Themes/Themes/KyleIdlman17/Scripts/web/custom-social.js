//*-----------------------------------------------------------------------------------*/
/*	Social Networks Block
/*-----------------------------------------------------------------------------------*/
	
jQuery('.share-options a').click(function(e) {
	e.preventDefault();
});

// Twitter
function twitterSharer(){
	window.open( 'http://twitter.com/intent/tweet?text='+jQuery(".title-content h2").text() +' '+window.location, 
		"twitterWindow", 
		"width=650,height=350" );
	return false;
}

// Facebook

function facebookSharer(){
	window.open( 'https://www.facebook.com/sharer/sharer.php?u='+encodeURIComponent(location.href), 
		'facebookWindow', 
		'width=650,height=350');
	return false;
}		

// Pinterest

function pinterestSharer(){
	window.open( 'http://pinterest.com/pin/create/bookmarklet/?media='+ jQuery('.begin-content img').first().attr('src') + '&description='+jQuery('.title-content h2').text()+' '+encodeURIComponent(location.href), 
		'pinterestWindow', 
		'width=750,height=430, resizable=1');
	return false;
}	


// Google Plus

function googleSharer(){
	window.open( 'https://plus.google.com/share?url='+encodeURIComponent(location.href), 
		'googleWindow', 
		'width=500,height=500');
	return false;
}	


// Delicious

function deliciousSharer(){
	window.open( 'http://delicious.com/save?url='+encodeURIComponent(location.href)+'?title='+jQuery(".title-content h2").text(), 
		'deliciousWindow', 
		'width=550,height=550, resizable=1');
	return false;
}

// Linkedin

function linkedinSharer(){
	window.open( 'http://www.linkedin.com/shareArticle?mini=true&url='+encodeURIComponent(location.href)+'&title='+jQuery("h1").text(), 
		'linkedinWindow', 
		'width=650,height=450, resizable=1');
	return false;
}