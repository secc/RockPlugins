$(function () { //<--- here..the codes below is called whn document is ready

//@prepros-prepend functions.js
//@prepros-prepend validation.js

// document.domain = "southeastchristian.org";

$("#SiteNav .mobile.main .btn-menu").click(function() {
	$("body").toggleClass("menu-open");
});

$(".expandable-menu .dropdown > .toggle[id!='mobileToggleAccount']").click(function(e) {
	e.preventDefault();
	var $Dropdown = $(this).parent();
	$Dropdown.toggleClass('open');
});

// $(document).on('click', '.locations-toggle', function(){
$(".locations-toggle").click(function() {
	var $LocationsMenu = $("#LocationsMenu");

	$("#AccountMenu > nav").removeClass("active");
	$(".account-menu-toggle").removeClass("active");
	$(this).toggleClass("active");
	$LocationsMenu.toggleClass("active");
});

// $("#AccountMenu > nav").mouseleave(function() {
// 	var $AccountMenuButton = $(".account-menu-toggle");
//
// 	$(this).removeClass("active");
// 	$AccountMenuButton.removeClass("active");
// });

$("#SiteNav .desktop button.btn-menu").click(function() {
	var SubMenuName = $(this).data("subMenu");
	var $MegaMenus = $("#MegaMenus");
	var $SubMenu = $("#MegaMenus .sub-menu[data-menu='" + SubMenuName + "']");

	//Is the Mega Menu currently being displayed?
	if (!$MegaMenus.hasClass("active")) {
		//Nope ...

		//So, First we need to mark the appropriate child menu item as active.
		//This will make sure that the right child menu is already displayed when
		//the mega menu opens.
		$MegaMenus.find(".item.active").removeClass("active");
		$SubMenu.addClass("active");

		//Now, make the Mega Menu active, which will slide it down into view.
		$MegaMenus.addClass("active");
	} else {
		//The Mega Menu IS already being displayed.

		//So, did the user click on a different menu item
		//than what is being displayed?
		var $ActiveItem = $MegaMenus.find(".item.active");
		if ($ActiveItem.data("menu") !== SubMenuName) {
			//Yep...

			//Then we need to switch the child menu being displayed.
			var SubMenuIndex;
			//Get the index of the menu that we want to display. This
			//will be passed to the bootstrap carousel function to show the
			//menu.
			$MegaMenus.find(".item").each(function(i) {
				if ($(this).data("menu") === SubMenuName) {
					SubMenuIndex = i;
					return false;
				}
			});

			//Now pass the index to bootstrap, and it will handle the rest,
			//sliding the current menu out of view, and the new menu into view.
			$MegaMenus.carousel(SubMenuIndex);
		} else {
			//User clicked on the same menu item,
			//so just close the mega menu entirely.
			$MegaMenus.removeClass("active");
		}
	}
});

$("#PageBody").click(function() {
	$("#MegaMenus").removeClass("active");
});



$('.se-logo').click(function() {
	var campus = $(this).data('class');
	var container = $(this).data('target');

	if (container !== undefined) {
		$(container + " .campus.active").removeClass('active');
		$(container + " .se-logo").prop('class', 'se-logo');

		$(this).addClass(campus);
		$(container + " ." + campus + "-info").addClass('active');
	}
});

$('.top-link').click(function(e) {
	e.preventDefault();
	$('html, body').animate({scrollTop: 0}, 350);
});

$("#SiteSearch input:text").on('keyup', function (e) {
	e.preventDefault();
	var value = $( this ).val();
	if (e.keyCode == 13) {
		// Do something
		submitSearchSite(value);
	}
});
$("a#search-submit").click(function(e) {
	e.preventDefault();
	var value = $("#SiteSearch input:text").val();
	submitSearchSite(value);
});

function submitSearchSite(val) {
	// alert('ya :' + val);
	window.location = "https://www.southeastchristian.org/search.php?q="+val;
}
//@prepros-append event-rotator.js

function modalMutationObserver()
{
	// Select the node that will be observed for mutations
	var targetNode = document.getElementById('PageBody');

	// Options for the observer (which mutations to observe)
	var config = { attributes: true, childList: true, subtree: true };

	// Callback function to execute when mutations are observed
	var callback = function(mutationsList, observer) {
		for(var mutation of mutationsList) {
			if (mutation.type == 'childList') {
				for(var j=0; j<mutation.addedNodes.length; ++j) {
					if (mutation.target != undefined) {
						// was a child added with class of modal-open
						if(mutation.target.classList.contains('modal-open')) {
							console.log('Removing the PageBody zIndex.');
							document.getElementById('PageBody').style.zIndex = 'auto';
						}
					}
				}
				for(var j=0; j<mutation.removedNodes.length; ++j) {
					if (mutation.target != undefined) {
						// was a child removed with class of modal-scrollable
						if(mutation.target.classList.contains('modal-scrollable')) {
							console.log('Resetting the PageBody zIndex.');
							document.getElementById('PageBody').style.zIndex = 2;
						}
					}
				}
			}
		}
	};

	// Create an observer instance linked to the callback function
	var observer = new MutationObserver(callback);

	// Start observing the target node for configured mutations
	observer.observe(targetNode, config);
}

/**********************************************************/
/******** Start: Expandable DIVs for the footer ***********/
/**********************************************************/

	function collapseSection(element) {
	  // get the height of the element's inner content, regardless of its actual size
	  var sectionHeight = element.scrollHeight;
	  // temporarily disable all css transitions
	  var elementTransition = element.style.transition;
	  element.style.transition = '';
	  // on the next frame (as soon as the previous style change has taken effect),
	  // explicitly set the element's height to its current pixel height, so we
	  // aren't transitioning out of 'auto'
	  requestAnimationFrame(function() {
	    element.style.height = sectionHeight + 'px';
	    element.style.transition = elementTransition;
	    // on the next frame (as soon as the previous style change has taken effect),
	    // have the element transition to height: 0
	    requestAnimationFrame(function() {
	      element.style.height = 0 + 'px';
	    });
	  });
	  // mark the section as "currently collapsed"
	  element.setAttribute('data-collapsed', 'true');
	}

	function expandSection(element) {
	  // get the height of the element's inner content, regardless of its actual size
	  var sectionHeight = element.scrollHeight;
	  // have the element transition to the height of its inner content
	  element.style.height = sectionHeight + 'px';
	  // when the next css transition finishes (which should be the one we just triggered)
	  element.addEventListener('transitionend', function(e) {
	    // remove this event listener so it only gets triggered once
	    element.removeEventListener('transitionend', arguments.callee);
	    // remove "height" from the element's inline styles, so it can return to its initial value
	    element.style.height = null;
	  });
	  // mark the section as "currently not collapsed"
	  element.setAttribute('data-collapsed', 'false');
	}

	$('.se-expandable-header').on( "click", function(e) {
		e.stopPropagation();
		e.preventDefault();

		if (this) {
			var section = this.parentNode.querySelector('.se-expandable-content')
		}
		if (section) {
			var isCollapsed = section.getAttribute('data-collapsed') === 'true';
		}

		if(isCollapsed) {
			expandSection(section)
			this.setAttribute('data-expanded', 'true')
			section.setAttribute('data-collapsed', 'false')
		} else {
			collapseSection(section)
			this.setAttribute('data-expanded', 'false')
		}
	});

/**********************************************************/
/******** End: Expandable DIVs for the footer ***********/
/**********************************************************/

	$(document).ready(function(o){var t=300,a=1200,s=700,l=o(".js__back-to-top");o(window).scroll(function(){o(this).scrollTop()>t?l.addClass("-is-visible"):l.removeClass("-is-visible -zoom-out"),o(this).scrollTop()>a&&l.addClass("-zoom-out")}),l.on("click",function(t){t.preventDefault(),o("body,html").animate({scrollTop:0},s)})});

	$(document).ready(modalMutationObserver);

});
