// <copyright>
// Copyright Southeast Christian Church
//
// Licensed under the  Southeast Christian Church License (the "License");
// you may not use this file except in compliance with the License.
// A copy of the License should be included with this file.
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System.Collections.Generic;
using System.Linq;
using org.secc.Workflow.Utility;
using Rock.Security;
using Xunit;

namespace org.secc.Workflow.Tests
{
    /// <summary>
    /// Runs WorkflowBulkUpdateAccess.GetEditableWorkflows over in-memory workflows whose Rock
    /// authorization answer is supplied by the test.
    /// </summary>
    public class WorkflowBulkUpdateAccessTests
    {
        private static readonly Rock.Model.Person CurrentPerson = new Rock.Model.Person { Id = 42, FirstName = "Test", LastName = "Person" };

        [Fact]
        public void KeepsOnlyWorkflowsThePersonCanEdit_InOrder()
        {
            var editable1 = new TestWorkflow( 1, Authorization.VIEW, Authorization.EDIT );
            var viewOnly = new TestWorkflow( 2, Authorization.VIEW );
            var editable2 = new TestWorkflow( 3, Authorization.EDIT );
            var none = new TestWorkflow( 4 );

            int skippedCount;
            var result = WorkflowBulkUpdateAccess.GetEditableWorkflows( new[] { editable1, viewOnly, editable2, none }, CurrentPerson, out skippedCount );

            Assert.Equal( new[] { 1, 3 }, result.Select( w => w.Id ).ToArray() );
            Assert.Equal( 2, skippedCount );
        }

        [Fact]
        public void ViewOrAdministrateWithoutEdit_IsSkipped()
        {
            var workflow = new TestWorkflow( 7, Authorization.VIEW, Authorization.ADMINISTRATE );

            int skippedCount;
            var result = WorkflowBulkUpdateAccess.GetEditableWorkflows( new[] { workflow }, CurrentPerson, out skippedCount );

            Assert.Empty( result );
            Assert.Equal( 1, skippedCount );
        }

        [Fact]
        public void ChecksEditForTheCurrentPerson()
        {
            var workflow = new TestWorkflow( 8, Authorization.EDIT );

            int skippedCount;
            WorkflowBulkUpdateAccess.GetEditableWorkflows( new[] { workflow }, CurrentPerson, out skippedCount );

            var check = Assert.Single( workflow.Checks );
            Assert.Equal( Authorization.EDIT, check.Action );
            Assert.Same( CurrentPerson, check.Person );
        }

        [Fact]
        public void EveryWorkflowEditable_NoneSkipped()
        {
            var workflows = new[] { new TestWorkflow( 1, Authorization.EDIT ), new TestWorkflow( 2, Authorization.EDIT ) };

            int skippedCount;
            var result = WorkflowBulkUpdateAccess.GetEditableWorkflows( workflows, CurrentPerson, out skippedCount );

            Assert.Equal( 2, result.Count );
            Assert.Equal( 0, skippedCount );
        }

        [Fact]
        public void NoWorkflowEditable_ReturnsEmptyListAndCountsAll()
        {
            var workflows = new[] { new TestWorkflow( 1 ), new TestWorkflow( 2 ), new TestWorkflow( 3 ) };

            int skippedCount;
            var result = WorkflowBulkUpdateAccess.GetEditableWorkflows( workflows, CurrentPerson, out skippedCount );

            Assert.NotNull( result );
            Assert.Empty( result );
            Assert.Equal( 3, skippedCount );
        }

        [Fact]
        public void NullOrEmptySet_ReturnsEmptyListAndNoneSkipped()
        {
            int skippedCount;
            var fromNull = WorkflowBulkUpdateAccess.GetEditableWorkflows( null, CurrentPerson, out skippedCount );
            Assert.NotNull( fromNull );
            Assert.Empty( fromNull );
            Assert.Equal( 0, skippedCount );

            var fromEmpty = WorkflowBulkUpdateAccess.GetEditableWorkflows( new List<Rock.Model.Workflow>(), CurrentPerson, out skippedCount );
            Assert.NotNull( fromEmpty );
            Assert.Empty( fromEmpty );
            Assert.Equal( 0, skippedCount );
        }

        /// <summary>
        /// A workflow that answers IsAuthorized from a fixed set of allowed actions and records each check.
        /// </summary>
        private class TestWorkflow : Rock.Model.Workflow
        {
            private readonly HashSet<string> _allowedActions;

            public TestWorkflow( int id, params string[] allowedActions )
            {
                Id = id;
                _allowedActions = new HashSet<string>( allowedActions );
            }

            public List<(string Action, Rock.Model.Person Person)> Checks { get; } = new List<(string Action, Rock.Model.Person Person)>();

            public override bool IsAuthorized( string action, Rock.Model.Person person )
            {
                Checks.Add( (action, person) );
                return _allowedActions.Contains( action );
            }
        }
    }
}
