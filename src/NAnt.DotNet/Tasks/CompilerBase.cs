// NAnt - A .NET build tool
// Copyright (C) 2001-2002 Gerry Shaw
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
// Gerry Shaw (gerry_shaw@yahoo.com)
// Mike Krueger (mike@icsharpcode.net)
// Ian MacLean (ian_maclean@another.com)

using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Tasks;
using NAnt.Core.Types;
using NAnt.Core.Util;
using NAnt.DotNet.Types;

namespace NAnt.DotNet.Tasks {
    /// <summary>
    /// Provides the abstract base class for compiler tasks.
    /// </summary>
    public abstract class CompilerBase : ExternalProgramBase {
        #region Private Instance Fields

        private string _responseFileName;
        private string _output = null;
        private string _target = null;
        private bool _debug = false;
        private string _define = null;
        private string _win32icon = null;
        private bool _warnAsError = false;
        private string _mainType = null;
        private FileSet _references = new FileSet();
        private FileSet _modules = new FileSet();
        private FileSet _sources = new FileSet();
        private ResourceFileSetCollection _resourcesList = new ResourceFileSetCollection();

        #endregion Private Instance Fields

        #region Protected Static Fields

        /// <summary>
        /// Contains a list of extensions for all file types that should be treated as
        /// 'code-behind' when looking for resources.  Ultimately this will determine
        /// if we use the "namespace+filename" or "namespace+classname" algorithm, since
        /// code-behind will use the "namespace+classname" algorithm.
        /// </summary>
        protected static string[] CodebehindExtensions = {".aspx", ".asax", ".ascx", ".asmx"};
        
        /// <summary>
        /// List of valid culture names for this platform
        /// </summary>
        protected static StringCollection cultureNames = new StringCollection();

        #region Class Constructor
        
        /// <summary>
        /// Class constructor for CompilerBase. It is called when the type is first loaded by the runtime
        /// </summary>
        static CompilerBase() {
            // fill the culture list
            foreach ( CultureInfo ci in CultureInfo.GetCultures( CultureTypes.AllCultures  ) ) {		       
                cultureNames.Add( ci.Name  );
            }
        }
        
        #endregion
        #endregion Protected Static Fields

    
        #region Public Instance Properties

