using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using org.secc.PDF;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace org.secc.Plugins.Tests.Plugins.org.secc.PDF.Workflows
{
    [TestClass]
    public class LavaPDFTest
    {
        /// <summary>
        /// A small test subclass that overrides the two protected methods so we can observe calls.
        /// </summary>
        private class TestLavaPDF : LavaPDF
        {
            public bool GeneratePdfCalled { get; private set; } = false;
            public string GeneratedHtml { get; private set; }
            public string GeneratedHeader { get; private set; }
            public string GeneratedFooter { get; private set; }
            public string GeneratedDocumentName { get; private set; }

            public bool SaveAndAssignPdfCalled { get; private set; } = false;
            public Guid? SaveDestinationAttributeGuid { get; private set; }
            public BinaryFile SavePdfBinary { get; private set; }
            public WorkflowAction SaveActionParameter { get; private set; }
            public RockContext SaveRockContextParameter { get; private set; }

            protected override BinaryFile GeneratePdf( string html, string header, string footer, RockContext rockContext, string documentName )
            {
                GeneratePdfCalled = true;
                GeneratedHtml = html;
                GeneratedHeader = header;
                GeneratedFooter = footer;
                GeneratedDocumentName = documentName;

                return new BinaryFile
                {
                    Guid = Guid.NewGuid(),
                    FileName = documentName ?? "test.pdf",
                    MimeType = "application/pdf",
                    IsTemporary = true,
                    IsSystem = false,
                    FileSize = 1
                };
            }

            protected override void SaveAndAssignPdf( BinaryFile pdfBinary, Guid destinationAttributeGuid, WorkflowAction action, RockContext rockContext )
            {
                SaveAndAssignPdfCalled = true;
                SaveDestinationAttributeGuid = destinationAttributeGuid;
                SavePdfBinary = pdfBinary;
                SaveActionParameter = action;
                SaveRockContextParameter = rockContext;
            }
        }

        /// <summary>
        /// A test subclass whose GeneratePdf always throws to exercise Execute's error handling.
        /// </summary>
        private class ThrowingLavaPDF : LavaPDF
        {
            protected override BinaryFile GeneratePdf( string html, string header, string footer, RockContext rockContext, string documentName )
            {
                throw new InvalidOperationException( "PDF generation failed." );
            }

            protected override void SaveAndAssignPdf( BinaryFile pdfBinary, Guid destinationAttributeGuid, WorkflowAction action, RockContext rockContext )
            {
                // Should never be reached in the throwing scenario.
            }
        }

        /// <summary>
        /// WorkflowAction subclass that allows overriding the ActionTypeCache getter so we can provide attribute values for the action.
        /// </summary>
        private class TestWorkflowAction : WorkflowAction
        {
            private WorkflowActionTypeCache _cache;

            public TestWorkflowAction( WorkflowActionTypeCache cache )
            {
                _cache = cache;
            }

            public override WorkflowActionTypeCache ActionTypeCache => _cache;
        }

        private static WorkflowActionTypeCache CreateActionTypeCacheWithValues( Dictionary<string, string> values )
        {
            var cache = new WorkflowActionTypeCache();
            cache.AttributeValues = new Dictionary<string, AttributeValueCache>();

            int attrId = 1;
            foreach ( var kvp in values )
            {
                cache.AttributeValues[kvp.Key] = new AttributeValueCache( attrId++, null, kvp.Value );
            }

            return cache;
        }

        [TestMethod]
        public void Execute_GeneratesPdf_But_DoesNot_Save_When_PDF_AttributeEmpty()
        {
            // Arrange
            var values = new Dictionary<string, string>
            {
                { "Lava", "<p>Hello PDF</p>" },
                { "Header", "" },
                { "Footer", "" },
                { "DocumentName", "MyDoc.pdf" },
                { "PDF", "" }
            };

            var actionTypeCache = CreateActionTypeCacheWithValues( values );
            var action = new TestWorkflowAction( actionTypeCache )
            {
                Activity = new WorkflowActivity { Workflow = new Workflow() }
            };

            var sut = new TestLavaPDF();

            // Act
            List<string> errors;
            var result = sut.Execute( null, action, null, out errors );

            // Assert
            Assert.IsTrue( result );
            Assert.AreEqual( 0, errors.Count );
            Assert.IsTrue( sut.GeneratePdfCalled, "GeneratePdf should have been called." );
            Assert.AreEqual( "<p>Hello PDF</p>", sut.GeneratedHtml );
            Assert.AreEqual( "", sut.GeneratedHeader );
            Assert.AreEqual( "", sut.GeneratedFooter );
            Assert.AreEqual( "MyDoc.pdf", sut.GeneratedDocumentName );
            Assert.IsFalse( sut.SaveAndAssignPdfCalled, "SaveAndAssignPdf should not be called when PDF attribute value is empty." );
        }

        [TestMethod]
        public void Execute_GeneratesPdf_And_Calls_SaveWhen_PDF_AttributeGuid_Present()
        {
            // Arrange
            var destinationGuid = Guid.NewGuid();
            var values = new Dictionary<string, string>
            {
                { "Lava", "<p>Document</p>" },
                { "Header", "<h1>h</h1>" },
                { "Footer", "<footer/>" },
                { "DocumentName", "Doc2.pdf" },
                { "PDF", destinationGuid.ToString() }
            };

            var actionTypeCache = CreateActionTypeCacheWithValues( values );
            var action = new TestWorkflowAction( actionTypeCache )
            {
                Activity = new WorkflowActivity { Workflow = new Workflow() }
            };

            var sut = new TestLavaPDF();

            // Act
            List<string> errors;
            var result = sut.Execute( null, action, null, out errors );

            // Assert
            Assert.IsTrue( result );
            Assert.AreEqual( 0, errors.Count );
            Assert.IsTrue( sut.GeneratePdfCalled, "GeneratePdf should have been called." );
            Assert.AreEqual( "<p>Document</p>", sut.GeneratedHtml );
            Assert.AreEqual( "<h1>h</h1>", sut.GeneratedHeader );
            Assert.AreEqual( "<footer/>", sut.GeneratedFooter );
            Assert.IsTrue( sut.SaveAndAssignPdfCalled, "SaveAndAssignPdf should be called when PDF attribute contains a guid." );
            Assert.AreEqual( destinationGuid, sut.SaveDestinationAttributeGuid.Value );
            Assert.IsNotNull( sut.SavePdfBinary );
            Assert.AreEqual( "Doc2.pdf", sut.SavePdfBinary.FileName );
            Assert.AreEqual( action, sut.SaveActionParameter );
        }

        [TestMethod]
        public void Execute_ReturnsFalse_And_PopulatesErrors_When_GeneratePdf_Throws()
        {
            // Arrange
            var values = new Dictionary<string, string>
            {
                { "Lava", "<p>Hello</p>" },
                { "Header", "" },
                { "Footer", "" },
                { "DocumentName", "Doc.pdf" },
                { "PDF", "" }
            };

            var actionTypeCache = CreateActionTypeCacheWithValues( values );
            var action = new TestWorkflowAction( actionTypeCache )
            {
                Activity = new WorkflowActivity { Workflow = new Workflow() }
            };

            var sut = new ThrowingLavaPDF();

            // Act
            List<string> errors;
            var result = sut.Execute( null, action, null, out errors );

            // Assert
            Assert.IsFalse( result, "Execute should return false when PDF generation throws." );
            Assert.AreEqual( 1, errors.Count );
            Assert.AreEqual( "PDF generation failed.", errors[0] );
        }
    }
}
