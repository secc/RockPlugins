using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;



namespace org.secc.PayPalReporting.Services
{
    class PayFlowProRequest
    {
        #region Fields
        private string mURL = String.Empty;
        private int mRequestTimeout = 100000; //default timeout is 100 seconds.
        private bool mForceSSL = true; 

        #endregion

        #region "Properties"
        public int RequestTimeout 
        {
            get
            {
                return mRequestTimeout;
            }
            set
            {
                mRequestTimeout = value;
            }
        }

        public string URL
        {
            get
            {
                return mURL;
            }
            set
            {
                mURL = value;
            }
        }

        public bool ForceSSL
        {
            get
            {
                return mForceSSL;
            }
        }

        #endregion

        #region Constructors
        public PayFlowProRequest() { }

        public PayFlowProRequest(string url)
        {
            URL = url;
        }

        public PayFlowProRequest(string url, int timeout)
        {
            URL = url;
            RequestTimeout = timeout;
        }
        #endregion

        #region Public Methods
        public reportingEngineResponse SendRequest(reportingEngineRequest reportRequest)
        {
            if (reportRequest == null)
            {
                throw new ArgumentNullException("reportRequest", "Report Request object must be provided.");
            }

            if (String.IsNullOrEmpty(URL))
            {
                throw new Exception("Value must be provided for URL property.");
            }

            FormatEndpointURL();

            reportingEngineResponse reportResponse = null;
            try
            {
                System.Net.WebRequest httpReq = System.Net.WebRequest.Create(URL);
                httpReq.Method = "POST";
                httpReq.ContentType = "text/plain";
                //httpReq.Timeout = RequestTimeout;

                using(StreamWriter xmlStream = new StreamWriter(httpReq.GetRequestStream()))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(reportingEngineRequest));
                    XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                    ns.Add(String.Empty, String.Empty);
                    serializer.Serialize(xmlStream, reportRequest,ns);
                }


                System.Net.WebResponse httpResp = httpReq.GetResponse();


                if (httpResp == null)
                {
                    throw new Exception("Null response returned from " + URL);
                }


                if (httpReq.ContentType != "text/plain")
                {
                    throw new Exception(httpResp.ContentType + " was returned. text/xml was expected.");
                }

                using (StreamReader xmlStream = new StreamReader(httpResp.GetResponseStream()))
                {

                    //using (StreamWriter xmlWrite = new StreamWriter(@"C:\projects\results.xml"))
                    //{
                    //    xmlWrite.Write(xmlStream.ReadToEnd());
                    //}
                    XmlSerializer serializer = new XmlSerializer(typeof(reportingEngineResponse));
                    reportResponse = (reportingEngineResponse)serializer.Deserialize(xmlStream);
                    
                }

            }
            catch (System.Net.WebException webEx)
            {
                throw new Exception("An error has occurred connecting to "  + URL, webEx);
            }
            catch (Exception ex)
            {

                throw ex;
            }

            return reportResponse; 

        }

        public void TestRequest(reportingEngineRequest req, string fileName)
        {
            StreamWriter writer = new StreamWriter(fileName);
            System.Xml.Serialization.XmlSerializer serial = new System.Xml.Serialization.XmlSerializer(req.GetType());
            XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
            ns.Add(String.Empty, String.Empty);
            serial.Serialize(writer, req, ns);
        }
     
        #endregion

        #region PrivateMethods
        private void FormatEndpointURL()
        {
            if (!URL.StartsWith("https://"))
            {
                if (!URL.StartsWith("http://"))
                {
                    URL = "https://" + URL;
                }
                else
                {
                    URL.Replace("http://", "https://");
                }
            }
        }
        #endregion
    }
}
