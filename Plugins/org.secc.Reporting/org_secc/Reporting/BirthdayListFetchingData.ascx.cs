// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System;
using System.ComponentModel;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using Rock.Data;
using Rock.Model;
using Rock.Web.UI;
using Rock.Attribute;
using System.Collections.Generic;
using Rock;
using Rock.Web.Cache;

namespace RockWeb.Plugins.org_secc.Reporting
{
    /// <summary>
    /// Template block for developers to use to start a new block.
    /// </summary>
    [DisplayName( "Birthday List Fetching Data" )]
    [Category( "SECC2 > Project" )]
    [Description( "A simple block to fetch some data from Rock" )]
    [DataViewField( "Data View", entityTypeName:"Rock.Model.Person" )]
    [MemoField( "Lava", required:false, allowHtml:true )]
    [IntegerField( "Limit", required:false )]
    
    public partial class BirthdayListFetchingData : RockBlock, ICustomGridColumns
    {
        #region Fields

        // used for private variables

        #endregion

        #region Properties

        // used for public / protected properties

        #endregion

        #region Base Control Methods

        //  overrides of the base RockBlock methods (i.e. OnInit, OnLoad)

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
            gPeople.GridRebind += gPeople_GridRebind;

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlContent );
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );
                      
            if ( !Page.IsPostBack )
            {
                BindGrid();
            }
        }

        #endregion

        #region Events

        // handlers called by the controls on your block

        /// <summary>
        /// Handles the BlockUpdated event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {

        }

        private void gPeople_GridRebind( object sender, EventArgs e )
        {
            BindGrid();
        }

        #endregion

        #region Methods

        private void BindGrid()
        {                   
            Person person = new Person();
            var l = GetAttributeValue( "Limit" ).AsInteger();
            var tt = GetAttributeValue( "DataView" );
            Guid newguid = new Guid( tt );
            RockContext rockContext = new RockContext();
            DataViewService dataService = new DataViewService( rockContext );
            var items = dataService
                .Get( newguid );
            List<string> list = new List<string>();
            var qry = items.GetQuery( null, null, out list );
            var personList = (IQueryable<Person>)qry;
            var plist = personList
                .Where( p => p.BirthDate != null )
                .OrderBy( p => p.DaysUntilBirthday );
            List<Person> plistd = new List<Person>();

            if ( l != 0 )
            {
                var plist2 = plist.Take( l );
                plistd = plist2.ToList();
            }
            else
            {
                plistd = plist.ToList();
            }
            var lava = GetAttributeValue( "Lava" );

            if ( string.IsNullOrWhiteSpace(lava) )
            {
                gPeople.DataSource = plistd;
                gPeople.DataBind();
            }
            else
            {
                Dictionary<string, object> mergeObjects = new Dictionary<string, object>
            {
                { "People", plistd }
            };
                lLava.Text = ProcessLava( lava, person, mergeObjects );
            }
        }

        public static string ProcessLava( string lava, Person person, Dictionary<string, object> mergeObjects = null )
        {
            if ( mergeObjects == null )
            {
                mergeObjects = GetMergeFields( person );
            }
            
            while ( ( lava.HasLavaCommandFields() || lava.HasMergeFields() ) )
            {
                lava = lava.ResolveMergeFields( mergeObjects, null);
            }
            return lava;
        }

        public static Dictionary<string, Object> GetMergeFields( Person person )
        {
            var mergeObjects = Rock.Lava.LavaHelper.GetCommonMergeFields( null, person );
            return mergeObjects;
        }

        #endregion
    }
}