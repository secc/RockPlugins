using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using dotless.Core.configuration;
using LibSass.Compiler;
using LibSass.Compiler.Options;
using Rock.Web.UI;

namespace org.secc.Sass
{
    public static class ThemeExtensions
    {
        public static bool CompileSass( this RockTheme theme, out string messages )
        {
            messages = string.Empty;
            bool result = true;

            try
            {
                DirectoryInfo themeDirectory = new DirectoryInfo( theme.AbsolutePath + @"\Styles" );
                if ( themeDirectory.Exists )
                {
                    List<FileInfo> files = GetSCSSFiles( themeDirectory );

                    DotlessConfiguration dotLessConfiguration = new DotlessConfiguration();
                    dotLessConfiguration.MinifyOutput = true;

                    Directory.SetCurrentDirectory( themeDirectory.FullName );

                    if ( files != null )
                    {
                        if ( theme.AllowsCompile )
                        {
                            // don't compile files that start with an underscore
                            foreach ( var file in files.Where( f => f.Name.EndsWith( ".scss" ) && !f.Name.StartsWith( "_" ) ) )
                            {
                                var content = File.ReadAllText( file.FullName );
                                var sassCompiler = new SassCompiler( new SassOptions() { Data = content, IncludePath = file.DirectoryName } );
                                var compact = sassCompiler.Compile();
                                File.WriteAllText( file.DirectoryName + @"\" + file.Name.Replace( ".scss", ".css" ), compact.Output );
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
