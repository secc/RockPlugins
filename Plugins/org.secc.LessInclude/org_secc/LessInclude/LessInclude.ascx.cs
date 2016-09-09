using System;
using System.ComponentModel;
using System.Linq;
using System.IO;
using System.Web.UI;
using System.Web.UI.WebControls;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;
using System.Collections.Generic;
using System.Web;
using System.Web.UI.HtmlControls;
using dotless.Core;
using dotless.Core.configuration;
using System.Text;

namespace RockWeb.Plugins.org_secc.LessInclude
{
    [DisplayName("Less Include")]
    [Category("CMS")]
    [Description("Block to include custom CSS via LESS to current page.")]

    // Custom Settings
    [CodeEditorField("Variables", "LESS Variables", CodeEditorMode.Less,CodeEditorTheme.Rock,400,false,"", "CustomSetting")]
    [CodeEditorField("Overrides", "LESS Overrides", CodeEditorMode.Less, CodeEditorTheme.Rock, 400, false, "", "CustomSetting")]
    [CodeEditorField("Theme", "LESS Theme", CodeEditorMode.Less, CodeEditorTheme.Rock, 400, false, "", "CustomSetting")]
    [CodeEditorField("Print", "LESS Print Theme", CodeEditorMode.Less, CodeEditorTheme.Rock, 400, false, "", "CustomSetting")]


    public partial class LessInclude : RockBlockCustomSettings
    {
        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            //Remove fallback css
            var ctrl = (ContentPlaceHolder)Page.Header.FindControl("CSSDefault");
            if (ctrl != null)
                ctrl.Visible = false;

            //Load our new css
            string bootstrapFileLocation = GetFileLocation("bootstrap.css");

            var bootstrap = new HtmlLink();
            bootstrap.Href = ResolveRockUrl(bootstrapFileLocation);
            bootstrap.Attributes.Add("rel", "stylesheet");
            bootstrap.Attributes.Add("type", "text/css");
            Page.Header.Controls.Add(bootstrap);

            string themeFileLocation = GetFileLocation("theme.css");

            var theme = new HtmlLink();
            theme.Href = ResolveRockUrl(themeFileLocation);
            theme.Attributes.Add("rel", "stylesheet");
            theme.Attributes.Add("type", "text/css");
            Page.Header.Controls.Add(theme);
        }

        private string GetFileLocation(string fileName)
        {
            string blockId = BlockId.ToString();

            var cache = (string) GetCacheItem(blockId + fileName);

            if (cache != null)
            {
                return cache;
            }

            //Set CWD
            string styleLocation = "~/Themes/" + base.RockPage.Site.Theme + "/Styles/";
            Environment.CurrentDirectory = Server.MapPath(styleLocation);

            string path = blockId + "/";

            string fileLocation;

            if (File.Exists(path + fileName))
            {
                fileLocation = "~~/Styles/" + blockId + "/" + fileName;
                
            }
            else
            {
                fileLocation = "~~/Styles/" + fileName;
            }
            AddCacheItem(blockId + fileName, fileLocation);
            return fileLocation;
        }

        protected override void ShowSettings()
        {
            pnlEditModal.Visible = true;
            upnlContent.Update();
            mdEdit.Show();

            ceVariables.Text = GetPageFile("_variables.less");
            ceOverrides.Text = GetPageFile("_variable-overrides.less");
            ceTheme.Text = GetPageFile("theme.less");
            cePrint.Text = GetPageFile("_print.less");

        }

        private string GetPageFile(string fileName)
        {
            string lessLocation = "~/Themes/" + base.RockPage.Site.Theme + "/Styles/";
            Environment.CurrentDirectory = Server.MapPath(lessLocation);

            string blockId = BlockId.ToString();
            string path = blockId + "/";
            
            if (File.Exists(path + fileName))
            {
                using (var fs = File.OpenText(path + fileName))
                {
                    return fs.ReadToEnd();
                }
            }
            else
            {
                using (var fs = File.OpenText(fileName))
                {
                    return fs.ReadToEnd();
                }
            }
        }

        private void ProcessLess()
        {
            string blockId = BlockId.ToString();

            string lessLocation = "~/Themes/" + base.RockPage.Site.Theme + "/Styles/"+ blockId + "/";
            Environment.CurrentDirectory = Server.MapPath(lessLocation);

            string bootstrapLocation = lessLocation + "bootstrap.less";
            var bootstrapMapped =  Server.MapPath(bootstrapLocation);
            var bootstrapFile = File.ReadAllText(bootstrapMapped);
            var bootstrapCSS = Less.Parse(bootstrapFile);

            SavePageFile(bootstrapCSS, "bootstrap.css");

            string themeLocation = lessLocation + "theme.less";
            var themeMapped = Server.MapPath(themeLocation);
            var themeFile = File.ReadAllText(themeMapped);
            var themeCSS = Less.Parse(themeFile);

            SavePageFile(themeCSS, "theme.css");

            FlushCacheItem(blockId + "bootstrap.css");
            FlushCacheItem(blockId + "theme.css");
        }
        
        private void SavePageFile(string content, string fileName)
        {
            string lessLocation = "~/Themes/" + base.RockPage.Site.Theme + "/Styles/";
            Environment.CurrentDirectory = Server.MapPath(lessLocation);

            string blockId = BlockId.ToString();
            string path = blockId + "/";

            //If a directory for this block does not exist
            //Create the directory and copy the custom bootsrap
            //and template less files to the folder
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                File.Copy("themeCustom.less", path + "theme.less");
                File.Copy("bootstrapCustom.less", path + "bootstrap.less");
            }

                
            // Truncate the file if it exists.
            if (File.Exists(path + fileName))
            {
                using (FileStream fs = File.Open(path + fileName, FileMode.Truncate))
                {
                    Byte[] info = new UTF8Encoding(true).GetBytes(content);
                    // Add some information to the file.
                    fs.Write(info, 0, info.Length);
                }
            }
            else
            {
                // Create the file.
                using (FileStream fs = File.Create(path + fileName))
                {
                    Byte[] info = new UTF8Encoding(true).GetBytes(content);
                    // Add some information to the file.
                    fs.Write(info, 0, info.Length);
                }
            }
        }

        protected void mdEdit_SaveClick(object sender, EventArgs e)
        {
            SavePageFile (ceVariables.Text, "_variables.less");
            SavePageFile(ceOverrides.Text, "_variable-overrides.less");
            SavePageFile(ceTheme.Text, "theme.less");
            SavePageFile(cePrint.Text, "_print.less");

            ProcessLess();

            mdEdit.Hide();
        }
    }
}