﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Core;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.Persistent.Base;
using Xpand.Persistent.Base.General;
using Xpand.Utils.Helpers;
using Fasterflect;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.Extensions.XAF.ApplicationModulesManagerExtensions;
using Xpand.Persistent.Base.ModelAdapter;

namespace Xpand.Persistent.Base.ModelDifference {
    internal class ModelBuilder {

        readonly string _assembliesPath = AppDomain.CurrentDomain.ApplicationPath();
        XafApplication _application;
        ITypesInfo _typesInfo;
        string _moduleName;
        XpandApplicationModulesManager _modulesManager;
        

        ModelBuilder() {
        }

        private IEnumerable<string> GetAspects(string configFileName) {
            if (!string.IsNullOrEmpty(configFileName) && configFileName.EndsWith(".config")) {
                var exeConfigurationFileMap = new ExeConfigurationFileMap { ExeConfigFilename = configFileName };
                Configuration configuration = ConfigurationManager.OpenMappedExeConfiguration(exeConfigurationFileMap, ConfigurationUserLevel.None);
                KeyValueConfigurationElement languagesElement = configuration.AppSettings.Settings["Languages"];
                if (languagesElement != null) {
                    string languages = languagesElement.Value;
                    return languages.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                }
            }
            return Enumerable.Empty<string>();
        }

        public static ModelBuilder Create() {
            return new();
        }

        string GetConfigPath() {
            string path = Path.Combine(_assembliesPath, _moduleName);
            string config = path + ".config";
            if (File.Exists(_assembliesPath + "web.config"))
                config = Path.Combine(_assembliesPath, "web.config");
            return config;
        }

        private string[] GetModulesFromConfig(XafApplication application) {
            Configuration config = null;
            if (application is IWinApplication) {
                config = ConfigurationManager.OpenExeConfiguration(AppDomain.CurrentDomain.ApplicationPath() + _moduleName);
            } else {
                // var mapping = new WebConfigurationFileMap();
                // mapping.VirtualDirectories.Add("/Dummy", new VirtualDirectoryMapping(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, true));
                // config = WebConfigurationManager.OpenMappedWebConfiguration(mapping, "/Dummy");
            }

            return config?.AppSettings.Settings["Modules"]?.Value.Split(';');
        }


        ModelApplicationBase BuildModel(XafApplication application, string configFileName, XpandApplicationModulesManager applicationModulesManager) {
            XpandModuleBase.CallMonitor.Clear();
            var modelAssemblyFile = typeof(XafApplication).Invoke(application, "GetModelAssemblyFilePath") as string;
            if (!File.Exists(modelAssemblyFile)&&!SkipModelAssemblyFile) {
                throw new FileNotFoundException(
                    $"The ModelEditor requires a valid ModelAssembly. Reference the Xpand.ExpressApp.ModelDifference assembly in your front end project, override the {application.GetType().FullName} GetModelAssemblyFilePath to provide a valid filename using the call this.GetModelFilePath()");
            }

            applicationModulesManager.TypesInfo.AssignAsInstance();
            var modelApplication = applicationModulesManager.CreateModel(GetAspects(configFileName));
            var modelApplicationBase = modelApplication.CreatorInstance.CreateModelApplication();
            modelApplicationBase.Id = "After Setup";
            ModelApplicationHelper.AddLayer(modelApplication, modelApplicationBase);
            return modelApplication;
        }

        public static bool SkipModelAssemblyFile { get; set; }

