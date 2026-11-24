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

namespace org.secc.RegistrationCart.Domain;

/// <summary>Rules for what the browser sends as a signature.</summary>
public static class SignatureRules
{
    private const string DrawnSignaturePrefix = "data:image/png;base64,";

    /// <summary>A drawn signature is at most this long (the signature control's PNG is a few thousand characters).</summary>
    public const int MaxDrawnSignatureLength = 1000000;

    /// <summary>
    /// Whether a drawn signature is what Rock's signature control sends: a PNG as a data URL.
    /// Rock puts the value in an image's src when it makes the signed document's PDF, so
    /// anything else (a web address, say) would have the server fetch it.
    /// </summary>
    public static bool IsDrawnSignatureImage(string? signatureData)
    {
        if (signatureData is null
            || signatureData.Length > MaxDrawnSignatureLength
            || !signatureData.StartsWith(DrawnSignaturePrefix, StringComparison.Ordinal))
        {
            return false;
        }

        try
        {
            var image = Convert.FromBase64String(signatureData.Substring(DrawnSignaturePrefix.Length));
            return image.Length > 8 && image[0] == 0x89 && image[1] == 0x50 && image[2] == 0x4E && image[3] == 0x47;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
