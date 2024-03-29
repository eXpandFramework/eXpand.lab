﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.MiddleTier;
using DevExpress.ExpressApp.Utils;
using DevExpress.ExpressApp.Xpo;
using DevExpress.Persistent.Base;
using DevExpress.Xpo;
using DevExpress.Xpo.DB.Helpers;
using Fasterflect;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;

namespace Xpand.Persistent.Base.General {

    public class ApplicationBuilder {
        string _assemblyPath = AppDomain.CurrentDomain.ApplicationPath();
        Func<string, ITypesInfo> _buildTypesInfoSystem = BuildTypesInfoSystem(true);
        string _moduleName;
        bool _withOutObjectSpaceProvider;

        ApplicationBuilder() {
        }

        public static ApplicationBuilder Create() {
            return new();
        }

        static Func<string, ITypesInfo> BuildTypesInfoSystem(bool tryToUseCurrentTypesInfo) {
            return moduleName => TypesInfoBuilder.Create()
                                                 .FromModule(moduleName)
                                                 .Build(tryToUseCurrentTypesInfo);
        }

        public ApplicationBuilder FromAssembliesPath(string path) {
            _assemblyPath = path;
            return this;
        }
        public ApplicationBuilder UsingTypesInfo(Func<string, ITypesInfo> buildTypesInfoSystem) {
            _buildTypesInfoSystem = buildTypesInfoSystem;
            return this;
        }

        public ApplicationBuilder FromModule(string moduleName) {
            _moduleName = moduleName;
            return this;
        }

        public XafApplication Build(ModuleList moduleList) {
            try {
                var typesInfo = _buildTypesInfoSystem.Invoke(_moduleName);
                ReflectionHelper.AddResolvePath(_assemblyPath);
                var assembly = ReflectionHelper.GetAssembly(Path.GetFileNameWithoutExtension(_moduleName), _assemblyPath);
                var assemblyInfo = typesInfo.FindAssemblyInfo(assembly);
                typesInfo.LoadTypes(assembly);
                var findTypeInfo = typesInfo.FindTypeInfo(typeof(XafApplication));
                var findTypeDescendants = ReflectionHelper.FindTypeDescendants(assemblyInfo, findTypeInfo, false);
                var securityInstance = SecuritySystem.Instance;
                var info = XafTypesInfo.Instance;
                var application = ApplicationHelper.Instance.Application;
                typesInfo.AssignAsInstance();
                var xafApplication = ((XafApplication)Enumerator.GetFirst(findTypeDescendants).CreateInstance());
                foreach (var m in moduleList) {
                    if (xafApplication.FindModule(m.GetType()) == null) {
                        xafApplication.Modules.Add(m);
                    }    
                }
                
                SecuritySystem.SetInstance(securityInstance);
                SetConnectionString(xafApplication);
                if (!_withOutObjectSpaceProvider) {
                    var objectSpaceProviders = xafApplication.ObjectSpaceProviders();
                    objectSpaceProviders.Add(new MyClass(xafApplication));
                }
                info.AssignAsInstance();
                if (application != null) ApplicationHelper.Instance.Initialize(application);
                return xafApplication;
            } finally {
                ReflectionHelper.RemoveResolvePath(_assemblyPath);
            }
        }

        class MyClass : XPObjectSpaceProvider {
            public MyClass(XafApplication xafApplication)
                : base(new ConnectionStringDataStoreProvider(GetConnectionString(xafApplication))) {
            }

            [SuppressMessage("Design", "XAF0013:Avoid reading the XafApplication.ConnectionString property")]
            static string GetConnectionString(XafApplication xafApplication) {
                if (!string.IsNullOrEmpty(xafApplication.ConnectionString))
                    return xafApplication.ConnectionString;
                if (!Environment.Is64BitProcess)
                    return XpoDefault.ConnectionString;
                var connectionStringParser = new ConnectionStringParser(XpoDefault.ActiveConnectionString);
                connectionStringParser.UpdatePartByName("Provider", "Microsoft.ACE.OLEDB.12.0");
                return connectionStringParser.GetConnectionString();
            }
        }
        void SetConnectionString(XafApplication xafApplication) {
            try {
                var connectionString = XpandModuleBase.ConnectionString;
                if (connectionString != null) {
                    (xafApplication).ConnectionString = connectionString;
                }
            }
            catch (NullReferenceException) {
            }
        }

        public ApplicationBuilder WithOutObjectSpaceProvider() {
            _withOutObjectSpaceProvider = true;
            return this;
        }

    }
}