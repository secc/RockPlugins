   A. Go out to elevatesoft.com and download odbc driver
        i. Click Login link - credentials at \\office\ministrydata\IT\Private\Applications\pw.xlsx
        ii. Click Downloads link
        iii. Click db-isam button (Access the DBISAM downloads)
        iv. Scroll down to DBISAM-ODBC-STD - DBISAM ODBC Standard section 
        v. Download latest build
	vi. Note: version in production as of 4/13/2017 is Version 4.43 Build 5
   B. Copy executable to web server, and install
	i. Symantec Endpoint Protection (allow this file)
        ii. Click Next on "Information" screen
        iii. Click Next on "Select Destination Location" screen (default location is fine)
        iv. Click Next on "Select Start Menu Folder" screen (leave "Don't create a Start Menu folder" unchecked)
        v. Click Install
        vi. Uncheck "View the Release Notes" and click Finish
   C. Configure ODBC
        i. at run prompt type "odbc" and select "ODBC Data Source Administrator (64-bit)"
               Note: dev server doesn't have 64-bit option
        ii. Click on "System DSN" folder
        iii. Click Add
        iv. Choose DBISAM 4 ODBC Driver (Read-Only) <--- just running SELECT statements
        v. Data Source Name = TotalMD
        vi. Connection = Remote (Client/Server)
        vii. Host Name = 920-apps03, IP Address = 10.125.0.3, Port = 12005, Service = leave empty, Compression = None
        viii. accept defaults on next screen
        ix. user=Admin, pwd=DBAdmin, Enable Encryption=true, Encryption Password = don't change
        x. Database=Southeast Christian Counseling
        xi. accepts default on next 2 screens
        xii. Private Directory = c:\temp
        xiii. Table Password = leave empty
   D. Add dsn to web.ConnectionStrings.config
        i. <add name="TotalMDContext" connectionString="Dsn=TotalMD" />
