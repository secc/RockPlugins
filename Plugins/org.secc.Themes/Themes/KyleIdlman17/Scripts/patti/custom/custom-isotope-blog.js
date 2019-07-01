jQuery(document).ready(function() {
	// Isotope for Blog

	// modified Isotope methods for gutters in masonry
	jQuery.Isotope.prototype._getMasonryGutterColumns = function() {
		var gutter = this.options.masonry && this.options.masonry.gutterWidth || 0;
			containerWidth = this.element.width();
  
		this.masonry.columnWidth = this.options.masonry && this.options.masonry.columnWidth ||
			// or use the size of the first item
			this.$filteredAtoms.outerWidth(true) ||
			// if there's no items, use size of container
			containerWidth;

		this.masonry.columnWidth += gutter;

		this.masonry.cols = Math.floor( ( containerWidth + gutter ) / this.masonry.columnWidth );
		this.masonry.cols = Math.max( this.masonry.cols, 1 );
	};

	jQuery.Isotope.prototype._masonryReset = function() {
		// layout-specific props
		this.masonry = {};
		// FIXME shouldn't have to call this again
		this._getMasonryGutterColumns();
		var i = this.masonry.cols;
		this.masonry.colYs = [];
		while (i--) {
			this.masonry.colYs.push( 0 );
		}
	};	
  
	jQuery.Isotope.prototype._masonryResizeChanged = function() {
		var prevSegments = this.masonry.cols;
		// update cols/rows
		this._getMasonryGutterColumns();
		// return if updated cols/rows is not equal to previous
		return ( this.masonry.cols !== prevSegments );
	};  

	
	blogisotope = function () {
		var gutterwidth,
			conwidth = jQuery('.blog-masonry').width(),
			columnwidth = Math.floor(conwidth);
		
		if (jQuery('.blog-masonry').hasClass('on-two-columns') === true) {
			
			columnwidth = Math.floor(conwidth*0.48);
			gutterwidth = Math.floor(conwidth*0.04);
			
			if (jQuery(window).width() < 768) {
			columnwidth = Math.floor(conwidth*1);
			}
			else {
				columnwidth = Math.floor(conwidth*0.48);
			}		
			
		} else
		
		if (jQuery('.blog-masonry').hasClass('on-three-columns') === true) {
			columnwidth = Math.floor(conwidth*0.3033);
			gutterwidth = Math.floor(conwidth*0.04);

					if (jQuery(window).width() < 1023) {
						if (jQuery(window).width() < 768) {
						columnwidth = Math.floor(conwidth*1);
						}
						else {
							columnwidth = Math.floor(conwidth*0.48);
						}
					}
					else {
						columnwidth = Math.floor(conwidth*0.3033);
					}
		}

		else
		
		if (jQuery('.blog-masonry').hasClass('on-one-column') === true) {
			columnwidth = Math.floor(conwidth);
			gutterwidth = 0;
		}				

		
		jQuery('.blog-masonry').find('.post-masonry').each(function() {
			jQuery(this).css({'width' : columnwidth});
			// jQuery('.owl-item').css({'width': columnwidth});
		});		

			
		// Calculate Audio bar width	
		var audiowidth = jQuery('.audio-item').width();
		jQuery('.jp-progress').css({'width': audiowidth-250});		

		return gutterwidth;		

	}	
	
	var $blog_container = jQuery('.blog-masonry');	
		// Blog Isotope Call
		bloggingisotope = function() {

			// $blog_container.imagesLoaded( function(){
			  $blog_container.isotope({
					itemSelector: '.post-masonry',
					animationEngine: 'jquery',
					masonry: {
						gutterWidth: blogisotope()
					}	
			  });
			// });  

		};
		bloggingisotope();
		jQuery(window).smartresize(bloggingisotope);
		jQuery(window).load(bloggingisotope);

});