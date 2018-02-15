// <copyright>
// Copyright Southeast Christian Church
//
// Licensed under the  Southeast Christian Church License (the "License");
// you may not use this file except in compliance with the License.
// A copy of the License shoud be included with this file.
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using org.secc.Purchasing.DataLayer;
using org.secc.Purchasing.DataLayer.Accounting;

namespace org.secc.Purchasing.Accounting
{
    /// <summary>
    /// General Ledger Accounts
    /// </summary>
    public class Account
    {
        private int mCompanyID;
        private int mFundID;
        private int mDepartmentID;
        private int mAccountID;
        private DateTime mStartDate;
        private string mAccountName;
        private Company mCompany;
        private Fund mFund;
        private Department mDepartment;
        private FiscalYear mFiscalYear;

        public int CompanyID
        {
            get
            {
                return mCompanyID;
            }
        }

        public int FundID
        {
            get
            {
                return mFundID;
            }
        }

        public int DepartmentID
        {
            get
            {
                return mDepartmentID;
            }
        }

        public int AccountID
        {
            get
            {
                return mAccountID;
            }
        }

        public DateTime StartDate
        {
            get
            {
                return mStartDate;
            }
        }

        public string AccountName
        {
            get
            {
                return mAccountName;
            }
        }

        public Company Company
        {
            get
            {
                if (mCompany == null && IsPopulated())
                    mCompany = GetCompany();
                return mCompany;
            }
        }

        public FiscalYear FiscalYear
        {
            get
            {
                if (mFiscalYear == null && IsPopulated())
                    mFiscalYear = GetFiscalYear();
                return mFiscalYear;
            }
        }

        public Fund Fund
        {
            get
            {
                if (mFund == null && IsPopulated())
                    mFund = GetFund();
                return mFund;
            }
        }

        public Department Department
        {
            get
            {
                if (mDepartment == null && IsPopulated())
                    mDepartment = GetDepartment();
                return mDepartment;
            }
        }

        public Account() { }

        public Account(int companyID, int fundID, int departmentID, int accountID)
        {
            LoadAccount(companyID, fundID, departmentID, accountID, new Company(companyID).GetCurrentFiscalYear().StartDate);
        }

        public Account(int companyID, int fundID, int departmentID, int accountID, DateTime startDate)
        {
            LoadAccount(companyID, fundID, departmentID, accountID, startDate);
        }

        public Account(GLAccountData a)
        {
            Load(a);
        }

        private Company GetCompany()
        {
            using (ShelbyContext Context = ContextHelper.GetShelbyContext())
            {
                var companyData = Context.GLAccountDatas
                                    .Where( a => a.CompanyNumber == CompanyID )
                                    .Where( a => a.FundNumber == FundID )
                                    .Where( a => a.DepartmentNumber == DepartmentID )
                                    .Where( a => a.AccountNumber == AccountID )
                                    .Where( a => a.BeginDate == StartDate )
                                    .FirstOrDefault()
                                    .GLCompanyData;

                return new Company( companyData );
            }
        }

        private FiscalYear GetFiscalYear()
        {
            using (ShelbyContext Context = ContextHelper.GetShelbyContext())
            {
                var fyData = Context.GLAccountDatas
                                .Where( a => a.CompanyNumber == CompanyID )
                                .Where( a => a.FundNumber == FundID )
                                .Where( a => a.DepartmentNumber == DepartmentID )
                                .Where( a => a.AccountNumber == AccountID )
                                .Where( a => a.BeginDate == StartDate )
                                .FirstOrDefault()
                                .GLYearData;

                return new FiscalYear( fyData );
            }
        }

        private Fund GetFund()
        {
            using (ShelbyContext Context = ContextHelper.GetShelbyContext())
            {
                var fundData = Context.GLAccountDatas
                                .Where( a => a.CompanyNumber == CompanyID )
                                .Where( a => a.FundNumber == FundID )
                                .Where( a => a.DepartmentNumber == DepartmentID )
                                .Where( a => a.AccountNumber == AccountID )
                                .Where( a => a.BeginDate == StartDate )
                                .FirstOrDefault()
                                .GLFundData;

                return new Fund( fundData );
            }
        }

        private Department GetDepartment()
        {
            using (ShelbyContext Context = ContextHelper.GetShelbyContext())
            {
                var departmentData = Context.GLAccountDatas
                                        .Where( a => a.CompanyNumber == CompanyID )
                                        .Where( a => a.FundNumber == FundID )
                                        .Where( a => a.DepartmentNumber == DepartmentID )
                                        .Where( a => a.AccountNumber == AccountID )
                                        .Where( a => a.BeginDate == StartDate )
                                        .FirstOrDefault()
                                        .GLDepartmentData;

                return new Department( departmentData );
            }
        }
        private void Init()
        {
            mCompanyID = 0;
            mFundID = 0;
            mDepartmentID = 0;
            mAccountID = 0;
            mStartDate = DateTime.Now;
            mAccountName = String.Empty;
        }

        private bool IsPopulated()
        {
            if (mCompanyID > 0 && mFundID > 0 && mDepartmentID > 0 && mAccountID > 0 && mStartDate > DateTime.MinValue)
                return true;
            else
                return false;
        }

        private void Load(GLAccountData a)
        {
            Init();
            if(a != null)
            {
                mCompanyID = a.CompanyNumber;
                mFundID = a.FundNumber;
                mDepartmentID = a.DepartmentNumber;
                mAccountID = a.AccountNumber;
                mStartDate = a.BeginDate;
                mAccountName = a.Description;
            }

        }

        private void LoadAccount(int companyID, int fundID, int departmentID, int accountID, DateTime startDate)
        {
            if (companyID <= 0)
                throw new ArgumentNullException("Company ID", "Company ID is required.");
            if (fundID <= 0)
                throw new ArgumentNullException("Fund ID", "Fund ID is required.");
            if (departmentID <= 0)
                throw new ArgumentNullException("Department ID", "Department ID is required.");
            if (accountID <= 0)
                throw new ArgumentNullException("Account ID", "Account ID is required.");
            if (startDate == DateTime.MinValue)
                throw new ArgumentNullException("Start Date", "Start Date is required.");

            using ( ShelbyContext Context = ContextHelper.GetShelbyContext() )
            {
                var accountData = Context.GLAccountDatas
                                    .Where( a => a.CompanyNumber == companyID )
                                    .Where( a => a.FundNumber == fundID )
                                    .Where( a => a.DepartmentNumber == departmentID )
                                    .Where( a => a.AccountNumber == accountID )
                                    .Where( a => a.BeginDate == startDate )
                                    .FirstOrDefault();
                Load( accountData );
            }
        }
    }
}
