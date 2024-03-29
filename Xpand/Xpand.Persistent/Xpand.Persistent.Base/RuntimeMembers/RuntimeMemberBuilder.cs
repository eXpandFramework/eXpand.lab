﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Localization;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Xpo;
using DevExpress.Persistent.Base;
using DevExpress.Xpo;
using DevExpress.Xpo.Metadata;
using Xpand.Extensions.XAF.Xpo;
using Xpand.Persistent.Base.RuntimeMembers.Model;
using Xpand.Persistent.Base.RuntimeMembers.Model.Collections;
using Xpand.Persistent.Base.Xpo;
using Xpand.Persistent.Base.Xpo.MetaData;
using Xpand.Xpo;
using Xpand.Xpo.MetaData;
using Xpand.Persistent.Base.General;

namespace Xpand.Persistent.Base.RuntimeMembers {
    public class RuntimeMemberBuilder {
        public static event EventHandler<CustomCreateMemberArgs> CustomCreateMember;

        static void OnCustomCreateMember(CustomCreateMemberArgs e) {
            EventHandler<CustomCreateMemberArgs> handler = CustomCreateMember;
            handler?.Invoke(null, e);
        }

        
        private static IEnumerable<IModelMemberEx> GetMembersEx(IModelApplication model) {
            return model.BOModel.SelectMany(modelClass => modelClass.AllMembers).OfType<IModelMemberEx>().Distinct();
        }

        public static void CreateRuntimeMembers(IModelApplication model) {
            using (var objectSpace = CreateObjectSpace()) {
                Tracing.Tracer.LogVerboseSubSeparator("RuntimeMembers Creation started");
                var modelMemberOneToManyCollections = new List<IModelMemberOneToManyCollection>();
                var xpObjectSpace = objectSpace as XPObjectSpace;
                var modelMemberExs = GetMembersEx(model).ToArray();
                var exceptions = new List<Exception>();
                foreach (var memberEx in modelMemberExs) {
                    try{
                        var customCreateMemberArgs = new CustomCreateMemberArgs(memberEx);
                        OnCustomCreateMember(customCreateMemberArgs);
                        if (!customCreateMemberArgs.Handled) {
                            var modelMemberOneToManyCollection = memberEx as IModelMemberOneToManyCollection;
                            if (modelMemberOneToManyCollection == null) {
                                CreateXpandCustomMemberInfo(memberEx, xpObjectSpace);
                            }
                            else {
                                modelMemberOneToManyCollections.Add(modelMemberOneToManyCollection);
                            }
                        }

                    }
                    catch (Exception e){
                        exceptions.Add(new Exception($"Failed to create {memberEx.Name} on {memberEx.ModelClass.Name}",e));
                    }
                }
                foreach (var exception in exceptions){
                    Tracing.Tracer.LogError(exception);
                }
                if (!SuppressException&&exceptions.Any())
                    throw new AggregateException("Runtime members creation failed",exceptions);
                RefreshTypes(XafTypesInfo.Instance, modelMemberExs.Select(ex => ex.ModelClass.TypeInfo).Distinct());
                CreateAssociatedCollectionMembers(modelMemberOneToManyCollections, xpObjectSpace);
                RefreshTypes(XafTypesInfo.Instance, modelMemberOneToManyCollections.Select(collection => collection.CollectionType.TypeInfo).Distinct());
            }
            Tracing.Tracer.LogVerboseSubSeparator("RuntimeMembers Creation finished");
        }

        public static bool SuppressException{ get; set; }

        static void CreateAssociatedCollectionMembers(IEnumerable<IModelMemberOneToManyCollection> modelMemberOneToManyCollections, XPObjectSpace xpObjectSpace) {
            foreach (var modelMemberOneToManyCollection in modelMemberOneToManyCollections) {
                CreateXpandCustomMemberInfo(modelMemberOneToManyCollection, xpObjectSpace);
            }
        }

        static void RefreshTypes(ITypesInfo typesInfo, IEnumerable<ITypeInfo> typeInfos) {
            foreach (var typeInfo in typeInfos) {
                typesInfo.RefreshInfo(typeInfo);
            }
        }

        static IObjectSpace CreateObjectSpace() {
            return XpandModuleBase.ObjectSpaceCreated?ApplicationHelper.Instance.Application.ObjectSpaceProvider.CreateObjectSpace():null;
        }

        static void CreateXpandCustomMemberInfo(IModelMemberEx modelMemberEx, XPObjectSpace objectSpace) {
            try {
                Type classType = modelMemberEx.ModelClass.TypeInfo.Type;
                XPClassInfo xpClassInfo = modelMemberEx.ModelClass.TypeInfo.QueryXPClassInfo();
                lock (xpClassInfo) {
                    var customMemberInfo = xpClassInfo.FindMember(modelMemberEx.Name) as XPCustomMemberInfo;
                    if (customMemberInfo == null) {
                        customMemberInfo= CreateMemberInfo(modelMemberEx, xpClassInfo);
                        XafTypesInfo.Instance.RefreshInfo(classType);
                        AddAttributes(modelMemberEx, customMemberInfo);
                    }

                    if (customMemberInfo is XpandCustomMemberInfo xpandCustomMemberInfo) {
                        CreateColumn(modelMemberEx as IModelMemberPersistent, objectSpace, xpandCustomMemberInfo);
                        CreateForeignKey(modelMemberEx as IModelMemberOneToManyCollection, objectSpace, xpandCustomMemberInfo);
                        UpdateMember(modelMemberEx, customMemberInfo);
                    }
                }
            }
            catch (Exception exception) {
                throw new Exception(
                    ExceptionLocalizerTemplate<SystemExceptionResourceLocalizer, ExceptionId>.GetExceptionMessage(
                        ExceptionId.ErrorOccursWhileAddingTheCustomProperty,
                        modelMemberEx.MemberInfo.MemberType,
                        ((IModelClass) modelMemberEx.Parent).Name,
                        modelMemberEx.Name,
                        exception.Message));
            }
        }

