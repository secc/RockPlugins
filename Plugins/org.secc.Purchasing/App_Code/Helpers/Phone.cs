using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace org.secc.Purchasing.Helpers
{
    [Serializable]
    public class PhoneNumber
    {
        private const string ArenaSeparator = "^";
        public string Number { get; set; }
        public string Extension { get; set; }

        public PhoneNumber() { }

        public PhoneNumber(string phone)
        {
            if (phone.IndexOf(ArenaSeparator) >= 0)
                ParseArenaPhone(phone);
            else
            {
                Number = RemoveFormatting(phone);
                Extension = String.Empty;
            }
        }

        public PhoneNumber(string phone, string extension)
        {
            Number = RemoveFormatting(phone);
            if(!String.IsNullOrEmpty(extension))
                Extension = extension.Trim();
        }

        private void ParseArenaPhone(string phone)
        {
            if (String.IsNullOrEmpty(phone))
                throw new ArgumentNullException("Phone", "Phone Number is required.");
            
            string[] PhoneParts = phone.Split(ArenaSeparator.ToCharArray());

            if (PhoneParts.Length != 2)
                throw new ArgumentException("Phone", "Arena phone number is not formatted properly.");

            if (String.IsNullOrEmpty(PhoneParts[0].Trim()))
                throw new ArgumentException("Phone", "Phone Number can not be parsed.");

            Number = RemoveFormatting(PhoneParts[0]);
            
            if(!String.IsNullOrEmpty(PhoneParts[1]))
            {
                Extension = PhoneParts[1].Trim();
            }
        }

        public string FormatNumber()
        {
            return FormatNumber(false);
        }

        public string FormatNumber(bool includeExtension)
        {
            Dictionary<string, string> ValErrors = Validate();
            if (ValErrors.Count > 0)
                throw new RequisitionNotValidException("Phone number is not valid.", ValErrors);

            string n = string.Format("({0}) {1}-{2}", Number.Substring(0, 3), Number.Substring(3, 3), Number.Substring(6, 4));

            if (includeExtension && !string.IsNullOrEmpty(Extension))
                n = n + string.Format(" Ext. {0}", Extension);

            return n;
        }

        public bool IsValid()
        {
            return (Validate().Count == 0);
        }

        public string ToArenaFormat()
        {
            Dictionary<string, string> ValErrors = Validate();
            if (ValErrors.Count > 0)
                throw new RequisitionNotValidException("Phone number is not valid.", ValErrors);
            return FormatNumber(false) + ArenaSeparator + Extension;
        }

        private string RemoveFormatting(string phone)
        {
            Char[] CharsToRemove = @"().-/\ ".ToCharArray();

            phone = phone.Trim();
            foreach (Char c in CharsToRemove)
            {
                phone = phone.Replace(c.ToString(),String.Empty );
            }

            return phone;
        }


        public Dictionary<string, string> Validate()
        {
            Dictionary<string, string> ValErrors = new Dictionary<string, string>();

            if(!System.Text.RegularExpressions.Regex.IsMatch(Number, @"^\d{10}$"))
                ValErrors.Add("Phone Number", "Phone Number must be in (xxx) xxx-xxxx or xxx-xxx-xxxx format.");
            if(!String.IsNullOrEmpty(Extension) && !System.Text.RegularExpressions.Regex.IsMatch(Extension, @"^\d{1,10}$"))
                ValErrors.Add("Extension", "Extensions must be a numeric value.");

            return ValErrors;
        }


    }
}
