using org.secc.PayPalReporting.Services;
using System;
using System.Data;
using Rock;

namespace org.secc.PayPalReporting.Engine
{
    class XMLReport
    {
        const int REPORTSTATUS_CREATEDSUCCESSFULLY = 3;

        public String URL { get; set;}

        private int pollingTimeout = 10;
        public int PollingTimeout {
            get {
                return pollingTimeout;
            }
            set
            {
                pollingTimeout = value;
            }
        }

        private DataTable Data;

        public String name = "GivingReport";
        public String Name
        {
            get
            {
                return name;
            }
            set
            {
                name = value;
            }
        }

        public Rock.Model.FinancialGateway Gateway { get; set;}

        public DataTable RunReport(DateTime startDate, DateTime endDate)
        {
            // Report variables
            string reportId = "";
            int totalPages = 0;
            int statusCode = 0;

            // Build the report parameters
            reportParam[] ReportParams = new reportParam[2];
            ReportParams[0] = new Services.reportParam()
            {
                paramName = "start_date",
                paramValue = String.Format("{0:yyyy-MM-dd HH:mm:ss}", startDate)
            };
            ReportParams[1] = new Services.reportParam()
            {
                paramName = "end_date",
                paramValue = String.Format("{0:yyyy-MM-dd HH:mm:ss}", endDate)
            };

            // Setup the report and bind parameters to it.
            reportingEngineRequestRunReportRequest reportRequest = new Services.reportingEngineRequestRunReportRequest();
            reportRequest.ItemElementName = reportType.templateName;
            reportRequest.Item = Name;
            reportRequest.pageSize = 50;
            reportRequest.pageSizeSpecified = true;
            reportRequest.reportParam = ReportParams;


            // Setup the request, with authentication
            reportingEngineRequest req = new reportingEngineRequest();
            req.authRequest = GetAuthRequest();
            req.Item = reportRequest;
            
            reportingEngineResponse resp = SendRequest(req);

            // Get the response from the report request
            reportingEngineResponseRunReportResponse reportResp = (reportingEngineResponseRunReportResponse)resp.Item;
            if (reportResp == null)
            {
                throw new Exception(resp.baseResponse.responseCode + ": " + resp.baseResponse.responseMsg);
            }

            reportId = reportResp.reportId;
            statusCode = reportResp.statusCode;

            // Now just wait until PayPal is done generating the report
            while (statusCode != REPORTSTATUS_CREATEDSUCCESSFULLY)
            {
                //wait before checking status... 
                System.Threading.Thread.Sleep(PollingTimeout * 1000);

                Services.reportingEngineRequestGetResultsRequest resultsRequest = new Services.reportingEngineRequestGetResultsRequest();
                resultsRequest.ItemElementName = ItemChoiceType3.reportId;
                resultsRequest.Item = reportId;

                resp = SendRequest(req);

                statusCode = ((reportingEngineResponseRunReportResponse)resp.Item).statusCode;
            }

            // Get the total number of pages (from the report metadata)
            Services.reportingEngineRequestGetMetaDataRequest metadataRequest = new Services.reportingEngineRequestGetMetaDataRequest();
            metadataRequest.reportId = reportId;
            
            req.Item = metadataRequest;
            resp = SendRequest(req);
            reportingEngineResponseGetMetaDataResponse metaResp = (reportingEngineResponseGetMetaDataResponse)resp.Item;
            
            totalPages = metaResp.numberOfPages;
            SetupDataTable(metaResp.columnMetaData);
            

            // Now loop and aggregate all the pages together
            for (int p = 0; p < totalPages; p++)
            {
                Services.reportingEngineRequestGetDataRequest dataRequest = new Services.reportingEngineRequestGetDataRequest();
                dataRequest.reportId = reportId;
                dataRequest.pageNum = p+1;
                dataRequest.pageNumSpecified = true;

                req.Item = dataRequest;
                resp = SendRequest(req);

                reportingEngineResponseGetDataResponse dataResp = (reportingEngineResponseGetDataResponse)resp.Item;

                LoadDataRows(dataResp.reportDataRow);
            }

            return Data;
        }

        private reportingEngineResponse SendRequest(reportingEngineRequest req)
        {

            PayFlowProRequest pfpReq = new PayFlowProRequest();
            pfpReq.URL = URL;
            reportingEngineResponse resp = pfpReq.SendRequest(req);
            if (resp.Item == null)
            {
                throw new Exception(resp.baseResponse.responseCode + ": " + resp.baseResponse.responseMsg);
            }
            return resp;
        }

        private reportingEngineRequestAuthRequest GetAuthRequest()
        {
            reportingEngineRequestAuthRequest authRequest = new reportingEngineRequestAuthRequest();
            Gateway.LoadAttributes();
            authRequest.partner = Gateway.GetAttributeValue("Partner");
            authRequest.password = Gateway.GetAttributeValue("Password");
            authRequest.user = String.IsNullOrEmpty(Gateway.GetAttributeValue("User"))? Gateway.GetAttributeValue("Vendor"): Gateway.GetAttributeValue("User");
            authRequest.vendor = Gateway.GetAttributeValue("Vendor");
            return authRequest;
        }

        /// <summary>
        /// Setup the datatable from the column data from a metadata request
        /// </summary>
        /// <param name="columnData"></param>
        private void SetupDataTable(Services.reportingEngineResponseGetMetaDataResponseColumnMetaData[] columnData)
        {
            if (Data != null)
            {
                Data = null;
            }

            Data = new DataTable(Name);

            for (int i = 0; i < columnData.Length; i++)
            {
                DataColumn col = new DataColumn(columnData[i].dataName);
                switch (columnData[i].dataType.ToString().ToLower())
                {
                    case "string":
                        col.DataType = typeof(string);
                        break;
                    case "number":
                        col.DataType = typeof(decimal);
                        break;
                    case "date":
                        col.DataType = typeof(DateTime);
                        break;
                    case "currency":
                        col.DataType = typeof(decimal);
                        break;
                    default:
                        col.DataType = typeof(string);
                        break;
                }

                Data.Columns.Add(col);
            }

        }

        /// <summary>
        /// Load all of the row data from a data request (1 page at a time) into the datatable
        /// </summary>
        /// <param name="rowData"></param>
        private void LoadDataRows(Services.reportingEngineResponseGetDataResponseReportDataRow[] rowData)
        {


            for (int iRow = 0; iRow < rowData.Length; iRow++)
            {
                int columnCount = 0;
                if (rowData[iRow].columnData.Length <= Data.Columns.Count)
                {
                    columnCount = rowData[iRow].columnData.Length;
                }
                else
                {
                    columnCount = Data.Columns.Count;
                }
                DataRow dr = Data.NewRow();
                for (int iColumn = 0; iColumn < columnCount; iColumn++)
                {
                    string dataItem = rowData[iRow].columnData[iColumn].data;

                    if (Data.Columns[iColumn].DataType == typeof(System.String))
                    {
                        dr[iColumn] = dataItem;
                    }
                    else if (Data.Columns[iColumn].DataType == typeof(System.Decimal))
                    {
                        if (!String.IsNullOrEmpty(dataItem.Trim()))
                            dr[iColumn] = Decimal.Parse(dataItem);
                    }
                    else if (Data.Columns[iColumn].DataType == typeof(System.DateTime))
                    {
                        if (!String.IsNullOrEmpty(dataItem.Trim()))
                            dr[iColumn] = DateTime.Parse(dataItem);
                    }
                    else
                    {
                        dr[iColumn] = dataItem;
                    }

                }
                Data.Rows.Add(dr);
            }

        }

    }
}
