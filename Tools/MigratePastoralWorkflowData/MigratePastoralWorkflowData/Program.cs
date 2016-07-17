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
            Console.ReadLine();
        }

    }
}
