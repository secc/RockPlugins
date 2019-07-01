jQuery(document).ready(function(){
 
        jQuery("body").queryLoader2({
            onComplete: function() {},
            onLoadComplete: function() {},
            backgroundColor: "#fff",
            barColor: dt_loader.bcolor,
            overlayId: 'qLoverlay',
            barHeight: 4,
            percentage: false,
            deepSearch: true,
            completeAnimation: "fade",
            minimumTime: 500    
        });

        jQuery("#qLoverlay").css({'display':'none'});

        // Rev Slider default heights
        var fullscreenheight = jQuery(window).height() - jQuery('#header').outerHeight();
        jQuery('.rev_slider_wrapper .fullscreenbanner').css({'height': fullscreenheight});
        jQuery('.rev_slider_wrapper .fullscreenbanner ul').css({'height': fullscreenheight});   

});