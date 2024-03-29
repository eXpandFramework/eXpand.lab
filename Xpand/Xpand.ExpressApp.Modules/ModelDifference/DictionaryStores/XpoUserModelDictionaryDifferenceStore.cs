﻿using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Security;
using DevExpress.ExpressApp.Xpo;
using Xpand.ExpressApp.ModelDifference.Core;
using Xpand.ExpressApp.ModelDifference.DataStore.BaseObjects;
using Xpand.ExpressApp.ModelDifference.DataStore.Queries;
using Xpand.ExpressApp.ModelDifference.Security.Improved;
using Xpand.ExpressApp.Security.Core;
using Xpand.Persistent.Base;
using Xpand.Persistent.Base.General;
using Xpand.Persistent.Base.ModelAdapter;
using Xpand.Persistent.Base.ModelDifference;


namespace Xpand.ExpressApp.ModelDifference.DictionaryStores {
    public class XpoUserModelDictionaryDifferenceStore : XpoDictionaryDifferenceStore {
        public XpoUserModelDictionaryDifferenceStore(XafApplication application)
            : base(application) {
        }

        public override DifferenceType DifferenceType => DifferenceType.User;

        protected override bool DeviceModelsEnabled => Application.UserDeviceModelsEnabled();

        protected internal override ModelDifferenceObject GetActiveDifferenceObject(string name, DeviceCategory deviceCategory) {
            return new QueryUserModelDifferenceObject(ObjectSpace.Session).GetActiveModelDifference(Application.GetType().FullName, Application,deviceCategory);
        }

        protected internal IQueryable<ModelDifferenceObject> GetActiveDifferenceObjects(DeviceCategory deviceCategory) {
            return new QueryUserModelDifferenceObject(ObjectSpace.Session).GetActiveModelDifferences(Application.GetType().FullName, null,deviceCategory).Cast<ModelDifferenceObject>();
        }

        protected internal IQueryable<ModelDifferenceObject> GetActiveRoleDifferenceObjects(DeviceCategory deviceCategory) {
            return new QueryRoleModelDifferenceObject(ObjectSpace.Session).GetActiveModelDifferences(Application.GetType().FullName, null,deviceCategory).Cast<ModelDifferenceObject>();
        }


        void CombineWithActiveDifferenceObjects(ModelApplicationBase model, IEnumerable<ModelDifferenceObject> modelDifferenceObjects) {
            var reader = new ModelXmlReader();
            foreach (var modelDifferenceObject in modelDifferenceObjects) {
                foreach (var aspectObject in modelDifferenceObject.AspectObjects) {
                    var xml = aspectObject.Xml;
                    if (!string.IsNullOrEmpty(xml))
                        reader.ReadFromString(model, modelDifferenceObject.GetAspectName(aspectObject), xml);
                }
            }
        }

        protected internal override ModelDifferenceObject GetNewDifferenceObject(IObjectSpace session) {
            var aspectObject = session.CreateObject<UserModelDifferenceObject>();
            aspectObject.AssignToCurrentUser();
            return aspectObject;
        }

        protected internal override void OnDifferenceObjectSaving(ModelDifferenceObject userModelDifferenceObject, ModelApplicationBase model) {
            var userStoreObject = ((UserModelDifferenceObject)userModelDifferenceObject);
            if (!userStoreObject.NonPersistent) {
                userModelDifferenceObject.CreateAspectsCore(model);
                base.OnDifferenceObjectSaving(userModelDifferenceObject, model);
            }
            CombineModelFromPermission(model);
        }

        public override void SaveDifference(ModelApplicationBase model) {
            if (SecuritySystem.Instance is {UserType: null})
                base.SaveDifference(model);
            else if ( SecuritySystem.CurrentUser != null)
                base.SaveDifference(model);
        }

        void CombineModelFromPermission(ModelApplicationBase model) {
            if (SecuritySystem.Instance is ISecurityComplex && IsGranted()) {
                var space = Application.CreateObjectSpace(typeof(ModelDifferenceObject));
                ModelDifferenceObject difference = GetDifferenceFromPermission((XPObjectSpace)space);
                if (difference != null) {
                    InterfaceBuilder.SkipAssemblyCleanup = true;
                    var master = new ModelLoader(difference.PersistentApplication.ExecutableName, XafTypesInfo.Instance).GetMasterModel(Application,true,info => info.AssignAsInstance());
                    InterfaceBuilder.SkipAssemblyCleanup = false;
                    var diffsModel = difference.GetModel(master);
                    new ModelXmlReader().ReadFromModel(diffsModel, model);
                    difference.CreateAspectsCore(diffsModel);
                    space.SetModified(difference);
                    space.CommitChanges();
                }
            }
        }

        bool IsGranted() {
            if (((IRoleTypeProvider)SecuritySystem.Instance).IsNewSecuritySystem())
                return SecuritySystem.IsGranted(new ModelCombinePermissionRequest(ApplicationModelCombineModifier.Allow));
            return true;
        }

        private ModelDifferenceObject GetDifferenceFromPermission(XPObjectSpace space) {
            return new QueryModelDifferenceObject(space.Session).GetModelDifferences(GetNames()).SingleOrDefault();
        }

        private IEnumerable<string> GetNames() {
            if (SecuritySystem.CurrentUser is ISecurityUserWithRoles) {
                return ((ISecurityUserWithRoles)SecuritySystem.CurrentUser).GetPermissions().OfType<ModelCombineOperationPermission>().Select(permission => permission.Difference);
            }
            throw new NotImplementedException(SecuritySystem.CurrentUser.GetType().FullName);
        }

        public void Load() {
            var model = (ModelApplicationBase)Application.Model;
            var userDiff = model.LastLayer;
            ModelApplicationHelper.RemoveLayer(model);
            var modelDifferenceObjects = GetActiveRoleDifferenceObjects(DeviceCategory.All);
            if (DeviceModelsEnabled)
                modelDifferenceObjects=modelDifferenceObjects.Concat(GetActiveRoleDifferenceObjects(Application.GetDeviceCategory()));
            foreach (var roleModel in modelDifferenceObjects)
                roleModel.GetModel(model);
            ModelApplicationHelper.AddLayer(model, userDiff);
            LoadCore(userDiff);
        }

        private void LoadCore(ModelApplicationBase userDiff){
            var modelDifferenceObjects = GetActiveDifferenceObjects(DeviceCategory.All);
            if (DeviceModelsEnabled)
                modelDifferenceObjects=modelDifferenceObjects.Concat(GetActiveDifferenceObjects(Application.GetDeviceCategory()));
            if (!modelDifferenceObjects.Any()){
                SaveDifference(userDiff);
                return;
            }
            CombineWithActiveDifferenceObjects(userDiff, modelDifferenceObjects);
        }

        public override void Load(ModelApplicationBase model) {
            LoadCore(model);
        }

    }

}