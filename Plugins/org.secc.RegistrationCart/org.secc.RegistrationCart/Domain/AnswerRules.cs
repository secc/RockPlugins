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
using System.Linq;
using Newtonsoft.Json.Linq;

namespace org.secc.RegistrationCart.Domain;

/// <summary>How a form answer is stored, which decides what an empty one looks like.</summary>
public enum AnswerKind
{
    /// <summary>Plain text, a Guid, or an attribute's stored value.</summary>
    Text,
    /// <summary>Rock's address control as JSON (street1, street2, city, state, postalCode, country).</summary>
    Address,
    /// <summary>Rock's birthday picker as JSON (year, month, day).</summary>
    Date,
    /// <summary>A phone number, or the SMS phone control's JSON (number, isMessagingEnabled).</summary>
    Phone,
    /// <summary>Rock's gender value ("0" = unknown).</summary>
    Gender
}

/// <summary>
/// Whether a stored form answer is empty, the way Rock's own controls treat a required field
/// as unanswered. Pure — no Rock.
/// </summary>
public static class AnswerRules
{
    public static bool IsBlank(AnswerKind kind, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return true;

        switch (kind)
        {
            case AnswerKind.Address:
                // A country on its own (the control fills it in) is not an address.
                var address = Parse(value!);
                return address is null || new[] { "street1", "street2", "city", "state", "postalCode" }.All(key => string.IsNullOrWhiteSpace(Property(address, key)));

            case AnswerKind.Date:
                // Rock's birthday picker needs a month and a day (the year may be left out).
                var date = Parse(value!);
                return date is null || IsZero(Property(date, "month")) || IsZero(Property(date, "day"));

            case AnswerKind.Phone:
                var phone = Parse(value!);
                var number = phone is null ? value : Property(phone, "number");
                return string.IsNullOrWhiteSpace(number) || !number!.Any(char.IsDigit);

            case AnswerKind.Gender:
                return value!.Trim() == "0";

            default:
                return false;
        }
    }

    private static JObject? Parse(string value)
    {
        var text = value.Trim();
        if (!text.StartsWith("{"))
            return null;

        try
        {
            return JObject.Parse(text);
        }
        catch (Newtonsoft.Json.JsonException)
        {
            return null;
        }
    }

    /// <summary>A property's text, matching its name case-insensitively (the browser sends camelCase, C# bags PascalCase).</summary>
    private static string? Property(JObject json, string name) =>
        json.Properties().FirstOrDefault(p => string.Equals(p.Name, name, System.StringComparison.OrdinalIgnoreCase))?.Value?.ToString();

    private static bool IsZero(string? number) =>
        string.IsNullOrWhiteSpace(number) || (int.TryParse(number, out var n) && n == 0);
}
