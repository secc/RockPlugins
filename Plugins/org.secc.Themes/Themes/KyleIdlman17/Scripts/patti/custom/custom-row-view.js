window.VcRowViewCustom = window.VcRowView.extend({
buildDesignHelpers: function() {
var image,color, rowId,  params = this.model.get('params'),
              $column_edit = this.$el.find('> .controls .column_toggle');

          this.$el.find('> .controls .vc_row_color').remove();
          this.$el.find('> .controls .vc_row_image').remove();

          rowId = this.model.getParam( 'dt_id' );
          this.$el.find( '> .controls .vc_row-hash-id' ).remove();
          if ( ! _.isEmpty( rowId ) ) {
            jQuery( '<span class="vc_row-hash-id"></span>' )
              .text( '#' + rowId )
              .insertAfter( $column_edit );
          }

          if(!_.isEmpty(params.bg_color)) {
             color = jQuery('<span class="vc_row_color" style="background-color: ' + params.bg_color + '" title="' + i18nLocale.row_background_color + '"></span>');
             color.insertAfter($column_edit);
          }
          if(!_.isEmpty(params.bg_image)) {
            image = jQuery('<span class="vc_row_image" style="background-image: ' + params.bg_image + '" title="' + i18nLocale.row_background_image + '"></span>');
            image.insertAfter($column_edit);
            jQuery.ajax({
              type:'POST',
              url:window.ajaxurl,
              data:{
                action:'wpb_single_image_src',
                content: params.bg_image,
                size: 'thumbnail'
              },
              dataType:'html'
            }).done(function (url) {
                image.css({backgroundImage: 'url(' + url + ')'});
              });
          } else {

          }

        }

});