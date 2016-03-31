using System.Collections.Generic;
using Rock.Model;

namespace org.secc.PDFMerge
{
    public class PDFFormMergeEntity
    {
        public BinaryFile PDF { get; set; }

        public Dictionary<string,string> MergeFields { get; set; }

        public BinaryFile MergedPDF { get; set; }
    }
}
