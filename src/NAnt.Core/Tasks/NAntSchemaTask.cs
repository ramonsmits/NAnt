// NAnt - A .NET build tool
// Copyright (C) 2001-2003 Scott Hernandez
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
// Scott Hernandez (ScottHernandez@hotmail.com)
// Jaroslaw Kowalski (jkowalski@users.sourceforge.net)

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security.Permissions;
using System.Xml;
using System.Xml.Schema;

using NAnt.Core.Attributes;
using NAnt.Core.Util;

namespace NAnt.Core.Tasks {
    /// <summary>
    /// Creates an XSD File for all available tasks.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///   This can be used in conjuntion with the command-line option to do XSD 
    ///   Schema validation on the build file.
    ///   </para>
    /// </remarks>
    /// <example>
    ///   <para>Creates a <c>NAnt.xsd</c> file in the current project directory.</para>
    ///   <code>
    ///     <![CDATA[
    /// <nantschema output="NAnt.xsd" />
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("nantschema")]
    public class NAntSchemaTask : Task {
        #region Private Instance Fields

        private FileInfo _outputFile;
        private string _forType = null;
        private string _targetNamespace = "http://tempuri.org/nant-donotuse.xsd";

        #endregion Private Instance Fields

        #region Private Static Fields

        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        #endregion Private Static Fields

        #region Public Instance Properties

        /// <summary>
        /// The name of the output file to which the XSD should be written.
        /// </summary>
        [TaskAttribute("output", Required=true)]
        public virtual FileInfo OutputFile {
            get { return _outputFile; }
            set { _outputFile = value; }
        }

        /// <summary>
        /// The target namespace for the output. Defaults to "http://tempuri.org/nant-donotuse.xsd"
        /// </summary>
        [TaskAttribute("target-ns", Required=false)]
        public virtual string TargetNamespace {
            get { return _targetNamespace; }
            set { _targetNamespace = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// The <see cref="Type" /> for which an XSD should be created. If not
        /// specified, an XSD will be created for all available tasks.
        /// </summary>
        [TaskAttribute("class", Required=false)]
        public virtual string ForType {
            get { return _forType; }
            set { _forType = StringUtils.ConvertEmptyToNull(value); }
        }

        #endregion Public Instance Properties

        #region Override implementation of Task

        [ReflectionPermission(SecurityAction.Demand, Flags=ReflectionPermissionFlag.NoFlags)]
        protected override void ExecuteTask() {
            ArrayList taskTypes;

            if (ForType == null) {
                taskTypes = new ArrayList(TypeFactory.TaskBuilders.Count + TypeFactory.DataTypeBuilders.Count - 1);

                foreach (TaskBuilder tb in TypeFactory.TaskBuilders) {
                    taskTypes.Add(Assembly.LoadFrom(tb.AssemblyFileName).GetType(tb.ClassName, true, true));
                }
                /*
                foreach (DataTypeBaseBuilder tb in TypeFactory.DataTypeBuilders) {
                    taskTypes.Add(Assembly.LoadFrom(tb.AssemblyFileName).GetType(tb.ClassName, true, true));
                }
                */

            } else {
                taskTypes = new ArrayList(1);
                taskTypes.Add(Type.GetType(ForType, true, true));
            }
            
            FileIOPermission FilePermission = new FileIOPermission(FileIOPermissionAccess.AllAccess, OutputFile.FullName);             FilePermission.Assert();
            using (FileStream file = File.Open(OutputFile.FullName, FileMode.Create, FileAccess.Write, FileShare.Read)) {
                WriteSchema(file, (Type[])taskTypes.ToArray(typeof(Type)), TargetNamespace);

                file.Flush();
                file.Close();
            }

            Log(Level.Info, LogPrefix + "Wrote schema to '{0}'.", OutputFile.FullName);
        }

        #endregion Override implementation of Task

        #region Public Static Methods

        /// <summary>
        /// Creates a NAnt Schema for given types
        /// </summary>
        /// <param name="stream">The output stream to save the schema to. If null, writing is ignored, no exception generated</param>
        /// <param name="tasks">The list of Types to generate Schema for</param>
        /// <param name="targetNS">The target Namespace to output</param>
        /// <returns>The new NAnt Schema</returns>
        public static XmlSchema WriteSchema(System.IO.Stream stream, Type[] tasks, string targetNS) {
            NAntSchemaGenerator gen = new NAntSchemaGenerator(tasks, targetNS);

            if(!gen.Schema.IsCompiled) {
                gen.Compile();
            }

            if (stream != null) {
                gen.Schema.Write(stream);
            }

            return gen.Schema;
        }

        #endregion Public Static Methods

        #region Protected Static Methods

        protected static string GenerateIDFromType(Type type) {
            return type.ToString().Replace("+", "-").Replace("[","_").Replace("]","_");
        }

        /// <summary>
        /// Creates a new <see cref="XmlSchemaAttribute" /> instance.
        /// </summary>
        /// <param name="name">The name of the attribute.</param>
        /// <param name="required">Value indicating whether the attribute should be required.</param>
        /// <returns>The new <see cref="XmlSchemaAttribute" /> instance.</returns>
        protected static XmlSchemaAttribute CreateXsdAttribute(string name, bool required) {
            XmlSchemaAttribute newAttr = new XmlSchemaAttribute();

            newAttr.Name= name;

            if (required) {
                newAttr.Use = XmlSchemaUse.Required;
            } else {
                newAttr.Use = XmlSchemaUse.Optional;
            }

            return newAttr;
        }

        /// <summary>
        /// Creates a new <see cref="XmlSchemaSequence" /> instance.
        /// </summary>
        /// <param name="min">The minimum value to allow for this choice</param>
        /// <param name="max">The maximum value to allow, Decimal.MaxValue sets it to 'unbound'</param>
        /// <returns>The new <see cref="XmlSchemaSequence" /> instance.</returns>
        protected static XmlSchemaSequence CreateXsdSequence(Decimal min, Decimal max) {
            XmlSchemaSequence newSeq = new XmlSchemaSequence();

            newSeq.MinOccurs = min;

            if(max != Decimal.MaxValue) {
                newSeq.MaxOccurs = max;
            } else {
                newSeq.MaxOccursString = "unbounded";
            }
            
            return newSeq;
        }    

        protected static XmlNode[] TextToNodeArray(string text) {
            XmlDocument doc = new XmlDocument();

            return new XmlNode[1] {doc.CreateTextNode(text)};
        }

        /// <summary>
        /// Searches throught custom attributes for any attribute based on
        /// <paramref name="attributeType" />.
        /// </summary>
        /// <param name="member">Member that should be searched for custom attributes based on <paramref name="attributeType" />.</param>
        /// <param name="attributeType">Custom attribute type that should be searched for; meaning that you want something derived by it.</param>
        /// <param name="searchMemberHierarchy">Value indicating whether the <paramref name="member" /> class hierarchy should be searched for custom attributes.</param>
        /// <param name="searchAttributeHierarchy">Value indicating whether the <paramref name="attributeType" /> class hierarchy should be searched for a match.</param>
        /// <returns>
        /// A custom attribute matching the search criteria or a null reference 
        /// when no matching custom attribute is found.
        /// </returns>
        protected static Attribute GetDerivedAttribute(MemberInfo member, Type attributeType, bool searchMemberHierarchy, bool searchAttributeHierarchy) {
            if (searchAttributeHierarchy) {
                Attribute[] attrs = Attribute.GetCustomAttributes(member, searchMemberHierarchy);

                foreach (Attribute a in attrs) {
                    Type aType = a.GetType();

                    while (!typeof(object).Equals(aType.BaseType) && aType.BaseType != null) {
                        if (aType.Equals(attributeType)) {
                            return a;
                        }
                        aType = aType.BaseType;
                    }
                }
            } else {
                return Attribute.GetCustomAttribute(member, attributeType, searchMemberHierarchy);
            }

            return null;
        }

        #endregion Protected Static Methods

        private class NAntSchemaGenerator {
            #region Private Instance Fields

            IDictionary _nantComplexTypes = null;
            Type[] _tasks = null;
            XmlSchemaComplexType _targetCT = null;
            XmlSchema _nantSchema = new XmlSchema();
            string _namespaceURI = string.Empty;

            #endregion Private Instance Fields

            #region Public Instance Constructors

            /// <summary>
            /// Creates a new instance of the <see cref="NAntSchemaGenerator" />
            /// class.
            /// </summary>
            /// <param name="tasks">Tasks for which a schema should be generated.</param>
            /// <param name="targetNS">The namespace to use.
            /// <example> http://tempuri.org/nant.xsd </example>
            /// </param>
            public NAntSchemaGenerator(Type[] tasks, string targetNS) {
                //setup namespace stuff
                if (targetNS != null) {
                    _nantSchema.TargetNamespace = targetNS;
                    _nantSchema.Namespaces.Add("nant", _nantSchema.TargetNamespace);
                    _namespaceURI = targetNS;
                }

                _nantSchema.Namespaces.Add("vs","urn:schemas-microsoft-com:HTML-Intellisense");

                // Add XSD Namespace so that all xsd elements are prefix'd
                _nantSchema.Namespaces.Add("xs",XmlSchema.Namespace);

                //_nantSchema.ElementFormDefault = XmlSchemaForm.Unqualified;
                //_nantSchema.AttributeFormDefault = XmlSchemaForm.Unqualified;

                //initialize stuff
                _nantComplexTypes = new HybridDictionary(tasks.Length);
                _tasks = tasks;

                XmlSchemaAnnotation schemaAnnotation = new XmlSchemaAnnotation();
                XmlSchemaDocumentation schemaDocumentation = new XmlSchemaDocumentation();

                string doc = String.Format(CultureInfo.InvariantCulture, 
                    "NAnt schema generated at {0}", DateTime.Now);
                schemaDocumentation.Markup = TextToNodeArray(doc);
                schemaAnnotation.Items.Add(schemaDocumentation);
                _nantSchema.Items.Add(schemaAnnotation);

                //create temp list of task Complex Types
                ArrayList taskComplexTypes = new ArrayList(tasks.Length);
                ArrayList taskContainerComplexTypes = new ArrayList(15);

                XmlSchemaComplexType taskCT = FindOrCreateComplexType(typeof(TaskContainer));
                taskComplexTypes.Add(taskCT);
                taskContainerComplexTypes.Add(taskCT);

                foreach (Type t in tasks) {
                    taskCT = FindOrCreateComplexType(t);
                    taskComplexTypes.Add(taskCT);

                    //allow any tasks...
                    if(t.IsSubclassOf(typeof(TaskContainer)))
                        taskContainerComplexTypes.Add(taskCT);
                }

                Compile();
                //update the taskcontainerCTs to allow any other task and the list of tasks generated.
                foreach(XmlSchemaComplexType ct in taskContainerComplexTypes) {
                    XmlSchemaSequence seq = ct.Particle as XmlSchemaSequence;

                    if (seq != null) {
                        seq.Items.Add(CreateTaskListComplexType(tasks).Particle);
                    }
                    else {
                        logger.Error("Unable to fixup complextype with children. Particle is not XmlSchemaSequence");
                    }
                }
                Compile();

                //create target ComplexType
                _targetCT = CreateTaskListComplexType(tasks);
                _targetCT.Name="Target";

                //name attribute
                _targetCT.Attributes.Add(CreateXsdAttribute("name", true));

                //default attribute
                _targetCT.Attributes.Add(CreateXsdAttribute("depends", false));

                //description attribute
                _targetCT.Attributes.Add(CreateXsdAttribute("description", false));

                _nantSchema.Items.Add(_targetCT);

                //add to the list of ComplexTypes so that project will get it.
                taskComplexTypes.Add(_targetCT);

                Compile();

                // Generate project Element and ComplexType
                XmlSchemaElement projectElement = new XmlSchemaElement();
                projectElement.Name = "project";

                XmlSchemaComplexType projectCT = CreateTaskListComplexType(tasks, true);

                projectElement.SchemaType =  projectCT;

                //name attribute
                projectCT.Attributes.Add(CreateXsdAttribute("name", true));

                //default attribute
                projectCT.Attributes.Add(CreateXsdAttribute("default", false));

                //basedir attribute
                projectCT.Attributes.Add(CreateXsdAttribute("basedir", false));

                _nantSchema.Items.Add(projectElement);

                Compile();
            }

            protected XmlSchemaComplexType CreateTaskListComplexType(Type[] tasks) {
                return CreateTaskListComplexType(tasks, false);
            }

            protected XmlSchemaComplexType CreateTaskListComplexType(Type[] tasks, bool includeProjectLevelItems) {
                XmlSchemaComplexType tasklistCT = new XmlSchemaComplexType();
                XmlSchemaChoice choice = new XmlSchemaChoice();
                choice.MinOccurs = 0;
                choice.MaxOccursString = "unbounded";

                tasklistCT.Particle = choice;

                foreach (Type t in tasks) {
                    XmlSchemaElement taskElement = new XmlSchemaElement();
                    string typeId = GenerateIDFromType(t);
                    XmlSchemaComplexType taskCT = FindComplexTypeByID(typeId);

                    taskElement.Name = GetTaskName(t);
                    taskElement.SchemaTypeName = taskCT.QualifiedName;

                    choice.Items.Add(taskElement);
                }

                if (includeProjectLevelItems) {
                    XmlSchemaElement targetElement = new XmlSchemaElement();

                    targetElement.Name = "target";
                    targetElement.SchemaTypeName = _targetCT.QualifiedName;

                    choice.Items.Add(targetElement);
                }

                return tasklistCT;
            }

            private string GetTaskName(Type t) {
                TaskNameAttribute[] attrs = (TaskNameAttribute[])t.GetCustomAttributes(typeof(TaskNameAttribute), false);
                if (attrs.Length == 1) {
                    return attrs[0].Name;
                }
                else
                    return null;
            }

            /// <summary>
            /// Creates a new SchemaGenerator without a TargetNamespace.
            /// </summary>
            /// <param name="tasks">The Collection of Type(s) that represent the Task Classes to generation XSD for.</param>
            public NAntSchemaGenerator(Type[] tasks) : this(tasks, null) {
            }

            #endregion Public Instance Constructors

            #region Public Instance Properties

            public XmlSchema Schema {
                get {
                    if (!_nantSchema.IsCompiled) {
                        Compile();
                    }
                    return _nantSchema;
                }
            }

            #endregion Public Instance Properties

            #region Public Instance Methods

            public void Compile() {
                _nantSchema.Compile(new ValidationEventHandler(ValidationEH));
            }

            #endregion Public Instance Methods

            #region Protected Instance Methods

            protected void ValidationEH(object sender, ValidationEventArgs args) {
                if (args.Severity == XmlSeverityType.Warning) {
                    logger.Info("WARNING: ");
                } else if (args.Severity == XmlSeverityType.Error) {
                    logger.Error("ERROR: ");
                }

                XmlSchemaComplexType source = args.Exception.SourceSchemaObject as XmlSchemaComplexType;

                logger.Info(args.ToString());

                if (source != null) {
                    logger.Info(string.Format(CultureInfo.InvariantCulture, "{0}", source.Name));
                }
            }

            protected XmlSchemaComplexType FindComplexTypeByID(string id) {
                if (_nantComplexTypes.Contains(id)) {
                    return (XmlSchemaComplexType)_nantComplexTypes[id];
                }
                return null;
            }

            protected XmlSchemaComplexType FindOrCreateComplexType(Type t)  {
                XmlSchemaComplexType ct;
                string typeId = GenerateIDFromType(t);

                ct = FindComplexTypeByID(typeId);
                if (ct != null) {
                    return ct;
                }

                ct = new XmlSchemaComplexType();
                ct.Name = typeId;

#if NOT_IMPLEMENTED
                //
                // TODO - add task/type documentation in the future
                //

                ct.Annotation = new XmlSchemaAnnotation();
                XmlSchemaDocumentation doc = new XmlSchemaDocumentation();
                ct.Annotation.Items.Add(doc);
                doc.Markup = ...;
#endif

                XmlSchemaGroupBase group1 = CreateXsdSequence(0, Decimal.MaxValue);
                XmlSchemaObjectCollection attributesCollection = ct.Attributes;

                /*
                if (t.IsSubclassOf(typeof(TaskContainer))) {
                    //
                    // a failed attempt to map the class hierarchy to XSD
                    // I don't know how to do it because of "target" task which
                    // needs to show up in "project". But perhaps this can be done...
                    //
                    XmlSchemaComplexContentExtension ext = new XmlSchemaComplexContentExtension();
                    ext.BaseTypeName = FindComplexTypeByID(GenerateIDFromType(typeof(TaskContainer))).QualifiedName;
                    ext.Particle = group1;
                    ct.ContentModel = new XmlSchemaComplexContent();
                    ct.ContentModel.Content = ext;
                    attributesCollection = ext.Attributes;
                } else {
                */
                    ct.Particle = group1;
                    attributesCollection = ct.Attributes;
                //}

                foreach (MemberInfo memInfo in t.GetMembers(BindingFlags.Instance | BindingFlags.Public)) {
                    if (memInfo.DeclaringType.Equals(typeof(object))) {
                        continue;
                    }
                   
                    //Check for any return type that is derived from Element

                    //Add Attributes
                    TaskAttributeAttribute taskAttrAttr = (TaskAttributeAttribute) Attribute.GetCustomAttribute(memInfo, typeof(TaskAttributeAttribute), true);
                    BuildElementArrayAttribute buildElementArrayAttribute = (BuildElementArrayAttribute) Attribute.GetCustomAttribute(memInfo, typeof(BuildElementArrayAttribute), true);
                    BuildElementAttribute buildElemAttr = (BuildElementAttribute) Attribute.GetCustomAttribute(memInfo, typeof(BuildElementAttribute), true);

                    if (taskAttrAttr != null) {
                        XmlSchemaAttribute newAttr = CreateXsdAttribute(taskAttrAttr.Name, taskAttrAttr.Required);
                        attributesCollection.Add(newAttr);
                    } else if (buildElemAttr != null) {
                        // Create individial choice for any individual child Element
                        Decimal min = 0;

                        if (buildElemAttr.Required) {
                            min = 1;
                        }

                        //XmlSchemaGroupBase elementGroup = CreateXsdSequence(min, Decimal.MaxValue);
                        XmlSchemaElement childElement = new XmlSchemaElement();
                        childElement.MinOccurs = min;
                        childElement.MaxOccurs = 1;
                        childElement.Name = buildElemAttr.Name;

                        Type childType;

                        // We will only process child elements if they are defined for Properties or Fields, this should be enforced by the AttributeUsage on the Attribute class
                        if (memInfo is PropertyInfo) {
                            childType = ((PropertyInfo) memInfo).PropertyType;
                        } else if (memInfo is FieldInfo) {
                            childType = ((FieldInfo) memInfo).FieldType;
                        } else  {
                            throw new ApplicationException("Member Type != Field/Property");
                        }

                        // determine type of child elements
                        if (buildElementArrayAttribute != null)  {
                            if (buildElementArrayAttribute.ElementType == null) {
                                if (childType.IsArray) {
                                    childType = childType.GetElementType();
                                } 
                                else {
                                    Type elementType = null;

                                    // locate Add method with 1 parameter, type of that parameter is parameter type
                                    foreach (MethodInfo method in childType.GetMethods(BindingFlags.Public | BindingFlags.Instance)) {
                                        if (method.Name == "Add" && method.GetParameters().Length == 1) {
                                            ParameterInfo parameter = method.GetParameters()[0];
                                            elementType = parameter.ParameterType;
                                            break;
                                        }
                                    }

                                    childType = elementType;
                                }
                            } 
                            else {
                                childType = buildElementArrayAttribute.ElementType;
                            }

                            if (childType == null || !typeof(Element).IsAssignableFrom(childType)) {
                                throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                                    "BuildElementArrayAttribute and BuildElementCollectionAttribute" +
                                    " should have an element type assigned that derives from Element" +
                                    " for {0}.{1}.", memInfo.DeclaringType.FullName, memInfo.Name));
                            }
                        }

                        childElement.SchemaTypeName = FindOrCreateComplexType(childType).QualifiedName;
                        //elementGroup.Items.Add(childElement);
                        group1.Items.Add(childElement);
                    }
                }

                Schema.Items.Add(ct);
                _nantComplexTypes.Add(GenerateIDFromType(t), ct);
                Compile();

                return ct;
            }

            #endregion Public Instance Methods
        }
    }
}
