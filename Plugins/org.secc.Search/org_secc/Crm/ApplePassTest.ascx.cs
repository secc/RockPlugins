using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock;
using Rock.Attribute;
using Rock.Web.UI;
using Rock.Data;
using Rock.Model;


namespace RockWeb.Plugins.org_secc.Crm
{
    [DisplayName("Apple Pass Test")]
    [Category("SECC > CRM")]
    public partial class ApplePassTest : RockBlock
    {
        protected override void OnInit( EventArgs e )
        {
            base.OnLoad( e );
            lbTestPass.Click += LbTestPass_Click;
        }

        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );
        }

        private void LbTestPass_Click( object sender, EventArgs e )
        {
            if(!ppApplePassPerson.PersonId.HasValue)
            {
                return;
            }
            var rockContext = new RockContext();
            var personService = new PersonService( rockContext );
            var person = personService.Get( ppApplePassPerson.PersonId.Value );


            var script = $"GeneratePass(\"{person.PrimaryAlias.Guid}\");";
            ScriptManager.RegisterStartupScript( upMain, upMain.GetType(), "TestApplePass" + DateTime.Now.Ticks, script, true );
        }

    }
}