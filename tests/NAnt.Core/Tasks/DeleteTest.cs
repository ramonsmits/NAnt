// NAnt - A .NET build tool
// Copyright (C) 2002 Scott Hernandez (ScottHernandez@hotmail.com)
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

// Scott Hernandez (ScottHernandez@hotmail.com)

using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Globalization;

using NUnit.Framework;

namespace Tests.NAnt.Core.Tasks {
    /// <summary>
    /// <para>Tests the deletion of the following:</para>
    /// <para>
    ///     <list type="test">
    ///         <item> file</item>
    ///         <item> folder</item>
    ///         <item> folder with a file (recursive)</item>
    ///     </list>
    /// </para>
    /// </summary>
    /// <remarks>This test should also test for failures, like permission errors, and filesets</remarks>
    [TestFixture]
    public class DeleteTest : BuildTestBase {
        const string _xmlProjectTemplate = @"
            <project>
                <delete verbose='true' {0}='{1}'/>
            </project>";
        const string _xmlProjectTemplate2 = @"
            <project>
                <delete verbose='true'>
                    <fileset>
                        <include name='{0}' />
                    </fileset>
                </delete>
            </project>";
        
        string tempFile1, tempFile2, tempFile3, tempFile4, tempFile5, tempFile6, tempFile7;
        string tempDir1, tempDir2, tempDir3, tempDir4;

        /// <summary>
        /// Creates a structure like so:
        /// a.b\
        ///     a.bb
        ///     a.bc
        ///     foo\*
        ///         x.x
        ///     goo\*
        ///         x\
        ///             y.y
        ///         ha.he
        ///         ha.he2*
        ///         ha.he3*
        /// </summary>
        [SetUp]
        protected override void SetUp() {
            base.SetUp();

            tempDir1 = CreateTempDir("a.b");
            tempDir2 = CreateTempDir(Path.Combine(tempDir1, "foo"));
            tempDir3 = CreateTempDir(Path.Combine(tempDir1, "goo"));
            tempDir4 = CreateTempDir(Path.Combine(tempDir1, Path.Combine(tempDir3, "x")));

            tempFile1 = CreateTempFile(Path.Combine(tempDir1, "a.bb"));
            tempFile2 = CreateTempFile(Path.Combine(tempDir1, "a.bc"));
            tempFile3 = CreateTempFile(Path.Combine(tempDir2, "x.x"));
            tempFile4 = CreateTempFile(Path.Combine(tempDir4, "y.y"));
            tempFile5 = CreateTempFile(Path.Combine(tempDir3, "ha.he"));
            tempFile6 = CreateTempFile(Path.Combine(tempDir3, "ha.he2"));
            tempFile7 = CreateTempFile(Path.Combine(tempDir3, "ha.he3"));

            /*
            File.SetAttributes(tempDir2, FileAttributes.ReadOnly);
            File.SetAttributes(tempDir3, FileAttributes.ReadOnly); 
            */
            File.SetAttributes(Path.Combine(tempDir3, "ha.he3"), FileAttributes.ReadOnly);
            File.SetAttributes(Path.Combine(tempDir3, "ha.he2"), FileAttributes.ReadOnly);
        }

