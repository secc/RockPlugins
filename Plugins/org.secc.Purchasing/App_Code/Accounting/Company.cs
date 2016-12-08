using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rock;
using org.secc.Purchasing.DataLayer;
using org.secc.Purchasing.DataLayer.Accounting;
using Rock.Model;
using Rock.Web.Cache;

namespace org.secc.Purchasing.Accounting
{
    public class Company
    {
        private const string DefaultCompanyKey = "DefaultAccountingCompanyID";

        private int mCompanyID;
        private string mCompanyName;
        private List<FiscalYear> mFiscalYears;
        private List<Fund> mFunds;
        private List<Department> mDepartments;
        private List<Account> mAccounts;

        public int CompanyID
        {
            get
            {
                return mCompanyID;
            }
        }

        public string CompanyName
        {
            get
            {
                return mCompanyName;
            }
        }

        public List<FiscalYear> FiscalYears
        {
            get
            {
                if (mFiscalYears == null && mCompanyID > 0)
                {
                    mFiscalYears = GetFiscalYears();
                }

                return mFiscalYears;
            }
        }

        public List<Fund> Funds
        {
            get
            {
                if (mFiscalYears == null && mCompanyID > 0)
                    mFunds = GetFunds();
                return mFunds;
            }
        }

        public List<Department> Departments
        {
            get
            {
                if (mDepartments == null && mCompanyID > 0)
                    mDepartments = GetDepartments();
                return mDepartments;
            }
        }

        private List<Account> Accounts
        {
            get
            {
                if (mAccounts == null && mCompanyID > 0)
                    mAccounts = GetAccounts();
                return mAccounts;
            }
        }

        public Company() { }

        public Company(int companyID)
        {
            LoadByID(companyID);
        }

        public Company(GLCompanyData d)
        {
            Load(d);
        }

        public static List<Company> GetCompanies()
        {
            using ( ShelbyContext Context = ContextHelper.GetShelbyContext() )
            {
                return Context.GLCompanyDatas
                        .Select( c => new Company( c ) )
                        .ToList();
            }
        }

        public static Company GetDefaultCompany()
        {
            int? DefaultCompanyID = GlobalAttributesCache.Value( DefaultCompanyKey ).AsIntegerOrNull();

            return new Company(DefaultCompanyID.Value);
        }

        public FiscalYear GetCurrentFiscalYear()
        {
            return FiscalYears.Where(f => f.StartDate <= DateTime.Today && f.EndDate >= DateTime.Today).FirstOrDefault();
        }

        private void Init()
        {
            mCompanyID = 0;
            mCompanyName = String.Empty;
            mFiscalYears = null;
            mFunds = null;
            mDepartments = null;
            mAccounts = null;
        }

        private List<FiscalYear> GetFiscalYears()
        {
            using (ShelbyContext Context = ContextHelper.GetShelbyContext())
            {
                return Context.GLCompanyDatas
                        .Where( x => x.CompanyNumber == mCompanyID )
                        .FirstOrDefault()
                        .GLYearDatas
                        .Select( y => new FiscalYear( y ) )
                        .ToList();
            }
        }

        private List<Account> GetAccounts()
        {
            using ( ShelbyContext Context = ContextHelper.GetShelbyContext() )
            {
                return Context.GLCompanyDatas
                        .Where( c => c.CompanyNumber == mCompanyID )
                        .FirstOrDefault()
                        .GLAccountDatas
                        .Select( a => new Account( a ) )
                        .ToList();
            }
        }

        private List<Department> GetDepartments()
        {
            using ( ShelbyContext Context = ContextHelper.GetShelbyContext() )
            {
                return Context.GLCompanyDatas
                        .Where( c => c.CompanyNumber == mCompanyID )
                        .FirstOrDefault()
                        .GLDepartmentDatas
                        .Select( d => new Department( d ) )
                        .ToList();
            }
        }

        private List<Fund> GetFunds()
        {
            using(ShelbyContext Context = ContextHelper.GetShelbyContext())
            {
                return Context.GLCompanyDatas
                        .Where( c => c.CompanyNumber == mCompanyID )
                        .FirstOrDefault()
                        .GLFundDatas
                        .Select( f => new Fund( f ) )
                        .ToList();
            }
        }



        private void Load(GLCompanyData d)
        {
            Init();
            if (d != null)
            {
                mCompanyID = d.CompanyNumber;
                mCompanyName = d.Name;
            }
        }

        private void LoadByID(int id)
        {
            if (id <= 0)
                throw new ArgumentNullException("ID", "Company ID is required.");

            using ( ShelbyContext Context = ContextHelper.GetShelbyContext() )
            {
                Load( Context.GLCompanyDatas.FirstOrDefault( c => c.CompanyNumber == id ) );
            }
        }

    }
}