        /// <summary>
        /// The name of the output file created by the compiler.
        /// </summary>
        [TaskAttribute("output", Required=true)]
        [StringValidator(AllowEmpty=false)]
        public string Output {
            get { return (_output != null) ? Project.GetFullPath(_output) : null; }
            set { _output = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// Output type. Possible values are <c>exe</c>, <c>winexe</c>,
        /// <c>library</c> or <c>module</c>.
        /// </summary>
        [TaskAttribute("target", Required=true)]
        [StringValidator(AllowEmpty=false)]
        public string OutputTarget  {
            get { return _target; }
            set { _target = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// Generate debug output. The default is <see langword="false" />.
        /// </summary>
        [TaskAttribute("debug")]
        [BooleanValidator()]
        public bool Debug {
            get { return _debug; }
            set { _debug = value; }
        }

        /// <summary>
        /// Define conditional compilation symbol(s).
        /// </summary>
        /// <remarks>
        /// <para>
        /// Corresponds to <c>/d[efine]:</c> flag.
        /// </para>
        /// </remarks>
        [TaskAttribute("define")]
        public string Define {
            get { return _define; }
            set { _define = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// Icon to associate with the application.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Corresponds to <c>/win32icon:</c> flag.
        /// </para>
        /// </remarks>
        [TaskAttribute("win32icon")]
        public string Win32Icon {
            get { return (_win32icon != null) ? Project.GetFullPath(_win32icon) : null; }
            set { _win32icon = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// Instructs the compiler to treat all warnings as errors. The default
        /// is <see langword="false" />.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Corresponds to the <c>/warnaserror[+|-]</c> flag of the compiler.
        /// </para>
        /// <para>
        /// When this property is set to <see langword="true" />, any messages
        /// that would ordinarily be reported as warnings will instead be
        /// reported as errors.
        /// </para>
        /// </remarks>
        [TaskAttribute("warnaserror")]
        [BooleanValidator()]
        public bool WarnAsError {
            get { return _warnAsError; }
            set { _warnAsError = value; }
        }

        /// <summary>
        /// Specifies which type contains the Main method that you want to use
        /// as the entry point into the program.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Corresponds to the <c>/m[ain]:</c> flag of the compiler.
        /// </para>
        /// <para>
        /// Use this property when creating an executable file. If this property
        /// is not set, the compiler searches for a valid Main method in all
        /// public classes.
        /// </para>
        /// </remarks>
        [TaskAttribute("main")]
        public string MainType {
            get { return _mainType; }
            set { _mainType = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// Reference metadata from the specified assembly files.
        /// </summary>
        [BuildElement("references")]
        public FileSet References {
            get { return _references; }
            set { _references = value; }
        }

        /// <summary>
        /// Resources to embed.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This can be a combination of resx files and file resources.
        /// </para>
        /// <para>
        /// .resx files will be compiled by <see cref="ResGenTask" /> and then
        /// embedded into the resulting executable.
        /// </para>
        /// <para>
        /// The <see cref="ResourceFileSet.Prefix" /> property is used to make
        /// up the resource name added to the assembly manifest for non-resx
        /// files.
        /// </para>
        /// <para>
        /// For .resx files the namespace from the matching source file is used
        /// as prefix. This matches the behaviour of Visual Studio.
        /// </para>
        /// <para>
        /// Multiple resources tags with different namespace prefixes may be
        /// specified.
        /// </para>
        /// </remarks>
        [BuildElementArray("resources")]
        public ResourceFileSetCollection ResourcesList {
            get { return _resourcesList; }
        }

        /// <summary>
        /// Link the specified modules into this assembly.
        /// </summary>
        [BuildElement("modules")]
        public FileSet Modules {
            get { return _modules; }
            set { _modules = value; }
        }

        /// <summary>
        /// The set of source files for compilation.
        /// </summary>
        [BuildElement("sources", Required=true)]
        public FileSet Sources {
            get { return _sources; }
            set { _sources = value; }
        }

        #endregion Public Instance Properties

        #region Protected Instance Properties

        /// <summary>
        /// Gets the file extension required by the current compiler.
        /// </summary>
        /// <value>
        /// The file extension required by the current compiler.
        /// </value>
        protected abstract string Extension {
            get;
        }
        /// <summary>
        /// Gets the class name regular expression for the language of the current compiler.
        /// </summary>
        /// <value> class name regular expression for the language of the current compiler</value>
        protected abstract Regex ClassNameRegex {
            get;
        }
        /// <summary>
        /// Gets the namespace regular expression for the language of the current compiler.
        /// </summary>
        /// <value> namespace regular expression for the language of the current compiler</value>
        protected abstract Regex NamespaceRegex {
            get;
        }

        #endregion Protected Instance Properties

        #region Override implementation of ExternalProgramBase


        /// <summary>
        /// Gets the command-line arguments for the external program.
        /// </summary>
        /// <value>
        /// The command-line arguments for the external program.
        /// </value>
        public override string ProgramArguments {
            get { return "@" + "\"" + _responseFileName + "\""; }
        }

        /// <summary>
        /// Compiles the sources and resources.
        /// </summary>
        protected override void ExecuteTask() {
            if (NeedsCompiling()) {
                // create temp response file to hold compiler options
                _responseFileName = Path.GetTempFileName();
                StreamWriter writer = new StreamWriter(_responseFileName);
                Hashtable cultureResources = new Hashtable();
                StringCollection compiledResourceFiles = new StringCollection();
                
                try {
                                        
                    if (References.BaseDirectory == null) {
                        References.BaseDirectory = BaseDirectory;
                    }
                    if (Modules.BaseDirectory == null) {
                        Modules.BaseDirectory = BaseDirectory;
                    }
                    if (Sources.BaseDirectory == null) {
                        Sources.BaseDirectory = BaseDirectory;
                    }

                    Log(Level.Info, LogPrefix + "Compiling {0} files to {1}.", Sources.FileNames.Count, Output);

                    // specific compiler options
                    WriteOptions(writer);

                    // suppresses display of the sign-on banner
                    WriteOption(writer, "nologo");

                    // specify output file format
                    WriteOption(writer, "target", OutputTarget);

                    if (Define != null) {
                        WriteOption(writer, "define", Define);
                    }

                    // the name of the output file
                    WriteOption(writer, "out", Output);

                    if (Win32Icon != null) {
                        WriteOption(writer, "win32icon", Win32Icon);
                    }

                    // Writes the option that specifies the class containing the Main method that should
                    // be called when the program starts.
                    if (this.MainType != null) {
                        WriteOption(writer, "main", this.MainType);
                    }

                    // Writes the option that specifies whether the compiler should consider warnings
                    // as errors.
                    if (this.WarnAsError) {
                        WriteOption(writer, "warnaserror");
                    }

                    // fix references to system assemblies
                    if (Project.CurrentFramework != null) {
                        foreach (string pattern in References.Includes) {
                            if (Path.GetFileName(pattern) == pattern) {
                                string frameworkDir = Project.CurrentFramework.FrameworkAssemblyDirectory.FullName;
                                string localPath = Path.Combine(References.BaseDirectory, pattern);
                                string fullPath = Path.Combine(frameworkDir, pattern);

                                if (!File.Exists(localPath) && File.Exists(fullPath)) {
                                    // found a system reference
                                    References.FileNames.Add(fullPath);
                                }
                            }
                        }
                    }

                    foreach (string fileName in References.FileNames) {
                        WriteOption(writer, "reference", fileName);
                    }

                    foreach (string fileName in Modules.FileNames) {
                        WriteOption(writer, "addmodule", fileName);
                    }

                    // compile resources
                    foreach (ResourceFileSet resources in ResourcesList) {

                        // Resx args
                        foreach (string fileName in resources.ResxFiles.FileNames) {
                            // try and get it from matching form
                            ResourceLinkage resourceLinkage = GetFormResourceLinkage(fileName);

                            if (resourceLinkage != null && !resourceLinkage.HasNamespaceName) {
                                resourceLinkage.NamespaceName = resources.Prefix;
                            }

                            string actualFileName = Path.GetFileNameWithoutExtension(fileName);                           
                            string manifestResourceName = Path.ChangeExtension(Path.GetFileName(fileName), ".resources" );

                            // cater for asax/aspx special cases ...
                            foreach (string extension in CodebehindExtensions){
                                if (manifestResourceName.IndexOf(extension) > -1) {
                                    manifestResourceName = manifestResourceName.Replace(extension, "");
                                    actualFileName = actualFileName.Replace(extension, "");
                                    break;
                                }
                            }
                            
                            if ( resourceLinkage != null && !resourceLinkage.HasClassName ) {
                                resourceLinkage.ClassName = actualFileName;
                            }

                            if (resourceLinkage != null && resourceLinkage.IsValid) {
                                manifestResourceName = manifestResourceName.Replace(actualFileName, resourceLinkage.ToString());
                            }

                            if (resourceLinkage == null) {
                                manifestResourceName = Path.ChangeExtension(resources.GetManifestResourceName(fileName), "resources");
                            }
                            
                            string tmpResourcePath = fileName.Replace( Path.GetFileName(fileName), manifestResourceName );                            
                            compiledResourceFiles.Add( tmpResourcePath );
                            
                            // compile to a temp .resources file
                            CompileResxResource( fileName, tmpResourcePath);
                            
                            // Check for internationalised resource files.
                            string culture = "";
                            if ( ContainsCulture( fileName, ref culture )) {
                                if (! cultureResources.ContainsKey( culture ) )  {
                                    cultureResources.Add( culture, new StringCollection() );
                                }
                                
                                // store resulting .resoures file for later linking 
                                ((StringCollection)cultureResources[culture]).Add( tmpResourcePath );
                            } else {
                                
                                // regular embedded resources
                                string resourceoption = tmpResourcePath + "," + manifestResourceName;

                                // write resource option to response file
                                WriteOption(writer, "resource", resourceoption );
                            }
                        }
                        
                        // create a localised resource dll for each culture name
                        foreach (string culture in cultureResources.Keys ) {
                
                            string culturedir =  Path.GetDirectoryName( Output )  + Path.DirectorySeparatorChar +  culture;
                            Directory.CreateDirectory( culturedir );
                            string outputFile =  Path.Combine( culturedir, Path.GetFileNameWithoutExtension(Output) + ".resources.dll");                            
                            LinkResourceAssembly( (StringCollection)cultureResources[culture],outputFile, culture );                                                       
                        }
                        
                        // other resources
                        foreach (string fileName in resources.NonResxFiles.FileNames) {
                            string resourceoption = fileName + "," + resources.GetManifestResourceName(fileName);
                            WriteOption(writer, "resource", resourceoption);
                        }
                    }

                    foreach (string fileName in Sources.FileNames) {
                        writer.WriteLine("\"" + fileName + "\"");
                    }

                    // Make sure to close the response file otherwise contents
                    // will not be written to disc and EXecuteTask() will fail.
                    writer.Close();

                    if (Verbose) {
                        // display response file contents
                        Log(Level.Info, LogPrefix + "Contents of {0}.", _responseFileName);
                        StreamReader reader = File.OpenText(_responseFileName);
                        Log(Level.Info, reader.ReadToEnd());
                        reader.Close();
                    }

                    // call base class to do the work
                    base.ExecuteTask();
                 
                } finally {
                    
                    // cleanup .resource files
                    foreach( string fileName in compiledResourceFiles ) {
                        File.Delete( fileName ); 
                    }
                    // make sure we delete response file even if an exception is thrown
                    writer.Close(); // make sure stream is closed or file cannot be deleted
                    File.Delete(_responseFileName);
                    _responseFileName = null;
                }
            }
        }

        #endregion Override implementation of ExternalProgramBase

        #region Protected Instance Methods

        /// <summary>
        /// Allows derived classes to provide compiler-specific options.
        /// </summary>
        protected virtual void WriteOptions(TextWriter writer) {
        }

        /// <summary>
        /// Writes an option using the default output format.
        /// </summary>
        protected virtual void WriteOption(TextWriter writer, string name) {
            writer.WriteLine("/{0}", name);
        }

        /// <summary>
        /// Writes an option and its value using the default output format.
        /// </summary>
        protected virtual void WriteOption(TextWriter writer, string name, string arg) {
            // Always quote arguments ( )
            writer.WriteLine("\"/{0}:{1}\"", name, arg);
        }

        /// <summary>
        /// Determines whether compilation is needed.
        /// </summary>
        protected virtual bool NeedsCompiling() {
            // return true as soon as we know we need to compile

            FileInfo outputFileInfo = new FileInfo(Output);
            if (!outputFileInfo.Exists) {
                return true;
            }

            //Sources Updated?
            string fileName = FileSet.FindMoreRecentLastWriteTime(Sources.FileNames, outputFileInfo.LastWriteTime);
            if (fileName != null) {
                Log(Level.Verbose, LogPrefix + "{0} is out of date, recompiling.", fileName);
                return true;
            }

            //References Updated?
            fileName = FileSet.FindMoreRecentLastWriteTime(References.FileNames, outputFileInfo.LastWriteTime);
            if (fileName != null) {
                Log(Level.Verbose, LogPrefix + "{0} is out of date, recompiling.", fileName);
                return true;
            }

            //Modules Updated?
            fileName = FileSet.FindMoreRecentLastWriteTime(Modules.FileNames, outputFileInfo.LastWriteTime);
            if (fileName != null) {
                Log(Level.Verbose, LogPrefix + "{0} is out of date, recompiling.", fileName);
                return true;
            }

            //Resources Updated?
            foreach (ResourceFileSet resources in ResourcesList) {
                fileName = FileSet.FindMoreRecentLastWriteTime(resources.FileNames, outputFileInfo.LastWriteTime);
                if (fileName != null) {
                    Log(Level.Verbose, LogPrefix + "{0} is out of date, recompiling.", fileName);
                    return true;
                }
            }

            // check the args for /res or /resource options.
            StringCollection resourceFileNames = new StringCollection();
            foreach (Argument argument in Arguments) {
                if (argument.IfDefined && !argument.UnlessDefined) {
                    string argumentValue = argument.Value;
                    if (argumentValue != null && (argumentValue.StartsWith("/res:") || argumentValue.StartsWith("/resource:"))) {
                        string path = argumentValue.Substring(argumentValue.IndexOf(':') + 1);
                        int indexOfComma = path.IndexOf(',');
                        if (indexOfComma != -1) {
                            path = path.Substring(0, indexOfComma);
                        }
                        resourceFileNames.Add(path);
                    }
                }
            }

            fileName = FileSet.FindMoreRecentLastWriteTime(resourceFileNames, outputFileInfo.LastWriteTime);
            if (fileName != null) {
                Log(Level.Verbose, LogPrefix + "{0} is out of date, recompiling.", fileName);
                return true;
            }

            // if we made it here then we don't have to recompile
            return false;
        }

        /// <summary>
        /// Determines if a given file is a localised resource file.
        /// </summary>
        /// <param name="resXFile">The resx file path to check for culture info</param>   
        /// <param name="foundCulture">The name of the culture that was located</param>
        /// <returns>true if we found a culture name otherwise false</returns>
        protected bool ContainsCulture( string resXFile, ref string foundCulture ) {
            
            string noextpath = Path.GetFileNameWithoutExtension( resXFile );
            int index = noextpath.LastIndexOf( '.' );
            if ( index >= 0 && index <= noextpath.Length ) {
                string possibleculture = noextpath.Substring( index +1, noextpath.Length - (index +1) );
                // check that its in our list of culture names
                if ( cultureNames.Contains(possibleculture) ) {
                    foundCulture = possibleculture;
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// An abstract method that must be overridden in each compiler.  It is 
        /// responable for extracting and returning the associated namespace/classname 
        /// linkage found in the given stream.
        /// </summary>
        /// <param name="sr">The read-only stream of the source file to search.</param>
        /// <returns>
        /// The namespace/classname of the source file matching the resource.
        /// </returns>
        public virtual ResourceLinkage PerformSearchForResourceLinkage(TextReader sr){
            Regex matchNamespaceRE = NamespaceRegex;  
            Regex matchClassNameRE = ClassNameRegex;
            
            string namespaceName  = "";
            string className = "";
    
            while (sr.Peek() > -1) {
                string str = sr.ReadLine();
                            
                Match matchNamespace = matchNamespaceRE.Match(str);
                if (matchNamespace.Success) {
                    Group group = matchNamespace.Groups["namespace"];
                    if (group.Success) {
                        foreach (Capture capture in group.Captures) {
                            namespaceName += (namespaceName.Length > 0 ? "." : "") + capture.Value;
                        }
                    }
                }

                Match matchClassName = matchClassNameRE.Match(str);
                if (matchClassName.Success) {
                    Group group = matchClassName.Groups["class"];
                    if (group.Success) {
                      className = group.Value;
                      break;
                    }
                }
            }
            return new ResourceLinkage(namespaceName, className);
        }

        /// <summary>
        /// Opens matching source file to find the correct namespace for the
        /// specified rsource file.
        /// </summary>
        /// <param name="resxPath"></param>
        /// <returns>
        /// The namespace/classname of the source file matching the resource or
        /// <see langword="null" /> if there's no matching source file.
        /// </returns>
        /// <remarks>
        /// This behaviour may be overidden by each particular compiler to 
        /// support the namespace/classname syntax for that language.
        /// </remarks>
        protected virtual ResourceLinkage GetFormResourceLinkage(string resxPath) {
            // open matching source file if it exists
            string sourceFile = resxPath.Replace("resx", Extension);
            
            // check if this is a localised resx file
            string culture = "";
            if ( ContainsCulture( sourceFile, ref culture )) {
                sourceFile = sourceFile.Replace( string.Format( ".{0}", culture  ), "" );
            }
            StreamReader sr = null;
            ResourceLinkage resourceLinkage  = null; 
  
            try {
                sr = File.OpenText(sourceFile);
                resourceLinkage = PerformSearchForResourceLinkage(sr);
            } catch (FileNotFoundException) { // if no matching file, dump out
                Log(Level.Debug, LogPrefix + "Did not find associated source file for resource {0}.", resxPath);
                return resourceLinkage;
            } finally {
                if (sr != null) {
                    sr.Close();
                }
            }

            // output some debug information about resource linkage found...
            if (resourceLinkage.IsValid) {
                Log(Level.Debug, LogPrefix + "Found resource linkage '{0}' for resource {1}.", resourceLinkage.ToString(), resxPath);
            } else {
                Log(Level.Debug, LogPrefix + "Could not find any resource linkage in matching source file for resource {0}.", resxPath);
            }

            return resourceLinkage;
        }
        
        /// <summary>
        /// Link a list of files into a resource assembly.
        /// </summary>
        /// <param name="resourceFiles">List of files to bind into</param>
        /// <param name="outputFile">Resource assembly to generate</param>
        /// <param name="culture">Culture of the generated assembly.</param>
        protected void LinkResourceAssembly( StringCollection resourceFiles, string outputFile, string culture ){
            
            // defer to the assembly linker task
            AssemblyLinkerTask alink = new AssemblyLinkerTask();
            alink.Project = this.Project;
            alink.Parent = this.Parent;
            alink.InitializeTaskConfiguration();

            alink.Output = outputFile;
            alink.Culture = culture;
            alink.OutputTarget = "lib";
            
            foreach( string resource in resourceFiles ) {
                alink.Sources.FileNames.Add( resource );
            }
            
            // Fix up the indent level
            Project.Indent();
            
            // execute the nested task
            alink.Execute();
            Project.Unindent();
        }
        
        /// <summary>
        /// Compiles a resx files to a .resources file.
        /// </summary>
        /// <param name="inputFile"></param>
        /// <param name="outputFile"></param>
        protected void CompileResxResource(string inputFile, string outputFile )  {
            ResGenTask resgen = new ResGenTask();
            resgen.Project = this.Project;
            resgen.Parent = this.Parent;

            // temporary hack to force configuration settings to be
            // read from NAnt configuration file
            //
            // TO-DO : Remove this temporary hack when a permanent solution is
            // available for loading the default values from the configuration
            // file if a build element is constructed from code.
            resgen.InitializeTaskConfiguration();
           
            // inherit Verbose setting from current task
            resgen.Verbose = this.Verbose;

            resgen.Input = inputFile;
            resgen.Output = Path.GetFileName(outputFile);
            resgen.ToDirectory = Path.GetDirectoryName(outputFile);
            resgen.BaseDirectory = Path.GetDirectoryName(inputFile);

            // Fix up the indent level --
            Project.Indent();
            resgen.Execute();
            Project.Unindent();
        }
        
        #endregion Protected Instance Methods

        /// <summary>
        /// Holds class and namespace information for resource (*.resx) linkage.
        /// </summary>
        public class ResourceLinkage {
            #region Private Instance Fields
            
            private string _namespaceName;
            private string _className;

            #endregion Private Instance Fields

            #region Public Instance Constructors

            /// <summary>
            /// Initializes a new instance of the <see cref="ResourceLinkage" />
            /// class.
            /// </summary>
            /// <param name="namespaceName">The namespace the resource is under.</param>
            /// <param name="className">The class name the resource is associated with.</param>
            public ResourceLinkage(string namespaceName, string className) {
                _namespaceName = namespaceName;
                _className     = className;
            }

            #endregion Public Instance Constructors

            #region Override implementation of Object
  
            /// <summary>
            /// Returns the resource linkage as a string.
            /// </summary>
            /// <returns>
            /// A string representation of the resource linkage.
            /// </returns>
            public override string ToString() {
                if (!IsValid) {
                    return string.Empty;
                }
                if (HasNamespaceName && HasClassName) {
                    return NamespaceName + "." + ClassName;
                }
                if (HasNamespaceName) { 
                    return NamespaceName;
                }    
                return ClassName;
            }

            #endregion Override implementation of Object

            #region Public Instance Properties
  
            /// <summary>
            /// Gets a value indicating whether the <see cref="ResourceLinkage" />
            /// instances contains valid data.
            /// </summary>
            /// <value>
            /// <see langword="true" /> if the <see cref="ResourceLinkage" />
            /// instance contains valid data; otherwise, <see langword="false" />.
            /// </value>
            public bool IsValid {
                get { return !StringUtils.IsNullOrEmpty(_namespaceName) || !StringUtils.IsNullOrEmpty(_className); }
            }
  
            /// <summary>
            /// Gets a value indicating whether a namespace name is available
            /// for this <see cref="ResourceLinkage" /> instance.
            /// </summary>
            /// <value>
            /// <see langword="true" /> if a namespace name is available for 
            /// this <see cref="ResourceLinkage" /> instance; otherwise, 
            /// <see langword="false" />.
            /// </value>
            public bool HasNamespaceName {
                get { return !StringUtils.IsNullOrEmpty(_namespaceName); }
            }
  
            /// <summary>
            /// Gets a value indicating whether a class name is available
            /// for this <see cref="ResourceLinkage" /> instance.
            /// </summary>
            /// <value>
            /// <see langword="true" /> if a class name is available for 
            /// this <see cref="ResourceLinkage" /> instance; otherwise, 
            /// <see langword="false" />.
            /// </value>
            public bool HasClassName {
                get { return !StringUtils.IsNullOrEmpty(_className); }
            }
  
            /// <summary>
            /// Gets the name of namespace the resource is under.  
            /// </summary>
            /// <value>
            /// The name of namespace the resource is under.  
            /// </value>
            public string NamespaceName  {
                get { return _namespaceName; }
                set { _namespaceName = (value != null) ? value.Trim() : null; }
            }
  
            /// <summary>
            /// Gets the name of the class (most likely a form) that the resource 
            /// is associated with.  
            /// </summary>
            /// <value>
            /// The name of the class the resource is associated with.  
            /// </value>
            public string ClassName {
                get { return _className; }
                set { _className = (value != null) ? value.Trim() : null; }
            }

            #endregion Public Instance Properties
        }
    }
}
