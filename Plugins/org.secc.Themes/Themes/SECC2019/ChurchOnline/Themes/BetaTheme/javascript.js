app.on('event:live', function(){
  $('.co-chat-tab').trigger('click');
});


window.reflowWidgetTabs = function() {
  var container, index, items, newTabs, tabContainer, tabs, reflowTabs, willOverflow;

  container    = $(".more-widgets");
  tabContainer = $(".co-widget-tabs");
  reflowTabs   = [];

  container.find("ul li").detach().appendTo(tabContainer);
  container.hide().find("ul").remove();
  tabContainer.removeClass('has-more-widgets');
  tabContainer.find('li').each(function(){ if(this.offsetTop > 0) { willOverflow = true; } });

  if( willOverflow ){ tabContainer.addClass('has-more-widgets'); }

  tabContainer.find('li').each(function(){
    if(this.offsetTop > 0) {
      reflowTabs.push(this);
    }
  });
  if (reflowTabs.length > 0) {
    newTabs = $("<ul>").append(reflowTabs);
    container.show().append(newTabs);
  }
};

var lazyReflow = _.debounce(reflowWidgetTabs, 300);
$(window).resize(lazyReflow)
$(function(){ setTimeout(function(){ reflowWidgetTabs(); }, 0); });
app.on('private-chat:add private-chat:remove', function(){ reflowWidgetTabs(); });
