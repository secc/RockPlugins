using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using org.secc.PDF;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace org.secc.Plugins.Tests.Plugins.org.secc.PDF.Workflows
{
    [TestClass]
    public class PDFFormMergeTest
    {
        /// <summary>
        /// Test subclass that overrides all three protected virtual methods so no real
        /// DB, iText, or Rock infrastructure is invoked during tests.
        /// </summary>
        private class TestPDFFormMerge : PDFFormMerge
        {
            private readonly PDFWorkflowObject _resolvedObject;

            public TestPDFFormMerge( PDFWorkflowObject resolvedObject = null )
            {
                _resolvedObject = resolvedObject ?? new PDFWorkflowObject
                {
                    MergeObjects = new Dictionary<string, object>()
                };
            }

            // --- ResolvePdfWorkflowObject observation ---
            public bool ResolvePdfWorkflowObjectCalled { get; private set; }

            // --- MergePdfTemplate observation ---
            public bool MergePdfTemplateCalled { get; private set; }
            public Guid ReceivedTemplateGuid { get; private set; }
            public PDFWorkflowObject ReceivedWorkflowObject { get; private set; }
            public bool ReceivedFlatten { get; private set; }

            // --- SaveAndAssignPdf observation ---
            public bool SaveAndAssignPdfCalled { get; private set; }
            public BinaryFile SavedRenderedPDF { get; private set; }
            public Guid SavedOutputAttributeGuid { get; private set; }
            public WorkflowAction SavedActionParameter { get; private set; }

            protected override PDFWorkflowObject ResolvePdfWorkflowObject( object entity, WorkflowAction action, RockContext rockContext, out List<string> errorMessages )
            {
                ResolvePdfWorkflowObjectCalled = true;
                errorMessages = new List<string>();
                return _resolvedObject;
            }

            protected override BinaryFile MergePdfTemplate( Guid templateGuid, PDFWorkflowObject pdfWorkflowObject, bool flatten, RockContext rockContext )
            {
                MergePdfTemplateCalled = true;
                ReceivedTemplateGuid = templateGuid;
                ReceivedWorkflowObject = pdfWorkflowObject;
                ReceivedFlatten = flatten;

                return new BinaryFile
                {
                    Guid = Guid.NewGuid(),
                    FileName = "merged.pdf",
                    MimeType = "application/pdf",
                    IsTemporary = false,
                    ContentStream = new MemoryStream( new byte[] { 1, 2, 3 } )
                };
            }

            protected override void SaveAndAssignPdf( BinaryFile renderedPDF, Guid outputAttributeGuid, WorkflowAction action, RockContext rockContext )
            {
                SaveAndAssignPdfCalled = true;
                SavedRenderedPDF = renderedPDF;
                SavedOutputAttributeGuid = outputAttributeGuid;
                SavedActionParameter = action;
            }
        }

        /// <summary>
        /// Test subclass whose <see cref="MergePdfTemplate"/> always throws, used to
        /// verify that <see cref="PDFFormMerge.Execute"/> handles exceptions correctly.
        /// </summary>
        private class ThrowingPDFFormMerge : PDFFormMerge
        {
            protected override PDFWorkflowObject ResolvePdfWorkflowObject( object entity, WorkflowAction action, RockContext rockContext, out List<string> errorMessages )
            {
                errorMessages = new List<string>();
                return new PDFWorkflowObject { MergeObjects = new Dictionary<string, object>() };
            }

            protected override BinaryFile MergePdfTemplate( Guid templateGuid, PDFWorkflowObject pdfWorkflowObject, bool flatten, RockContext rockContext )
            {
                throw new InvalidOperationException( "PDF merge failed." );
            }

            protected override void SaveAndAssignPdf( BinaryFile renderedPDF, Guid outputAttributeGuid, WorkflowAction action, RockContext rockContext )
            {
                // Should never be reached in the throwing scenario.
            }
        }

        private class TestWorkflowAction : WorkflowAction
        {
            private readonly WorkflowActionTypeCache _cache;

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

        // -----------------------------------------------------------------------------------------
        // Tests
        // -----------------------------------------------------------------------------------------

        [TestMethod]
        public void Execute_CallsMergePdfTemplate_AndDoesNotSave_When_Entity_Is_PDFWorkflowObject()
        {
            // Arrange
            var templateGuid = Guid.NewGuid();
            var values = new Dictionary<string, string>
            {
                { "PDFTemplate", templateGuid.ToString() },
                { "PDFOutput", string.Empty },
                { "Flatten", "false" }
            };

            var actionTypeCache = CreateActionTypeCacheWithValues( values );
            var action = new TestWorkflowAction( actionTypeCache )
            {
                Activity = new WorkflowActivity { Workflow = new Workflow() }
            };

            var entityObject = new PDFWorkflowObject { MergeObjects = new Dictionary<string, object>() };
            var sut = new TestPDFFormMerge( entityObject );

            // Act
            List<string> errors;
            bool result = sut.Execute( null, action, entityObject, out errors );

            // Assert
            Assert.IsTrue( result );
            Assert.AreEqual( 0, errors.Count );
            Assert.IsTrue( sut.MergePdfTemplateCalled, "MergePdfTemplate should have been called." );
            Assert.AreEqual( templateGuid, sut.ReceivedTemplateGuid );
            Assert.IsFalse( sut.SaveAndAssignPdfCalled, "SaveAndAssignPdf should not be called when entity is PDFWorkflowObject." );
        }

        [TestMethod]
        public void Execute_CallsMergePdfTemplate_And_SaveAndAssignPdf_When_Entity_Is_Not_PDFWorkflowObject()
        {
            // Arrange
            var templateGuid = Guid.NewGuid();
            var outputGuid = Guid.NewGuid();
            var values = new Dictionary<string, string>
            {
                { "PDFTemplate", templateGuid.ToString() },
                { "PDFOutput", outputGuid.ToString() },
                { "Flatten", "false" }
            };

            var actionTypeCache = CreateActionTypeCacheWithValues( values );
            var action = new TestWorkflowAction( actionTypeCache )
            {
                Activity = new WorkflowActivity { Workflow = new Workflow() }
            };

            var sut = new TestPDFFormMerge();

            // Act
            List<string> errors;
            bool result = sut.Execute( null, action, null, out errors );

            // Assert
            Assert.IsTrue( result );
            Assert.AreEqual( 0, errors.Count );
            Assert.IsTrue( sut.MergePdfTemplateCalled, "MergePdfTemplate should have been called." );
            Assert.AreEqual( templateGuid, sut.ReceivedTemplateGuid );
            Assert.IsTrue( sut.SaveAndAssignPdfCalled, "SaveAndAssignPdf should be called when entity is not a PDFWorkflowObject." );
            Assert.AreEqual( outputGuid, sut.SavedOutputAttributeGuid );
            Assert.IsNotNull( sut.SavedRenderedPDF );
            Assert.AreEqual( action, sut.SavedActionParameter );
        }

        [TestMethod]
        public void Execute_PassesFlattenTrue_To_MergePdfTemplate()
        {
            // Arrange
            var values = new Dictionary<string, string>
            {
                { "PDFTemplate", Guid.NewGuid().ToString() },
                { "PDFOutput", string.Empty },
                { "Flatten", "true" }
            };

            var actionTypeCache = CreateActionTypeCacheWithValues( values );
            var action = new TestWorkflowAction( actionTypeCache )
            {
                Activity = new WorkflowActivity { Workflow = new Workflow() }
            };

            var sut = new TestPDFFormMerge();

            // Act
            List<string> errors;
            sut.Execute( null, action, null, out errors );

            // Assert
            Assert.IsTrue( sut.ReceivedFlatten, "flatten=true should be forwarded to MergePdfTemplate." );
        }

        [TestMethod]
        public void Execute_PassesFlattenFalse_To_MergePdfTemplate()
        {
            // Arrange
            var values = new Dictionary<string, string>
            {
                { "PDFTemplate", Guid.NewGuid().ToString() },
                { "PDFOutput", string.Empty },
                { "Flatten", "false" }
            };

            var actionTypeCache = CreateActionTypeCacheWithValues( values );
            var action = new TestWorkflowAction( actionTypeCache )
            {
                Activity = new WorkflowActivity { Workflow = new Workflow() }
            };

            var sut = new TestPDFFormMerge();

            // Act
            List<string> errors;
            sut.Execute( null, action, null, out errors );

            // Assert
            Assert.IsFalse( sut.ReceivedFlatten, "flatten=false should be forwarded to MergePdfTemplate." );
        }

        [TestMethod]
        public void Execute_PassesMergeObjects_From_ResolvedWorkflowObject_To_MergePdfTemplate()
        {
            // Arrange
            var mergeObjects = new Dictionary<string, object> { { "FirstName", "Jane" } };
            var resolvedObject = new PDFWorkflowObject { MergeObjects = mergeObjects };

            var values = new Dictionary<string, string>
            {
                { "PDFTemplate", Guid.NewGuid().ToString() },
                { "PDFOutput", string.Empty },
                { "Flatten", "false" }
            };

            var actionTypeCache = CreateActionTypeCacheWithValues( values );
            var action = new TestWorkflowAction( actionTypeCache )
            {
                Activity = new WorkflowActivity { Workflow = new Workflow() }
            };

            var sut = new TestPDFFormMerge( resolvedObject );

            // Act
            List<string> errors;
            sut.Execute( null, action, null, out errors );

            // Assert
            Assert.IsNotNull( sut.ReceivedWorkflowObject );
            Assert.AreSame( resolvedObject, sut.ReceivedWorkflowObject );
            Assert.AreEqual( "Jane", sut.ReceivedWorkflowObject.MergeObjects["FirstName"] );
        }

        [TestMethod]
        public void Execute_ReturnsFalse_And_PopulatesErrors_When_MergePdfTemplate_Throws()
        {
            // Arrange
            var values = new Dictionary<string, string>
            {
                { "PDFTemplate", Guid.NewGuid().ToString() },
                { "PDFOutput", string.Empty },
                { "Flatten", "false" }
            };

            var actionTypeCache = CreateActionTypeCacheWithValues( values );
            var action = new TestWorkflowAction( actionTypeCache )
            {
                Activity = new WorkflowActivity { Workflow = new Workflow() }
            };

            var sut = new ThrowingPDFFormMerge();

            // Act
            List<string> errors;
            bool result = sut.Execute( null, action, null, out errors );

            // Assert
            Assert.IsFalse( result );
            Assert.AreEqual( 1, errors.Count );
            Assert.AreEqual( "PDF merge failed.", errors[0] );
        }
    }
}