        XpandApplicationModulesManager CreateModulesManager(XafApplication application, string configFileName, string assembliesPath, ITypesInfo typesInfo) {
            if (!string.IsNullOrEmpty(configFileName)) {
                bool isWebApplicationModel = String.Compare(Path.GetFileNameWithoutExtension(configFileName), "web", StringComparison.OrdinalIgnoreCase) == 0;
                if (string.IsNullOrEmpty(assembliesPath)) {
                    assembliesPath = Path.GetDirectoryName(configFileName);
                    if (isWebApplicationModel) {
                        assembliesPath = Path.Combine(assembliesPath + "", "Bin");
                    }
                }
            }
            ReflectionHelper.AddResolvePath(assembliesPath);
            ITypesInfo synchronizeTypesInfo = null;
            try {
                var applicationModulesManager = new XpandApplicationModulesManager(new ControllersManager(), assembliesPath);
                if (application != null) {
                    foreach (ModuleBase module in application.Modules) {
                        applicationModulesManager.AddModule(module);
                    }
                    applicationModulesManager.Security = application.Security;
                    applicationModulesManager.AddAdditionalModules(application);
                }
                if (!string.IsNullOrEmpty(configFileName)) {
                    applicationModulesManager.AddModuleFromAssemblies(GetModulesFromConfig(application));
                }
                var loadTypesInfo = typesInfo != XafTypesInfo.Instance;
                synchronizeTypesInfo = XafTypesInfo.Instance;
                typesInfo.AssignAsInstance();
                applicationModulesManager.TypesInfo = typesInfo;
                var runtimeMode = InterfaceBuilder.RuntimeMode;
                InterfaceBuilder.RuntimeMode = false;
                applicationModulesManager.Load(typesInfo, loadTypesInfo);
                InterfaceBuilder.RuntimeMode = runtimeMode;
                return applicationModulesManager;
            } finally {
                synchronizeTypesInfo.AssignAsInstance();
                ReflectionHelper.RemoveResolvePath(assembliesPath);
            }

        }

        public ModelBuilder WithApplication(XafApplication xafApplication) {
            _application = xafApplication;
            return this;
        }

        public ModelApplicationBase Build(bool rebuild) {
            string config = GetConfigPath();
            if (!rebuild)
                _modulesManager = CreateModulesManager(_application, config, _assembliesPath, _typesInfo);
            return BuildModel(_application, config, _modulesManager);
        }

        public ModelBuilder UsingTypesInfo(ITypesInfo typesInfo) {
            _typesInfo = typesInfo;
            return this;
        }

        public ModelBuilder FromModule(string moduleName) {
            _moduleName = moduleName;
            return this;
        }
    }
    public class ModelLoader {
        public static bool IsDebug { get; set; }

        readonly string _moduleName;
        readonly ITypesInfo _instance;
        ITypesInfo _typesInfo;
        XafApplication _xafApplication;
        ModelBuilder _modelBuilder;

        public ModelLoader(string moduleName, ITypesInfo instance) {
            _moduleName = moduleName;
            _instance = instance;
        }

        public ModelApplicationBase ReCreate(XafApplication xafApplication) {
            _xafApplication=xafApplication;
            return GetMasterModelCore(true);
        }

        public ModelApplicationBase GetMasterModel( XafApplication xafApplication, Action<ITypesInfo> action=null) {
            _xafApplication=xafApplication;
            _typesInfo=_xafApplication.TypesInfo;
            var modelApplicationBase = GetMasterModelCore(false);
            action?.Invoke(_instance);
            return modelApplicationBase;
        }

        public ModelApplicationBase GetMasterModel(XafApplication application,bool tryToUseCurrentTypesInfo,Action<ITypesInfo> action=null) {
            
            if (!File.Exists(_moduleName))
                throw new UserFriendlyException(_moduleName+" not found in path");
            ModelApplicationBase masterModel=null;
            Retry.Do(() =>{
                _typesInfo = TypesInfoBuilder.Create()
                    .FromModule(_moduleName)
                    .Build(tryToUseCurrentTypesInfo);
                _xafApplication = ApplicationBuilder.Create().
                    UsingTypesInfo(_ => _typesInfo).
                    FromModule(_moduleName).
                    Build(application.Modules);

                masterModel = GetMasterModel(_xafApplication, action);
            }, TimeSpan.FromTicks(1), 2);
            return masterModel;
        }

        ModelApplicationBase GetMasterModelCore(bool rebuild) {
            ModelApplicationBase modelApplicationBase;
            try {
                _modelBuilder = !rebuild ? ModelBuilder.Create() : _modelBuilder;
                modelApplicationBase = _modelBuilder
                    .UsingTypesInfo(_typesInfo)
                    .FromModule(_moduleName)
                    .WithApplication(_xafApplication)
                    .Build(rebuild);
            } catch (Exception e) {
                Tracing.Tracer.LogSeparator("CompilerErrorException");
                Tracing.Tracer.LogError(e);
                // Tracing.Tracer.LogValue("Source Code", e.SourceCode);
                throw;
            }
            return modelApplicationBase;
        }

    }
}
