// <copyright>
// Copyright Southeast Christian Church
//
// Licensed under the  Southeast Christian Church License (the "License");
// you may not use this file except in compliance with the License.
// A copy of the License shoud be included with this file.
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System;
using System.Collections.Generic;
using System.Linq;
using Rock;
using Rock.Data;
using Rock.Model;

namespace MigratePastoralWorkflowData
{
    class Program
    {

        static void Main( string[] args )
        {
            HospitalWorkflowImport hospitalWorkflowImport = new HospitalWorkflowImport();
            hospitalWorkflowImport.Clean();
            hospitalWorkflowImport.Run();

            NursingHomeWorkflowImport nursingHomeWorkflowImport = new NursingHomeWorkflowImport();
            nursingHomeWorkflowImport.Clean();
            nursingHomeWorkflowImport.Run();

            HomeboundWorkflowImport homeboundWorkflowImport = new HomeboundWorkflowImport();
            homeboundWorkflowImport.Clean();
            homeboundWorkflowImport.Run();
            Console.ReadLine();
        }

    }
}
