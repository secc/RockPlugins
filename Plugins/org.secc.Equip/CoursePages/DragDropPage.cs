using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI;
using org.secc.Equip.Model;
using org.secc.Widgities.Controls;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;

namespace org.secc.Equip.CoursePages
{
    [Export( typeof( CoursePageComponent ) )]
    [ExportMetadata( "ComponentName", "Drag & Drop Page" )]
    [Description( "Build a page by dragging and dropping elements. Quickly build a lesson from scratch." )]
    public class DragDropPage : CoursePageComponent
    {
        public override string Name => "Drag & Drop Page";

        public override string Icon => "fa fa-bars";

        public override int Order => 0;

        public override List<Control> DisplayConfiguration( Control parentControl )
        {
            var controls = new List<Control>();
            WidgityControl widgityControl = new WidgityControl
            {
                ID = this.Id + "_widgityControl"
            };

            parentControl.Controls.Add( widgityControl );
            controls.Add( widgityControl );
            return controls;
        }


        public override void ConfigureControls( CoursePage coursePage, List<Control> controls )
        {
            if ( controls.Count > 0 )
            {
                var widgityControl = ( WidgityControl ) controls[0];
                widgityControl.EntityTypeId = EntityTypeCache.GetId( typeof( CoursePage ) ).Value;
                widgityControl.EntityGuid = coursePage.Guid;
                widgityControl.ShowPublishButtons = false;
                widgityControl.DataBind();
                widgityControl.ShowSettings();
            }
        }

        public override List<Control> DisplayCoursePage( CoursePage coursePage, Control parentControl )
        {
            {
                var controls = new List<Control>();
                WidgityControl widgityControl = new WidgityControl
                {
                    ID = this.Id + "_widgityControl"
                };
                parentControl.Controls.Add( widgityControl );
                controls.Add( widgityControl );

                widgityControl.EntityTypeId = EntityTypeCache.GetId( typeof( CoursePage ) ).Value;
                widgityControl.EntityGuid = coursePage.Guid;
                widgityControl.DataBind();

                return controls;
            }
        }


        public override void ConfigureCoursePage( CoursePage coursePage, List<Control> controls )
        {
            if ( controls.Count > 0 )
            {
                var widgityControl = ( WidgityControl ) controls[0];
                widgityControl.EntityTypeId = EntityTypeCache.GetId( typeof( CoursePage ) ).Value;
                widgityControl.EntityGuid = coursePage.Guid;
                widgityControl.Publish();
            }
        }

    }
}
