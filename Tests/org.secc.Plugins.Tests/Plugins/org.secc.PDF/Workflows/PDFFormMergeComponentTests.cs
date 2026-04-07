using System.Collections.Generic;
using System.IO;
using iText.Forms;
using iText.Forms.Fields;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using org.secc.PDF;

namespace org.secc.Plugins.Tests.Plugins.org.secc.PDF.Workflows
{
    /// <summary>
    /// Component tests that exercise the real iText 9 merge pipeline end-to-end.
    /// These tests use no Rock database — they create in-memory PDFs programmatically,
    /// call <see cref="PDFFormMerge.ApplyMergeFields"/> directly, then read the output
    /// back with iText to assert on the result.
    ///
    /// Primary purpose: regression guard against future iText package upgrades.
    /// </summary>
    [TestClass]
    public class PDFFormMergeComponentTests
    {
        /// <summary>
        /// Thin subclass that promotes <see cref="ApplyMergeFields"/> to public
        /// so tests can invoke it without going through the full Execute pipeline.
        /// </summary>
        private class TestablePDFFormMerge : PDFFormMerge
        {
            public byte[] InvokeApplyMergeFields( byte[] pdfBytes, PDFWorkflowObject workflowObject, bool flatten )
            {
                return ApplyMergeFields( pdfBytes, workflowObject, flatten );
            }
        }

        // -----------------------------------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Creates a minimal in-memory PDF whose AcroForm contains one text field per entry
        /// in <paramref name="fieldDefaults"/>, pre-populated with the supplied default value.
        /// </summary>
        private static byte[] CreatePdfWithFormFields( Dictionary<string, string> fieldDefaults )
        {
            using ( var ms = new MemoryStream() )
            {
                var pdfDoc = new PdfDocument( new PdfWriter( ms ) );
                var page = pdfDoc.AddNewPage();
                var form = PdfAcroForm.GetAcroForm( pdfDoc, true );

                float y = 700f;
                foreach ( var kvp in fieldDefaults )
                {
                    var field = new TextFormFieldBuilder( pdfDoc, kvp.Key )
                        .SetWidgetRectangle( new Rectangle( 36, y, 200, 20 ) )
                        .CreateText();

                    field.SetValue( kvp.Value );
                    form.AddField( field, page );
                    y -= 30f;
                }

                pdfDoc.Close();
                return ms.ToArray();
            }
        }

        private static string ReadFieldValue( byte[] pdfBytes, string fieldKey )
        {
            using ( var reader = new PdfReader( new MemoryStream( pdfBytes ) ) )
            using ( var pdfDoc = new PdfDocument( reader ) )
            {
                var form = PdfAcroForm.GetAcroForm( pdfDoc, false );
                return form?.GetField( fieldKey )?.GetValueAsString();
            }
        }

        // -----------------------------------------------------------------------------------------
        // Tests
        // -----------------------------------------------------------------------------------------

        [TestMethod]
        public void ApplyMergeFields_SetsStringValue_For_MatchingFieldKey()
        {
            // Arrange
            var pdfBytes = CreatePdfWithFormFields( new Dictionary<string, string> { { "FirstName", "" } } );
            var workflowObject = new PDFWorkflowObject
            {
                MergeObjects = new Dictionary<string, object> { { "FirstName", "Jane" } }
            };

            // Act
            var result = new TestablePDFFormMerge().InvokeApplyMergeFields( pdfBytes, workflowObject, flatten: false );

            // Assert
            Assert.AreEqual( "Jane", ReadFieldValue( result, "FirstName" ) );
        }

