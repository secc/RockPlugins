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
using Rock.Plugin;

namespace org.secc.RegistrationCart.Migrations
{
    /// <summary>
    /// Adds the Registration Cart Maintenance job (Jobs/CartMaintenance), every 15 minutes. Its
    /// settings are on the job in Rock (Admin Tools › System Settings › Jobs Administration).
    /// </summary>
    [MigrationNumber( 7, "1.16.0" )]
    public class AddCartMaintenanceJob : Migration
    {
        public override void Up()
        {
            Sql( $@"
                IF NOT EXISTS (SELECT 1 FROM [ServiceJob] WHERE [Guid] = '{SystemGuid.ServiceJob.CART_MAINTENANCE}')
                    INSERT INTO [ServiceJob] ([IsSystem], [IsActive], [Name], [Description], [Class], [CronExpression], [NotificationStatus], [Guid])
                    VALUES (0, 1, 'Registration Cart Maintenance',
                        'Abandons idle Registration Cart carts, finishes signed documents whose PDF failed, and reports checkouts whose payment outcome is unknown.',
                        'org.secc.RegistrationCart.Jobs.CartMaintenance', '0 0/15 * 1/1 * ? *', 3, '{SystemGuid.ServiceJob.CART_MAINTENANCE}');" );
        }

        public override void Down()
        {
            Sql( $"DELETE FROM [ServiceJob] WHERE [Guid] = '{SystemGuid.ServiceJob.CART_MAINTENANCE}';" );
        }
    }
}
