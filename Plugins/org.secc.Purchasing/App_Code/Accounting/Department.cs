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
    /// General Ledger Department List
    /// </summary>
    public class Department
    {
        private int mCompanyID;
        private int mFundID;
        private int mDepartmentID;
        private string mDepartmentName;

        private Company mCompany;
        private Fund mFund;
        private List<Account> mAccounts;
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

        public string DepartmentName
        {
            get
            {
                return mDepartmentName;
            }
        }

        public Company Company
        {
            get
            {
                if (mCompany == null)
                {
                    mCompany = GetCompany();
                }

                return mCompany;
            }
        }

        public Fund Fund
        {
            get
            {
                if (mFund == null)
                {
                    mFund = GetFund();
                }

                return mFund;
            }
        }

        public List<Account> Accounts
        {
            get
            {
                if (mAccounts == null)
                    mAccounts = GetAccounts();
                return mAccounts;
            }
        }

        public Department() { }

        public Department(int companyID, int fundID, int departmentID)
        {
            LoadDepartment(companyID, fundID, departmentID);
        }

        public Department(GLDepartmentData d)
        {
            Load(d);
        }

        public static List<Department> LoadDepartments()
        {
            using ( ShelbyContext Context = ContextHelper.GetShelbyContext() )
            {
                return Context.GLDepartmentDatas
                        .Select( d => new Department( d ) )
                        .ToList();
            }
        }

        private static List<Department> LoadDepartmentsByCompany(int companyID)
        {
            if (companyID <= 0)
                throw new ArgumentNullException("companyID", "Company ID is required");

            using ( ShelbyContext Context = ContextHelper.GetShelbyContext() )
            {
                return Context.GLDepartmentDatas
                        .Where( d => d.CompanyNumber == companyID )
                        .Select( d => new Department( d ) )
                        .ToList();
            }
        }

        private static List<Department> LoadByFund(int companyID, int fundID)
        {
            if (companyID <= 0)
                throw new ArgumentNullException("companyID", "Company ID is required.");

            if (fundID <= 0)
                throw new ArgumentNullException("fundID", "Fund ID is required.");


            using ( ShelbyContext Context = ContextHelper.GetShelbyContext() )
            {
                return Context.GLDepartmentDatas
                        .Where( d => d.CompanyNumber == companyID )
                        .Where( d => d.FundNumber == fundID )
                        .Select( d => new Department( d ) )
                        .ToList();
            }
        }

        private Company GetCompany()
        {
            using (ShelbyContext Context = ContextHelper.GetShelbyContext())
            {

                var companyData = Context.GLDepartmentDatas
                                    .Where( c => c.CompanyNumber == mCompanyID )
                                    .Where( c => c.FundNumber == mFundID )
                                    .Where( c => c.DepartmentNumber == mDepartmentID )
                                    .FirstOrDefault()
                                    .GLCompanyData;

                return new Company( companyData );
            }
        }

        private Fund GetFund()
        {
            using (ShelbyContext Context = ContextHelper.GetShelbyContext())
            {
                var fundData = Context.GLDepartmentDatas
                                .Where( c => c.CompanyNumber == mCompanyID )
                                .Where( c => c.FundNumber == mFundID )
                                .Where( c => c.DepartmentNumber == mDepartmentID )
                                .FirstOrDefault()
                                .GLFundData;

                return new Fund( fundData );
            }
        }

        private List<Account> GetAccounts()
        {
            using (ShelbyContext Context = ContextHelper.GetShelbyContext())
            {
                return Context.GLDepartmentDatas
                        .Where( d => d.CompanyNumber == mCompanyID )
                        .Where( d => d.FundNumber == mFundID )
                        .Where( d => d.DepartmentNumber == mDepartmentID )
                        .FirstOrDefault()
                        .GLAccountDatas
                        .Select( a => new Account( a ) )
                        .ToList();
            }

        }

        private void Init()
        {
            mCompanyID = 0;
            mFundID = 0;
            mDepartmentID = 0;
            mDepartmentName = String.Empty;
            mCompany = null;
            mFund = null;
            mAccounts = null;
        }

        private void Load(GLDepartmentData d)
        {
            Init();

            if (d != null)
            {
                mCompanyID = d.CompanyNumber;
                mFundID = d.FundNumber;
                mDepartmentID = d.DepartmentNumber;
                mDepartmentName = d.Name;
            }
        }

        private void LoadDepartment(int companyID, int fundID, int departmentID)
        {
            using ( ShelbyContext Context = ContextHelper.GetShelbyContext() )
            {
                var DepartmentData = Context.GLDepartmentDatas
                                        .Where( d => d.CompanyNumber == companyID )
                                        .Where( d => d.FundNumber == fundID )
                                        .Where( d => d.DepartmentNumber == departmentID )
                                        .FirstOrDefault();
                Load( DepartmentData );
            }
        }
    }
}
