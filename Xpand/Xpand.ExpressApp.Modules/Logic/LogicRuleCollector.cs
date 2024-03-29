﻿using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Security;
using DevExpress.ExpressApp.Xpo;
using Xpand.ExpressApp.Security.Core;
using Xpand.Persistent.Base.General;
using Xpand.Persistent.Base.Logic;
using Xpand.Persistent.Base.Logic.Model;
using Fasterflect;

namespace Xpand.ExpressApp.Logic {
    public class LogicRuleCollector {
        internal static bool PermissionsReloaded;
        ModuleBase _module;
        public event EventHandler RulesCollected;
        public event EventHandler<CollectModelLogicsArgs> CollectModelLogics;

        protected virtual void OnCollectModelLogics(CollectModelLogicsArgs e) {
            EventHandler<CollectModelLogicsArgs> handler = CollectModelLogics;
            handler?.Invoke(this, e);
        }

        readonly HashSet<IModelLogicWrapper> _modelLogics = new();
        private Type[] _generatedTypes;

        void ApplicationOnSetupComplete(object sender, EventArgs eventArgs) {
            _module.Application.SetupComplete -= ApplicationOnSetupComplete;
            var typesInfo = ((XafApplication) sender).TypesInfo;
            var xpoTypeInfoSource = XpoTypesInfoHelper.GetXpoTypeInfoSource();
            _generatedTypes = typesInfo.PersistentTypes.Where(info => info.IsInterface).Select(info => xpoTypeInfoSource.GetGeneratedEntityType(info.Type)).Where(type => type!=null).ToArray();
            CollectRules((XafApplication)sender);
        }

        protected virtual IEnumerable<ILogicRule> CollectRulesFromPermissions(IModelLogicWrapper modelLogic, ITypeInfo typeInfo) {
            return  GetPermissions().Where(permission =>permission.TypeInfo != null && permission.TypeInfo.Type == typeInfo.Type)
                .OrderBy(rule => rule.Index);
        }

        IEnumerable<IContextLogicRule> GetPermissions() {
            return SecuritySystem.CurrentUser is ISecurityUserWithRoles
                ? ((ISecurityUserWithRoles)SecuritySystem.CurrentUser).GetPermissions().OfType<IContextLogicRule>()
                : Enumerable.Empty<IContextLogicRule>();
        }

        void ReloadPermissions() {
            if (SecuritySystem.Instance is ISecurityComplex)
                if (SecuritySystem.CurrentUser != null && !PermissionsReloaded) {
                    SecuritySystem.ReloadPermissions();
                    PermissionsReloaded = true;
                }
        }

        protected virtual ILogicRuleObject CreateRuleObject(IContextLogicRule contextLogicRule, IModelLogicWrapper modelLogic) {
            var logicRuleObjectType = LogicRuleObjectType(contextLogicRule);
            var logicRuleObject = ((ILogicRuleObject)logicRuleObjectType.CreateInstance(contextLogicRule));
            logicRuleObject.TypeInfo = contextLogicRule.TypeInfo;
            logicRuleObject.ExecutionContext = GetExecutionContext(contextLogicRule, modelLogic);
            logicRuleObject.FrameTemplateContext = GetFrameTemplateContext(contextLogicRule, modelLogic);
            AddViews(logicRuleObject.Views,modelLogic,contextLogicRule);
            AddObjectChangedProperties(logicRuleObject.ObjectChangedPropertyNames, modelLogic, contextLogicRule);
            return logicRuleObject;
        }

        private void AddObjectChangedProperties(HashSet<string> objectChangedPropertyNames, IModelLogicWrapper modelLogic, IContextLogicRule contextLogicRule){
            if (!string.IsNullOrEmpty(contextLogicRule.ObjectChangedExecutionContextGroup)) {
                var executionContexts = modelLogic.ObjectChangedExecutionContextGroup.FirstOrDefault(
                        contexts => contexts.Id == contextLogicRule.ObjectChangedExecutionContextGroup);
                if (executionContexts != null)
                    foreach (var s in executionContexts.SelectMany(executionContext => executionContext.PropertyNames.Split(';'))){
                        objectChangedPropertyNames.Add(s);
                    }
            }
        }

        void AddViews(HashSet<string> views, IModelLogicWrapper modelLogic, IContextLogicRule contextLogicRule) {
            if (!string.IsNullOrEmpty(contextLogicRule.ViewContextGroup)) {
                var modelViewContexts =modelLogic.ViewContextsGroup.FirstOrDefault(
                        contexts => contexts.Id == contextLogicRule.ViewContextGroup);
                if (modelViewContexts != null)
                    foreach (var modelViewContext in modelViewContexts) {
                        views.Add(modelViewContext.Name);
                    }
            }
        }

