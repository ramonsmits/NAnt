// NAnt - A .NET build tool
// Copyright (C) 2002-2003 Scott Hernandez
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
//

using System;
using System.Text;
using NAnt.Core.Filters;
using NUnit.Framework;

namespace Tests.NAnt.Core.Filters {
    /// <summary>
    /// Tests the ReplaceTokens class.
    /// </summary>
    [TestFixture]
    public class ReplaceTokensTest : FilterTestBase {
        const string _tagName = "replacetokens";

        [Test]
        public void InstantiationTest() {
            base.TestFilter(@"<" + _tagName + @" endtoken=""@"" order=""0""><token key=""FALA"" value=""falalalalalalalala"" /></" + _tagName + @">", " ", " ");
        }

        [Test]
        public void EmptyFileTest() {
            base.TestFilter(@"<" + _tagName + @" endtoken=""@"" order=""0""><token key=""FALA"" value=""falalalalalalalala"" /></" + _tagName + @">", "", "");
        }

        /// <summary>
        /// Test if two tokens are next to each other
        /// </summary>
        [Test]
        public void ExtraTokenTest() {

            string prologueXml = null;

            string filterXml = @"<" + _tagName + @" endtoken=""@"" order=""0"">
                               <token key=""OLD"" value=""NEW""/>
                               </" + _tagName + @">";

            string input = @"@@OLD@";
            string expectedOutput = @"@NEW";
            base.TestFilter(filterXml, input, expectedOutput, prologueXml);
        }

        /// <summary>
        /// Test if two tokens are next to each other
        /// </summary>
        [Test]
        public void ExtraTokenTesta() {
            string prologueXml = null;

            string filterXml = @"<" + _tagName + @" endtoken=""@"" order=""0"">
                               <token key=""OLD"" value=""NEW""/>
                               </" + _tagName + @">";

            string input = @"@OLD@@";
            string expectedOutput = @"NEW@";
            base.TestFilter(filterXml, input, expectedOutput, prologueXml);
        }

        /// <summary>
        /// Test with filter tag
        /// </summary>
        [Test]
        public void TestFilterTag() {
            string prologueXml = null;

            string filterXml = @"<filter assembly=""NAnt.Core"" class=""NAnt.Core.Filters.ReplaceTokens"" order=""0"">
                               <param name=""endtoken"" value=""^""/>
                               <param name=""OLD"" value=""NEW""/>
                               </filter>";

            string input = @"@OLD^@";
            string expectedOutput = @"NEW@";
            base.TestFilter(filterXml, input, expectedOutput, prologueXml);
        }


        /// <summary>
        /// Test if two tokens are next to each other with different beginning and ending tokens
        /// </summary>
        [Test]
        public void ExtraTokenTestDiffToc() {
            //Token on left
            string prologueXml = null;

            string filterXml = @"<" + _tagName + @" endtoken=""^"" order=""0"">
                               <token key=""OLD"" value=""NEW""/>
                               </" + _tagName + @">";

            string input = @"@@OLD^";
            string expectedOutput = @"@NEW";
            base.TestFilter(filterXml, input, expectedOutput, prologueXml);
        }

        /// <summary>
        /// Test if two tokens are next to each other with different beginning and ending tokens
        /// </summary>
        [Test]
        public void ExtraTokenTestDiffToca() {
            //Token on right
            string prologueXml = null;

            string filterXml = @"<" + _tagName + @" endtoken=""^"" order=""0"">
                               <token key=""OLD"" value=""NEW""/>
                               </" + _tagName + @">";

            string input = @"@OLD^^";
            string expectedOutput = @"NEW^";
            base.TestFilter(filterXml, input, expectedOutput, prologueXml);
        }

