using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Rock.Web.UI;
using SharpScss;

namespace org.secc.Sass
{
    public static class ThemeExtensions
    {
        [DllImport( "kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true )]
        [return: MarshalAs( UnmanagedType.Bool )]
        static extern bool SetDllDirectory( string lpPathName );

        public static bool CompileSass( this RockTheme theme, out string messages )
        {
            messages = string.Empty;
            bool result = true;

            SetDllDirectory( System.Web.HttpContext.Current.Server.MapPath( "~/runtimes/win-x64/native" ) );

            try
            {
                DirectoryInfo themeDirectory = new DirectoryInfo( theme.AbsolutePath + @"\Styles" );
                if ( themeDirectory.Exists )
                {
                    List<FileInfo> files = GetSCSSFiles( themeDirectory );
                    
                    if ( files != null )
                    {
                        if ( theme.AllowsCompile )
                        {
                            // don't compile files that start with an underscore
                            foreach ( var file in files.Where( f => f.Name.EndsWith( ".scss" ) && !f.Name.StartsWith( "_" ) ) )
                            {
                                var content = File.ReadAllText( file.FullName );
                                var compact = Scss.ConvertToCss( content, new ScssOptions()
                                {
                                    InputFile = file.FullName,
                                    OutputFile = file.Name.Replace( ".scss", ".css" ), // Note: It will not generate the file, 
                                                             // only used for exception reporting
                                                             // includes and source maps
                                    GenerateSourceMap = false,
                                    
                                } );

                                File.WriteAllText( file.DirectoryName + @"\" + file.Name.Replace( ".scss", ".css" ), compact.Css );
                            }
                        }
                    }
                }
            }
            catch ( Exception ex )
            {
                result = false;
                messages = ex.Message;
            }
            return result;
        }

        private static List<FileInfo> GetSCSSFiles( DirectoryInfo directory )
        {
            List<FileInfo> files = directory.GetFiles().ToList();

            var childDirectories = directory.GetDirectories();
            foreach ( var childDirectory in childDirectories )
            {
                files.AddRange( GetSCSSFiles( childDirectory ) );
            }

            return files;
        }
    }
}
