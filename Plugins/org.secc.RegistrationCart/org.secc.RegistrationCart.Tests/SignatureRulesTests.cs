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
#nullable enable
using System;
using org.secc.RegistrationCart.Domain;
using Xunit;

namespace org.secc.RegistrationCart.Tests;

public class SignatureRulesTests
{
    private static readonly byte[] PngHeader = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00 };

    private static string DataUrl(byte[] bytes, string type = "image/png") =>
        $"data:{type};base64,{Convert.ToBase64String(bytes)}";

    [Fact]
    public void The_signature_controls_png_data_url_is_a_drawn_signature()
    {
        Assert.True(SignatureRules.IsDrawnSignatureImage(DataUrl(PngHeader)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("https://example.org/signature.png")]              // the PDF renderer would fetch it
    [InlineData("//example.org/signature.png")]
    [InlineData("file:///c:/windows/win.ini")]
    [InlineData("data:image/svg+xml;base64,PHN2Zy8+")]
    [InlineData("data:image/png;base64,not base64!")]
    [InlineData("data:image/png;base64,")]
    [InlineData(" data:image/png;base64,iVBORw0KGgoAAAA=")]
    public void Anything_else_is_not(string? signatureData)
    {
        Assert.False(SignatureRules.IsDrawnSignatureImage(signatureData));
    }

    [Fact]
    public void A_data_url_that_only_claims_to_be_a_png_is_not()
    {
        Assert.False(SignatureRules.IsDrawnSignatureImage(DataUrl(new byte[] { 0x3C, 0x73, 0x76, 0x67, 0x2F, 0x3E, 0x00, 0x00, 0x00, 0x00 })));
    }

    [Fact]
    public void An_oversized_value_is_not()
    {
        var bytes = new byte[SignatureRules.MaxDrawnSignatureLength];
        PngHeader.CopyTo(bytes, 0);

        Assert.False(SignatureRules.IsDrawnSignatureImage(DataUrl(bytes)));
    }
}