        /// <summary>
        /// Test with the same beginning and ending tokens
        /// </summary>
        [Test]
        public void ComplexTest() {
            string prologueXml = @"<property name=""DATE"" value=""13 May 2004"" />";

            string filterXml = @"<" + _tagName + @" endtoken=""@"" order=""0"">
                               <token key=""DATE"" value=""${DATE}""/>
                               <token key=""INNER_TEST"" value=""--$$--""/>
                               <token key=""EOF"" value=""End of file..""/>
                               </" + _tagName + @">";

            string input = @"public class ProjectName {
    static void Main() {

                        /*
                        @@ @@ @@ @@@ @@@@ @@@@@ @@@@@ @@@@ @DATE@
                        
                        @@@@@@@@@@@@@@@@@@@@@DATE@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
                        @@@@@@@@@@@@@@@@@@@@@DATE @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
                        
                        ^^^^^^^^^^^^^^^^^^^^@DATE^@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
                        
                        @@@@@@@@@@@@@@@@@@@@@DATE^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
                        @@@@@@@@@@@@@@@@@@@^@DATE^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
                        
                        @@@DATE@@DATE@@DATE@@DATE@@DATE@@DATE@@DATE@   @ @ @ @DATE@@DATE@@DATE@@DATE@@DATE@@DATE@@DATE@@DATE@@DATE@@DATE@
                        
                        @@@DATE^@DATE^^DATE@@DATE@^DATE@@DATE^@DATE@   @ @ @ #@DATE^@DATE@^DATE@^DATE^@DATE@@DATE@@DATE@@DATE@@DATE@@DATE@
                        
                        @INNER_@GOO@^
                        
                        @GOO@
                        */
                        
                        
                        System.Console.WriteLine(""Hello World using C# ~@DATE@~ "");
                        System.Console.WriteLine(""Hello World using C# ~@INNER_@GOO@@~ "");
                        System.Console.WriteLine(""Hello World using C# ~@INNER_@GOO@@~ "");
                        }
                      }//@EOF@";
            string expectedOutput = @"public class ProjectName {
    static void Main() {

                        /*
                        @@ @@ @@ @@@ @@@@ @@@@@ @@@@@ @@@@ 13 May 2004
                        
                        @@@@@@@@@@@@@@@@@@@@13 May 2004@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
                        @@@@@@@@@@@@@@@@@@@@@DATE @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
                        
                        ^^^^^^^^^^^^^^^^^^^^@DATE^@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
                        
                        @@@@@@@@@@@@@@@@@@@@@DATE^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
                        @@@@@@@@@@@@@@@@@@@^@DATE^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
                        
                        @@13 May 200413 May 200413 May 200413 May 200413 May 200413 May 200413 May 2004   @ @ @ 13 May 200413 May 200413 May 200413 May 200413 May 200413 May 200413 May 200413 May 200413 May 200413 May 2004
                        
                        @@@DATE^@DATE^^DATE@13 May 2004^DATE@@DATE^13 May 2004   @ @ @ #@DATE^13 May 2004^DATE@^DATE^13 May 200413 May 200413 May 200413 May 200413 May 200413 May 2004
                        
                        @INNER_@GOO@^
                        
                        @GOO@
                        */
                        
                        
                        System.Console.WriteLine(""Hello World using C# ~13 May 2004~ "");
                        System.Console.WriteLine(""Hello World using C# ~@INNER_@GOO@@~ "");
                        System.Console.WriteLine(""Hello World using C# ~@INNER_@GOO@@~ "");
                        }
                      }//End of file..";

            base.TestFilter(filterXml, input, expectedOutput, prologueXml);
        }

        /// <summary>
        /// Test with different beginning and ending tokens
        /// </summary>
        [Test]
        public void ComplexTest1() {
            string prologueXml = @"<property name=""DATE"" value=""13 May 2004"" />";

            string filterXml = @"<" + _tagName + @" endtoken=""^"" order=""0"">
                               <token key=""DATE"" value=""${DATE}""/>
                               <token key=""INNER_TEST"" value=""--$$--""/>
                               <token key=""EOF"" value=""End of file..""/>
                               </" + _tagName + @">";

            string input = @"public class ProjectName {
    static void Main() {
                            /*
                            
                            @@ @@ @@ @@@ @@@@ @@@@@ @@@@@ @@@@ @DATE@
                            
                            @@@@@@@@@@@@@@@@@@@@@DATE@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
                            @@@@@@@@@@@@@@@@@@@@@DATE @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
                            
                            ^^^^^^^^^^^^^^^^^^^^@DATE^@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
                            
                            @@@@@@@@@@@@@@@@@@@@@DATE^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
                            @@@@@@@@@@@@@@@@@@@^@DATE^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
                            
                            @@@DATE@@DATE@@DATE@@DATE@@DATE@@DATE@@DATE@   @ @ @ @DATE@@DATE@@DATE@@DATE@@DATE@@DATE@@DATE@@DATE@@DATE@@DATE@
                            
                            @@@DATE^@DATE^^DATE@@DATE@^DATE@@DATE^@DATE@   @ @ @ #@DATE^@DATE@^DATE@^DATE^@DATE@@DATE@@DATE@@DATE@@DATE@@DATE@
                            
                            @INNER_@GOO@^
                            
                            @GOO@
                            */
                            
                            
                            System.Console.WriteLine(""Hello World using C# ~@DATE@~ "");
                            System.Console.WriteLine(""Hello World using C# ~@INNER_@GOO@@~ "");
                            System.Console.WriteLine(""Hello World using C# ~@INNER_@GOO@@~ "");
                        }
                    }//@EOF@";
            string expectedOutput = @"public class ProjectName {
    static void Main() {
                            /*
                            
                            @@ @@ @@ @@@ @@@@ @@@@@ @@@@@ @@@@ @DATE@
                            
                            @@@@@@@@@@@@@@@@@@@@@DATE@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
                            @@@@@@@@@@@@@@@@@@@@@DATE @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
                            
                            ^^^^^^^^^^^^^^^^^^^^13 May 2004@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
                            
                            @@@@@@@@@@@@@@@@@@@@13 May 2004^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
                            @@@@@@@@@@@@@@@@@@@^13 May 2004^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
                            
                            @@@DATE@@DATE@@DATE@@DATE@@DATE@@DATE@@DATE@   @ @ @ @DATE@@DATE@@DATE@@DATE@@DATE@@DATE@@DATE@@DATE@@DATE@@DATE@
                            
                            @@13 May 200413 May 2004^DATE@@DATE@^DATE@13 May 2004@DATE@   @ @ @ #13 May 2004@DATE@^DATE@^DATE^@DATE@@DATE@@DATE@@DATE@@DATE@@DATE@
                            
                            @INNER_@GOO@^
                            
                            @GOO@
                            */
                            
                            
                            System.Console.WriteLine(""Hello World using C# ~@DATE@~ "");
                            System.Console.WriteLine(""Hello World using C# ~@INNER_@GOO@@~ "");
                            System.Console.WriteLine(""Hello World using C# ~@INNER_@GOO@@~ "");
                        }
                    }//@EOF@";

            base.TestFilter(filterXml, input, expectedOutput, prologueXml);
        }
    }
}
