using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using org.secc.Purchasing.DataLayer;
using org.secc.Purchasing.DataLayer.Accounting;

namespace org.secc.Purchasing.Accounting
{
    public class Fund
    {
        private int mCompanyID;
        private int mFundID;
        private string mFundName;
        private Company mCompany;
        private List<Department> mDepartments;
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

        public string FundName
        {
            get
            {
                return mFundName;
            }
        }

        public Company Company
        {
            get
            {
                if (mCompany == null && mCompanyID >= 0 && mFundID >= 0)
                    mCompany = GetCompany();

                return mCompany;
            }
        }

        public List<Department> Departments
        {
            get
            {
                if (mDepartments == null && mCompanyID >= 0 && mFundID >= 0)
                    mDepartments = GetDepartments();
                return mDepartments;
            }
        }

        public List<Account> Accounts
        {
            get
            {
                if (mAccounts == null && mCompanyID >= 0 && mFundID >= 0)
                    mAccounts = GetAccounts();

                return mAccounts;
            }
        }

        public Fund() { }

        public Fund(int companyID, int fundID)
        {
            LoadByCompanyFund(companyID, fundID);
        }

        public Fund(GLFundData f)
        {
            Load(f);
        }

        public static List<Fund> GetFunds()
        {
            using ( ShelbyContext Context = ContextHelper.GetShelbyContext() )
            {
                return Context.GLFundDatas
                        .Select( f => new Fund( f ) )
                        .ToList();
            }
        }

        public static List<Fund> GetFundByCompany(int companyID)
        {
            using ( ShelbyContext Context = ContextHelper.GetShelbyContext() )
            {
                return Context.GLFundDatas
                        .Where( f => f.CompanyNumber == companyID )
                        .Select( f => new Fund( f ) )
                        .ToList();
            }
        }

        private Company GetCompany()
        {
            using (ShelbyContext Context = ContextHelper.GetShelbyContext())
            {
                return new Company(Context.GLFundDatas
                                .Where(f => f.CompanyNumber == CompanyID )
                                .Where(f => f.FundNumber == FundID)
                                .FirstOrDefault().GLCompanyData);
            }
        }

        private List<Department> GetDepartments()
        {
            List<Department> Departments = new List<Department>();
            using (ShelbyContext Context = ContextHelper.GetShelbyContext())
            {
                return Context.GLFundDatas
                    .Where( f => f.CompanyNumber == CompanyID )
                    .Where( f => f.FundNumber == FundID )
                    .FirstOrDefault()
                    .GLDepartmentDatas
                    .Select( d => new Department( d ) )
                    .ToList();
            }

        }

        private List<Account> GetAccounts()
        {
            List<Account> Accounts = new List<Account>();
            using (ShelbyContext Context = ContextHelper.GetShelbyContext())
            {
                return Context.GLFundDatas
                        .Where( f => f.CompanyNumber == CompanyID )
                        .Where( f => f.FundNumber == FundID )
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
            mFundName = String.Empty;
            mCompany = null;
           
        }

        private void Load(GLFundData f)
        {
            Init();

            if (f != null)
            {
                mCompanyID = f.CompanyNumber;
                mFundID = f.FundNumber;
                mFundName = f.Name;
            }
        }

        private void LoadByCompanyFund(int companyId, int fundID)
        {
            if (companyId <= 0)
                throw new ArgumentNullException("CompanyID", "Company ID is required.");
            if(fundID <= 0)
                throw new ArgumentNullException("FundID", "Fund ID is required.");

            using ( ShelbyContext Context = ContextHelper.GetShelbyContext() )
            {
                var fundData = Context.GLFundDatas
                                .Where( f => f.CompanyNumber == companyId )
                                .Where( f => f.FundNumber == fundID )
                                .FirstOrDefault();

                Load( fundData );
            }
        }

    }
}