        FrameTemplateContext GetFrameTemplateContext(IContextLogicRule contextLogicRule, IModelLogicWrapper modelLogic) {
            var templateContexts = modelLogic.FrameTemplateContextsGroup.FirstOrDefault(contexts => contexts.Id == contextLogicRule.FrameTemplateContextGroup);
            return templateContexts?.FrameTemplateContext ?? FrameTemplateContext.All;
        }

        ExecutionContext GetExecutionContext(IContextLogicRule contextLogicRule, IModelLogicWrapper modelLogic) {
            var modelExecutionContexts = modelLogic.ExecutionContextsGroup.FirstOrDefault(contexts => contexts.Id == contextLogicRule.ExecutionContextGroup);
            return modelExecutionContexts?.ExecutionContext ?? ExecutionContext.None;
        }

        Type LogicRuleObjectType(ILogicRule logicRule) {
            var typesInfo = _module.Application.TypesInfo;
            var type = _modelLogics.Where(logicWrapper => logicWrapper.RuleType.IsInstanceOfType(logicRule)).Select(wrapper => wrapper.RuleType).First();
            return typesInfo.FindTypeInfo<LogicRule>().Descendants.Single(info => !info.Type.IsAbstract&&type.IsAssignableFrom(info.Type)).Type;
        }

        void OnRulesCollected(EventArgs e) {
            EventHandler handler = RulesCollected;
            handler?.Invoke(this, e);
        }

        public virtual void CollectRules(XafApplication xafApplication) {
            AddModelLogics();
            lock (LogicRuleManager.Instance) {
                ReloadPermissions();
                LogicRuleManager.Instance.ClearAllRules();
                foreach (var modelLogic in _modelLogics) {
                    CollectRules(modelLogic.Rules,modelLogic);
                }
                var groupings = GetPermissions().Select(rule => new{rule,Type=rule.GetType()}).
                    GroupBy(arg => arg.Type).Select(grouping 
                        => new { ModelLogic = GetModelLogic(grouping.Key), Rules = grouping.Select(arg => arg.rule) });
                foreach (var grouping in groupings) {
                    CollectRules(grouping.Rules, grouping.ModelLogic);
                }
            }
            OnRulesCollected(EventArgs.Empty);
        }

        protected virtual void CollectRules(IEnumerable<IContextLogicRule> logicRules, IModelLogicWrapper modelLogic) {
            var ruleObjects = logicRules.Select(rule => CreateRuleObject(rule, modelLogic));
            var groupings = ruleObjects.GroupBy(rule => rule.TypeInfo).Select(grouping => new { grouping.Key, Rules = grouping }).ToList();
            foreach (var grouping in groupings) {
                LogicRuleManager.Instance.AddRules(grouping.Key,grouping.Rules);
                if (grouping.Key.Type == typeof(AllViews)){
                    foreach (var typeInfo in XafTypesInfo.Instance.PersistentTypes){
                        LogicRuleManager.Instance.AddRules(typeInfo, grouping.Rules);
                    }
                }
                else{
                    foreach (var info in grouping.Key.Descendants)
                        LogicRuleManager.Instance.AddRules(info, grouping.Rules);
                    foreach (var generatedType in _generatedTypes) {
                        if (grouping.Key.Type.IsAssignableFrom(generatedType))
                            LogicRuleManager.Instance.AddRules(generatedType.GetITypeInfo(), grouping.Rules);
                    }
                }
            }
        }

        IModelLogicWrapper GetModelLogic(Type type) {
            return _modelLogics.First(logicWrapper => logicWrapper.RuleType.IsAssignableFrom(type));
        }

        void AddModelLogics() {
            var collectModelLogicsArgs = new CollectModelLogicsArgs();
            OnCollectModelLogics(collectModelLogicsArgs);
            var modelLogics = collectModelLogicsArgs.ModelLogics.Where(modelLogic => !_modelLogics.Contains(modelLogic));
            foreach (var modelLogic in modelLogics) {
                _modelLogics.Add(modelLogic);
            }
        }

        public void Attach(ModuleBase moduleBase) {
            _module = moduleBase;
            if (moduleBase.Application != null) {
                moduleBase.Application.LoggedOn += (o, _) => CollectRules((XafApplication)o);
                moduleBase.Application.SetupComplete += ApplicationOnSetupComplete;
            }
        }
    }

    public class CollectModelLogicsArgs : EventArgs {
        public List<IModelLogicWrapper> ModelLogics { get; } = new();
    }
}