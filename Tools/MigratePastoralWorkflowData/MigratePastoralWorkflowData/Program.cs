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