        [Test]
        public void Test_Delete() {
            string result;

            Assert.IsTrue(File.Exists(tempFile1), "File should have been created:" + tempFile1);
            Assert.IsTrue(File.Exists(tempFile2), "File should have been created:" + tempFile2);
            Assert.IsTrue(File.Exists(tempFile3), "File should have been created:" + tempFile3);
            Assert.IsTrue(File.Exists(tempFile4), "File should have been created:" + tempFile4);
            Assert.IsTrue(File.Exists(tempFile5), "File should have been created:" + tempFile5);
            Assert.IsTrue(File.Exists(tempFile6), "File should have been created:" + tempFile6);
            Assert.IsTrue(File.Exists(tempFile7), "File should have been created:" + tempFile7);

            Assert.IsTrue(Directory.Exists(tempDir1), "Dir should have been created:" + tempDir1);
            Assert.IsTrue(Directory.Exists(tempDir2), "Dir should have been created:" + tempDir2);
            Assert.IsTrue(Directory.Exists(tempDir3), "Dir should have been created:" + tempDir3);
            Assert.IsTrue(Directory.Exists(tempDir4), "Dir should have been created:" + tempDir4);

            result = RunBuild(String.Format(CultureInfo.InvariantCulture, _xmlProjectTemplate, "file", tempFile6 ));
            
            Assert.IsTrue(File.Exists(tempFile1), "File should not have been deleted:" + tempFile1);
            Assert.IsTrue(File.Exists(tempFile2), "File should not have been deleted:" + tempFile2);
            Assert.IsTrue(File.Exists(tempFile3), "File should not have been deleted:" + tempFile3);
            Assert.IsTrue(File.Exists(tempFile4), "File should not have been deleted:" + tempFile4);
            Assert.IsTrue(File.Exists(tempFile5), "File should not have been deleted:" + tempFile5);
            Assert.IsFalse(File.Exists(tempFile6), "File should have been deleted:" + tempFile6);
            Assert.IsTrue(File.Exists(tempFile7), "File should not have been deleted:" + tempFile7);

            Assert.IsTrue(Directory.Exists(tempDir1), "Dir should not have been deleted:" + tempDir1);
            Assert.IsTrue(Directory.Exists(tempDir2), "Dir should not have been deleted:" + tempDir2);
            Assert.IsTrue(Directory.Exists(tempDir3), "Dir should not have been deleted:" + tempDir3);
            Assert.IsTrue(Directory.Exists(tempDir4), "Dir should not have been deleted:" + tempDir4);

            result = RunBuild(String.Format(CultureInfo.InvariantCulture, _xmlProjectTemplate, "dir", tempDir2 ));

            Assert.IsTrue(File.Exists(tempFile1), "File should not have been deleted:" + tempFile1);
            Assert.IsTrue(File.Exists(tempFile2), "File should not have been deleted:" + tempFile2);
            Assert.IsFalse(File.Exists(tempFile3), "File should have been deleted:" + tempFile3);
            Assert.IsTrue(File.Exists(tempFile4), "File should not have been deleted:" + tempFile4);
            Assert.IsTrue(File.Exists(tempFile5), "File should not have been deleted:" + tempFile5);
            Assert.IsFalse(File.Exists(tempFile6), "File should have been deleted:" + tempFile6);
            Assert.IsTrue(File.Exists(tempFile7), "File should not have been deleted:" + tempFile7);

            Assert.IsTrue(Directory.Exists(tempDir1), "Dir should not have been deleted:" + tempDir1);
            Assert.IsFalse(Directory.Exists(tempDir2), "Dir should have been deleted:" + tempDir2);
            Assert.IsTrue(Directory.Exists(tempDir3), "Dir should not have been deleted:" + tempDir3);
            Assert.IsTrue(Directory.Exists(tempDir4), "Dir should not have been deleted:" + tempDir4);

            result = RunBuild(String.Format(CultureInfo.InvariantCulture, _xmlProjectTemplate, "file", tempFile1 ));

            Assert.IsFalse(File.Exists(tempFile1), "File should have been deleted:" + tempFile1);
            Assert.IsTrue(File.Exists(tempFile2), "File should not have been deleted:" + tempFile2);
            Assert.IsFalse(File.Exists(tempFile3), "File should have been deleted:" + tempFile3);
            Assert.IsTrue(File.Exists(tempFile4), "File should not have been deleted:" + tempFile4);
            Assert.IsTrue(File.Exists(tempFile5), "File should not have been deleted:" + tempFile5);
            Assert.IsFalse(File.Exists(tempFile6), "File should have been deleted:" + tempFile6);
            Assert.IsTrue(File.Exists(tempFile7), "File should not have been deleted:" + tempFile7);

            Assert.IsTrue(Directory.Exists(tempDir1), "Dir should not have been deleted:" + tempDir1);
            Assert.IsFalse(Directory.Exists(tempDir2), "Dir should have been deleted:" + tempDir2);
            Assert.IsTrue(Directory.Exists(tempDir3), "Dir should not have been deleted:" + tempDir3);
            Assert.IsTrue(Directory.Exists(tempDir4), "Dir should not have been deleted:" + tempDir4);

            result = RunBuild(String.Format(CultureInfo.InvariantCulture, _xmlProjectTemplate2, tempDir1 ));

            Assert.IsFalse(File.Exists(tempFile1), "File should have been deleted:" + tempFile1);
            Assert.IsFalse(File.Exists(tempFile2), "File should have been deleted:" + tempFile2);
            Assert.IsFalse(File.Exists(tempFile3), "File should have been deleted:" + tempFile3);
            Assert.IsFalse(File.Exists(tempFile4), "File should have been deleted:" + tempFile4);
            Assert.IsFalse(File.Exists(tempFile5), "File should have been deleted:" + tempFile5);
            Assert.IsFalse(File.Exists(tempFile6), "File should have been deleted:" + tempFile6);
            Assert.IsFalse(File.Exists(tempFile7), "File should have been deleted:" + tempFile7);

            Assert.IsFalse(Directory.Exists(tempDir1), "Dir should have been deleted:" + tempDir1);
            Assert.IsFalse(Directory.Exists(tempDir2), "Dir should have been deleted:" + tempDir2);
            Assert.IsFalse(Directory.Exists(tempDir3), "Dir should have been deleted:" + tempDir3);
            Assert.IsFalse(Directory.Exists(tempDir4), "Dir should have been deleted:" + tempDir4);
        }
    }
}
