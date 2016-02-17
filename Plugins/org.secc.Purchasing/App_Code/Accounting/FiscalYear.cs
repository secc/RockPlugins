using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using org.secc.Purchasing.DataLayer;
using org.secc.Purchasing.DataLayer.Accounting;


namespace org.secc.Purchasing.Accounting
{
    public class FiscalYear
    {
        private short mCompanyID;
        private DateTime mStartDate;
        private DateTime mEndDate;

        private Company mFiscalCompany;

        public short CompanyID
        {
            get
            {
                return mCompanyID;
            }
        }

        public DateTime StartDate
        {
            get
            {
                return mStartDate;
            }
        }

        public DateTime EndDate
        {
            get
            {
                return mEndDate;
            }
        }

        public Company FiscalCompany
        {
            get
            {
                if (mFiscalCompany == null && mCompanyID > 0)
                {
                    mFiscalCompany = new Company(mCompanyID);
                }

                return mFiscalCompany;
            }
        }


        public FiscalYear() { }


        public FiscalYear(int companyID, DateTime startDate)
        {
            LoadByCompanyStartDate(companyID, startDate);
        }

        public FiscalYear(GLYearData y)
        {
            Load( y );
        }

        public static List<FiscalYear> GetFiscalYears()
        {
            using ( ShelbyContext Context = ContextHelper.GetShelbyContext() )
            {
                return Context.GLYearDatas
                        .Select( y => new FiscalYear( y ) )
                        .ToList();
            }
        }

        public static List<FiscalYear> GetFiscalYearsByCompany(int companyID)
        {
            if (companyID <= 0)
                throw new ArgumentNullException("CompanyID", "Company ID is required.");

            using ( ShelbyContext Context = ContextHelper.GetShelbyContext() )
            {
                return Context.GLYearDatas
                    .Where( y => y.CompanyNumber == companyID )
                    .Select( y => new FiscalYear( y ) )
                    .ToList();
            }
        }

        private void Init()
        {
            mCompanyID = 0;
            mStartDate = DateTime.MinValue;
            mEndDate = DateTime.MinValue;
            mFiscalCompany = null;
        }

        private void Load(GLYearData y)
        {
            Init();

            if (y != null)
            {
                mCompanyID = y.CompanyNumber;
                mStartDate = y.BeginDate;

                if (y.EndDate != null)
                    mEndDate = (DateTime)y.EndDate;
            }
        }

        private void LoadByCompanyStartDate(int companyID, DateTime startDate)
        {
            using ( ShelbyContext Context = ContextHelper.GetShelbyContext() )
            {
                var fiscalYear = Context.GLYearDatas
                                    .Where( y => y.CompanyNumber == companyID )
                                    .Where( y => y.BeginDate == startDate )
                                    .FirstOrDefault();

                Load( fiscalYear );
            }
        }

    }
}
