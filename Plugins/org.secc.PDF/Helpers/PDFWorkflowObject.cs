
using System.Collections.Generic;
using Rock;
using Rock.Model;
using DotLiquid;

namespace org.secc.PDF
{
    public class PDFWorkflowObject
    {
        public BinaryFile PDFInput { get; set; }
        public string LavaInput { get; set; }
        public Dictionary<string, object> MergeObjects { get; set; }
        public string RenderedXHTML {
            get
            {
                return LavaInput.ResolveMergeFields( MergeObjects );
            }
        }
        public BinaryFile RenderedPDF { get; set; }
    }
}
