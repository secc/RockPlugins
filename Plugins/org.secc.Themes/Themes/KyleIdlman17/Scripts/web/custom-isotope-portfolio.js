jQuery(document).ready(function() {
	// Isotope for Portfolio

jQuery('.patti-grid[id^="gridwrapper_"]').each( function() { 

	var $div = jQuery(this);
	var token = $div.data('token');
	var settingObj = window['dt_grid_' + token];	
	
	var $container = '';
	var $optionSets = '';

	$initial_filter = '';

	if (typeof settingObj === 'undefined') {
		$container = jQuery(" .grid_portfolio");
		$optionSets = jQuery('#gridwrapper_portfolio #options .option-set');
	}
	else {
		if(settingObj.initial_word != '') {
			$initial_filter = '.'+settingObj.initial_word+'';
		}			
		$container = jQuery(".grid_"+settingObj.id+"");
		$optionSets = jQuery('#gridwrapper_'+settingObj.id+'  #options .option-set');
	}

	colWidth = function () {
		var w = $container.width(), 
			columnNum = 1,
			columnWidth = 0;

		// apply default settings
		if(vals.grid_manager != 1) {
			if (w > 1440) {
				columnNum  = 7;
			} else if (w > 1365) {
				columnNum  = 5;
			} else if (w > 1279) {
				columnNum  = 5;
			} else if (w > 1023) {
				columnNum  = 5;
			} else if (w > 767) {
				columnNum  = 3;
			} else if (w > 479) {
				columnNum  = 2;
			}	
		}	

		// apply custom settings
		else {
			if (w > 1440) {
				columnNum  = vals.grid_very_wide;
			} else if (w > 1365) {
				columnNum  = vals.grid_wide;
			} else if (w > 1279) {
				columnNum  = vals.grid_normal;
			} else if (w > 1023) {
				columnNum  = vals.grid_small;
			} else if (w > 767) {
				columnNum  = vals.grid_tablet;
			} else if (w > 479) {
				columnNum  = vals.grid_phone;
			}	
		}


		columnWidth = Math.floor(w/columnNum);

		$container.find('.grid-item').each(function() {
			var $item = jQuery(this);

			var gwidth = 4;
			if(vals.grid_manager == 1) {
				gwidth = vals.grid_gutter_width
			} 

			$item.css({'margin': gwidth/2});

			if ($item.hasClass('item-wide')) {
				if (w < 480) {
					jQuery('.item-wide').css({
						'width'		 : ((columnWidth)-gwidth) + 'px',
						'height' : Math.round(((columnWidth)-gwidth) * 0.7777777) + 'px'
					});
					jQuery('.item-wide img').css({
						'width'		 : ((columnWidth)-gwidth) + 'px',
						'height' : 'auto'
					});	
				}
				else {
					jQuery('.item-wide').css({
						'width'		 : ((columnWidth*2)-gwidth) + 'px',
						'height' : Math.round((2*(((columnWidth)-gwidth) * 0.7777777))+gwidth) + 'px'
					});
					jQuery('.item-wide img').css({
						'width'		 : ((columnWidth*2)-gwidth) + 'px',
						'height' : 'auto'
					});				
				}
			}	
			
			if ($item.hasClass('item-small')) {
				jQuery('.item-small').css({
					'width'		 : ((columnWidth)-gwidth) + 'px',
					'height' : Math.round(((columnWidth)-gwidth) * 0.7777777) + 'px'
				});
				jQuery('.item-small img').css({
					'width'		 : ((columnWidth)-gwidth) + 'px',
					'height' : 'auto'
				});						
			}
				
			if ($item.hasClass('item-long')) {
				if (w < 480) {
					jQuery('.item-long').css({
						'width'		 : ((columnWidth)-gwidth) + 'px',
						'height' : Math.round(((columnWidth)-gwidth) * 0.7777777/2) + 'px'
					});
					jQuery('.item-long img').css({
						'width'		 : ((columnWidth)-gwidth) + 'px',
						'height' : 'auto'
					});		
				}
				else {
					jQuery('.item-long').css({
						'width'		 : ((columnWidth*2)-gwidth) + 'px',
						'height' : Math.round(((columnWidth)-gwidth) * 0.7777777) + 'px'
					});
					jQuery('.item-long img').css({
						'width'		 : ((columnWidth*2)-gwidth) + 'px',
						'height' : 'auto'
					});					
				}
			}
			
			if ($item.hasClass('item-high')) {
				jQuery('.item-high').css({
					'width'		 : ((columnWidth)-gwidth) + 'px',
					'height' : Math.round((2*(((columnWidth)-gwidth) * 0.7777777))+gwidth) + 'px'
				});
				jQuery('.item-high img').css({
					'width'		 : ((columnWidth)-gwidth) + 'px',
					'height' : 'auto'
				});				
			}				

		});
		return columnWidth;
	}

	// Isotope Call
	gridIsotope = function () {
		$container.isotope({
			layoutMode : 'masonry',
			itemSelector: '.grid-item',
			animationEngine: 'jquery',
			filter: $initial_filter,	
			masonry: { columnWidth: colWidth(), gutterWidth: 0 }
		});
	};

	resizedIsotope = function () {
		$container.isotope({
			layoutMode : 'masonry',
			itemSelector: '.grid-item',
			animationEngine: 'jquery',
			masonry: { columnWidth: colWidth(), gutterWidth: 0 }
		});
	};	

	gridIsotope();
	jQuery(window).smartresize(resizedIsotope);	
	jQuery(window).load(gridIsotope);


	// Portfolio Filtering

	$optionLinks = $optionSets.find('a');

	if($initial_filter != '') {
		$optionLinks.each(function(){
			var $this = jQuery(this);
			if ( $this.hasClass('selected') ) {
				$this.removeClass('selected');
			}
			if($this.attr('data-option-value') == $initial_filter) {
				$this.addClass('selected');
			}
		});
	}

	$optionLinks.click(function(){
		var $this = jQuery(this);
		// don't proceed if already selected
		if ( $this.hasClass('selected') ) {
			return false;
		}
		var $optionSet = $this.parents('.option-set');
		$optionSet.find('.selected').removeClass('selected');
		$this.addClass('selected');
  
		// make option object dynamically, i.e. { filter: '.my-filter-class' }
		var options = {},
			key = $optionSet.attr('data-option-key'),
			value = $this.attr('data-option-value');
		// parse 'false' as false boolean
		value = value === 'false' ? false : value;
		options[ key ] = value;
		if ( key === 'layoutMode' && typeof changeLayoutMode === 'function' ) {
		  // changes in layout modes need extra logic
		  changeLayoutMode( $this, options )
		} else {
		  // otherwise, apply new options
		  $container.isotope( options );
		}
		
		return false;
	});				
	

	});
});