        static void CreateForeignKey(IModelMemberOneToManyCollection modelMemberOneToManyCollection, XPObjectSpace objectSpace,  XpandCustomMemberInfo customMemberInfo) {
            if (CanCreateForeignKey(modelMemberOneToManyCollection, objectSpace)) {
                var xpCustomMemberInfo = customMemberInfo.GetAssociatedMember() as XPCustomMemberInfo;
                if (xpCustomMemberInfo == null) throw new NullReferenceException("xpCustomMemberInfo");
                objectSpace.CreateForeignKey(xpCustomMemberInfo);
                modelMemberOneToManyCollection.AssociatedMember.DataStoreForeignKeyCreated = true;
                modelMemberOneToManyCollection.DataStoreForeignKeyCreated = true;
            }
        }

        static bool CanCreateForeignKey(IModelMemberOneToManyCollection modelMemberOneToManyCollection, XPObjectSpace objectSpace) {
            return CanCreateDbArtifact(modelMemberOneToManyCollection, objectSpace)&&!modelMemberOneToManyCollection.AssociatedMember.DataStoreForeignKeyCreated;
        }

        static void CreateColumn(IModelMemberPersistent modelMemberPersistent, XPObjectSpace objectSpace, 
                                 XpandCustomMemberInfo customMemberInfo) {
            if (CanCreateColumn(modelMemberPersistent, objectSpace)) {
                objectSpace.CreateColumn(customMemberInfo);
                modelMemberPersistent.DataStoreColumnCreated = true;
                modelMemberPersistent.DataStoreForeignKeyCreated = customMemberInfo.HasAttribute(typeof(AssociationAttribute));
            }
        }

        static bool CanCreateColumn(IModelMemberPersistent modelMemberPersistent, XPObjectSpace objectSpace) {
            return CanCreateDbArtifact(modelMemberPersistent, objectSpace) && !modelMemberPersistent.DataStoreColumnCreated && modelMemberPersistent.MemberInfo.IsPersistent;
        }

        static bool CanCreateDbArtifact(IModelMemberEx modelMemberEx, XPObjectSpace objectSpace) {
            return modelMemberEx != null && objectSpace != null && (modelMemberEx.CreatedAtDesignTime.HasValue&&!modelMemberEx.CreatedAtDesignTime.Value);
        }

        static void UpdateMember(IModelMemberEx modelMemberEx, XPMemberInfo xpMemberInfo) {
            if (modelMemberEx is IModelMemberCalculated modelRuntimeCalculatedMember) {
                ((XpandCalcMemberInfo)xpMemberInfo).SetAliasExpression(modelRuntimeCalculatedMember.AliasExpression);
            }
        }

        static void AddAttributes(IModelMemberEx modelMemberEx, XPCustomMemberInfo memberInfo){
            if (modelMemberEx.Size != 0)
                memberInfo.AddAttribute(new SizeAttribute(modelMemberEx.Size));
            if (modelMemberEx is IModelMemberNonPersistent && !(modelMemberEx is IModelMemberCalculated))
                memberInfo.AddAttribute(new NonPersistentAttribute());
        }

        static XpandCustomMemberInfo CreateMemberInfo(IModelMemberEx modelMemberEx, XPClassInfo xpClassInfo) {
            if (modelMemberEx is IModelMemberCalculated calculatedMember)
                return xpClassInfo.CreateCalculabeMember(calculatedMember.Name, calculatedMember.Type, calculatedMember.AliasExpression);
            if (modelMemberEx is IModelMemberOrphanedColection modelMemberOrphanedColection) {
                return xpClassInfo.CreateCollection(modelMemberOrphanedColection.Name, modelMemberOrphanedColection.CollectionType.TypeInfo.Type,
                                                    modelMemberOrphanedColection.Criteria);
            }

            if (modelMemberEx is IModelMemberOneToManyCollection modelMemberOneToManyCollection) {
                var elementType = modelMemberOneToManyCollection.CollectionType.TypeInfo.Type;
                var associationAttribute = new AssociationAttribute(modelMemberOneToManyCollection.AssociationName, elementType);
                var xpandCollectionMemberInfo = xpClassInfo.CreateCollection(modelMemberOneToManyCollection.Name, elementType, null, associationAttribute);
                modelMemberOneToManyCollection.AssociatedMember.ModelClass.TypeInfo.FindMember(modelMemberOneToManyCollection.AssociatedMember.Name).AddAttribute(associationAttribute);
                return xpandCollectionMemberInfo;
            }

            if (modelMemberEx is IModelMemberModelMember modelMemberModelMember){
                var memberInfo = ModelMemberModelMemberDomainLogic.Get_MemberInfo(modelMemberModelMember);
                return (XpandCustomMemberInfo) xpClassInfo.FindMember(memberInfo.Name);
            }   
            return xpClassInfo.CreateCustomMember(modelMemberEx.Name, modelMemberEx.Type,modelMemberEx is IModelMemberNonPersistent);
        }
    }

    public class CustomCreateMemberArgs : HandledEventArgs {
        public CustomCreateMemberArgs(IModelMemberEx modelMemberEx) {
            ModelMemberEx = modelMemberEx;
        }

        public IModelMemberEx ModelMemberEx { get; }
    }
}