        [TestMethod]
        public void ApplyMergeFields_DoesNotSetValue_When_MergeObject_Is_Not_A_String()
        {
            // Arrange — the merge object value is an int, not a string, so the field should be skipped
            var pdfBytes = CreatePdfWithFormFields( new Dictionary<string, string> { { "Amount", "0" } } );
            var workflowObject = new PDFWorkflowObject
            {
                MergeObjects = new Dictionary<string, object> { { "Amount", 999 } }
            };

            // Act
            var result = new TestablePDFFormMerge().InvokeApplyMergeFields( pdfBytes, workflowObject, flatten: false );

            // Assert — original default "0" should be unchanged
            Assert.AreEqual( "0", ReadFieldValue( result, "Amount" ) );
        }

        [TestMethod]
        public void ApplyMergeFields_LeavesField_Unchanged_When_No_Matching_MergeObject_And_No_Lava()
        {
            // Arrange
            var pdfBytes = CreatePdfWithFormFields( new Dictionary<string, string> { { "Notes", "Original" } } );
            var workflowObject = new PDFWorkflowObject
            {
                MergeObjects = new Dictionary<string, object>()
            };

            // Act
            var result = new TestablePDFFormMerge().InvokeApplyMergeFields( pdfBytes, workflowObject, flatten: false );

            // Assert
            Assert.AreEqual( "Original", ReadFieldValue( result, "Notes" ) );
        }

        [TestMethod]
        public void ApplyMergeFields_Flatten_True_Removes_All_Interactive_Fields()
        {
            // Arrange
            var pdfBytes = CreatePdfWithFormFields( new Dictionary<string, string>
            {
                { "Field1", "A" },
                { "Field2", "B" }
            } );
            var workflowObject = new PDFWorkflowObject
            {
                MergeObjects = new Dictionary<string, object>()
            };

            // Act
            var result = new TestablePDFFormMerge().InvokeApplyMergeFields( pdfBytes, workflowObject, flatten: true );

            // Assert — after flattening there should be no interactive fields left
            using ( var reader = new PdfReader( new MemoryStream( result ) ) )
            using ( var pdfDoc = new PdfDocument( reader ) )
            {
                var form = PdfAcroForm.GetAcroForm( pdfDoc, false );
                bool noInteractiveFields = form == null || form.GetAllFormFields().Count == 0;
                Assert.IsTrue( noInteractiveFields, "All form fields should be removed after flattening." );
            }
        }

        [TestMethod]
        public void ApplyMergeFields_Flatten_False_Preserves_Interactive_Fields()
        {
            // Arrange
            var pdfBytes = CreatePdfWithFormFields( new Dictionary<string, string> { { "Field1", "A" } } );
            var workflowObject = new PDFWorkflowObject
            {
                MergeObjects = new Dictionary<string, object>()
            };

            // Act
            var result = new TestablePDFFormMerge().InvokeApplyMergeFields( pdfBytes, workflowObject, flatten: false );

            // Assert — field should still be interactive
            using ( var reader = new PdfReader( new MemoryStream( result ) ) )
            using ( var pdfDoc = new PdfDocument( reader ) )
            {
                var form = PdfAcroForm.GetAcroForm( pdfDoc, false );
                Assert.IsNotNull( form );
                Assert.AreEqual( 1, form.GetAllFormFields().Count );
            }
        }

        [TestMethod]
        public void ApplyMergeFields_HandlesMultipleFields_Independently()
        {
            // Arrange
            var pdfBytes = CreatePdfWithFormFields( new Dictionary<string, string>
            {
                { "First", "" },
                { "Last", "" },
                { "Untouched", "Keep" }
            } );
            var workflowObject = new PDFWorkflowObject
            {
                MergeObjects = new Dictionary<string, object>
                {
                    { "First", "John" },
                    { "Last", "Smith" }
                }
            };

            // Act
            var result = new TestablePDFFormMerge().InvokeApplyMergeFields( pdfBytes, workflowObject, flatten: false );

            // Assert
            Assert.AreEqual( "John", ReadFieldValue( result, "First" ) );
            Assert.AreEqual( "Smith", ReadFieldValue( result, "Last" ) );
            Assert.AreEqual( "Keep", ReadFieldValue( result, "Untouched" ) );
        }
    }
}