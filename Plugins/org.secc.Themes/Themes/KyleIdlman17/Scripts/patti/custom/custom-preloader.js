jQuery(window).load(function() {
	// Take care of intro loader
	jQuery("#spinner").delay(400).fadeOut(); 
	jQuery(".whitebg").delay(800).fadeOut("slow");	
});

jQuery(document).ready(function() { 

	//Leaving Page Fade Out Effect
	jQuery('a.external').click(function(e){
		var url = jQuery(this).attr('href');		
		e.preventDefault();		
	  		jQuery('.whitebg').fadeIn(400, function(){		 
				jQuery("#spinner").fadeIn(400);				
			    document.location.href = url;
		  	});
	  return false;
	});	
		
});