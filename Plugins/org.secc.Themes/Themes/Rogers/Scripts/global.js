// Global Javascript Initialization
/*var Global = function() {
  'use strict';

  // Bootstra Components
  var handleBootstrapComponents = function() {
    // Bootstrap Carousel
    $('.carousel').carousel({
      interval: 5000,
      pause: 'hover'
    });

    // Tooltips
    $('.tooltips').tooltip();
    $('.tooltips-show').tooltip('show');
    $('.tooltips-hide').tooltip('hide');
    $('.tooltips-toggle').tooltip('toggle');
    $('.tooltips-destroy').tooltip('destroy');

    // Popovers
    $('.popovers').popover();
    $('.popovers-show').popover('show');
    $('.popovers-hide').popover('hide');
    $('.popovers-toggle').popover('toggle');
    $('.popovers-destroy').popover('destroy');
  }

  // Scroll To Section
  var handleScrollToSection = function() {
    $(function() {
      $("a[href*='#js__scroll-to-']").not("a[href='#js__scroll-to-']").on('click', function() {
        if (location.pathname.replace(/^\//, '') == this.pathname.replace(/^\//, '') && location.hostname == this.hostname) {
          var target = $(this.hash);
          target = target.length ? target : $('[name=' + this.hash.slice(1) + ']');
          if (target.length) {
            $('html,body').animate({
              scrollTop: target.offset().top - 90
            }, 1000);
            return false;
          }
        }
      });
    });
  }

  // Handle Promo Section
  var handlePromoSection = function() {
    $('.js__fullwidth-img').each(function() {
      $(this).css('background-image', 'url(' + $(this).children('img').attr('src') + ')');
      $(this).children('img').hide();
    });
  }

  // Handle Overlay
  var handleOverlay = function() {
    var overlay = $('.js__bg-overlay'),
      headerOverlay = $('.js__header-overlay'),
      trigger = $('.js__trigger');

    trigger.on('click', function() {
      overlay.toggleClass('-is-open');
      headerOverlay.toggleClass('-is-open');
      trigger.toggleClass('-is-active');
    });
  }

  // Vertical Center Aligned
  // Note! This works only with promo block and background image via CSS.
  var handleVerticalCenterAligned = function() {
    $('.js__ver-center-aligned').each(function() {
      $(this).css('padding-top', $(this).parent().height() / 2 - $(this).height() / 2);
    });
    $(window).resize(function() {
      $('.js__ver-center-aligned').each(function() {
        $(this).css('padding-top', $(this).parent().height() / 2 - $(this).height() / 2);
      });
    });
  }

  // handle group element heights
  var handleEqualHeight = function() {
   $('[data-auto-height]').each(function() {
      var parent = $(this);
      var items = $('[data-height]', parent);
      var height = 0;
      var mode = parent.attr('data-mode');
      var offset = parseInt(parent.attr('data-offset') ? parent.attr('data-offset') : 0);

      items.each(function() {
        if ($(this).attr('data-height') == "height") {
          $(this).css('height', '');
        } else {
          $(this).css('min-height', '');
        }

        var height_ = (mode == 'base-height' ? $(this).outerHeight() : $(this).outerHeight(true));
        if (height_ > height) {
          height = height_;
        }
      });

      height = height + offset;

      items.each(function() {
        if ($(this).attr('data-height') == "height") {
          $(this).css('height', height);
        } else {
          $(this).css('min-height', height);
        }
      });

      if(parent.attr('data-related')) {
        $(parent.attr('data-related')).css('height', parent.height());
      }
   });
  }

  return {
    init: function() {
      //handleBootstrapComponents(); // initial setup for Bootstrap Components
      //handleScrollToSection(); // initial setup for Scroll To Section
      //handlePromoSection(); // initial setup for Promo Section
      //handleOverlay(); // initial setup for Overlay
      //handleVerticalCenterAligned(); // initial setup for Vertical Center Aligned
      //handleEqualHeight(); // initial setup for Equal Height
    }
  }
}();*/

$(document).ready(function() {
  //Global.init();

  // scroll on any hash links
  var scroll = new SmoothScroll('a[href*="#"]', {
      // Selectors
      //header: '[data-scroll-header]',

      // Speed & Easing
      offset: ($('.cd-main-header').height() + 15), // Integer or Function returning an integer. How far to offset the scrolling anchor location in pixels
      easing: 'easeInOutCubic', // Easing pattern to use
  });

  // make it sticky
  var cmh = $(".cd-main-header");
      cmc = $(".cd-main-content");

      if (typeof ch !== 'undefined') {
          $(window).scroll(function() {
              /*
              Animation Scroll Project - WIP

              ch = $('.cd-hero').height() - (40);

              
              if ($(window).width() < 960) {
                  if( $(this).scrollTop() > ch ) {
                      cmh.addClass("nav-is-fixed");
                      cmc.addClass("nav-fixed-padding");
                      cmh.addClass("animated slideInDown");
                  } else if($(this).scrollTop() < ch - $('.cd-main-header').height()) {
                      cmh.removeClass("nav-is-fixed");
                      cmc.removeClass("nav-fixed-padding");
                      cmh.removeClass("animated slideInDown");
                  }
              } else {
                  if( $(this).scrollTop() > ch - $('.cd-main-header').height()) {
                      cmh.addClass("nav-is-fixed");
                      cmc.addClass("nav-fixed-padding");
                  } else {
                      cmh.removeClass("nav-is-fixed");
                      cmc.removeClass("nav-fixed-padding");
                  }
              }
              */
              if( $(this).scrollTop() > ch ) {
                  cmh.addClass("nav-is-fixed");
                  cmc.addClass("nav-fixed-padding");
              } else {
                  cmh.removeClass("nav-is-fixed");
                  cmc.removeClass("nav-fixed-padding");
              }
          });
      } else {
          ch = $('.cd-hero').height();
          $(window).scroll(function() {
              if( $(this).scrollTop() > ch ) {
                  cmh.addClass("nav-is-fixed");
                  cmc.addClass("nav-fixed-padding");
              } else {
                  cmh.removeClass("nav-is-fixed");
                  cmc.removeClass("nav-fixed-padding");
              }
          });
      }

      /*
     * Replace all SVG images with inline SVG
     */
        jQuery('img.svg').each(function(){
            var $img = jQuery(this);
            var imgID = $img.attr('id');
            var imgClass = $img.attr('class');
            var imgURL = $img.attr('src');

            jQuery.get(imgURL, function(data) {
                // Get the SVG tag, ignore the rest
                var $svg = jQuery(data).find('svg');

                // Add replaced image's ID to the new SVG
                if(typeof imgID !== 'undefined') {
                    $svg = $svg.attr('id', imgID);
                }
                // Add replaced image's classes to the new SVG
                if(typeof imgClass !== 'undefined') {
                    $svg = $svg.attr('class', imgClass+' replaced-svg');
                }

                // Remove any invalid XML tags as per http://validator.w3.org
                $svg = $svg.removeAttr('xmlns:a');

                // Replace image with new SVG
                $img.replaceWith($svg);

            }, 'xml');

        });
});
