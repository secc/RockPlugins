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

});
