using System.ComponentModel;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.Persistent.Base;
using DevExpress.Xpo;
using Xpand.Extensions.XAF.ActionExtensions;
using Xpand.Persistent.Base.General.Model;
using Xpand.Persistent.Base.Xpo.MetaData;
using Xpand.Xpo;

namespace Xpand.Persistent.Base.General.Controllers.Actions{
    [NonPersistent]
    public class ModelConfiguration:XpandCustomObject {
        public ModelConfiguration(Session session) : base(session){
        }
    }

    public interface IModelViewConfigurationView{
        [DataSourceProperty(ModelApplicationViewsDomainLogic.DetailViews)]
        [Category(AttributeCategoryNameProvider.Xpand)]
        IModelDetailView ConfigurationView { get; set; }
    }

    public class ModelConfigurationController:ModifyModelActionControllerBase,IModelExtender{
        protected override void OnActivated(){
            base.OnActivated();
            var choiceActionItem = ActionModifyModelController.ModifyModelAction.Items.FindItemByID(ModifyModelActionChoiceItemsUpdater.ChangeViewModel);
            if (choiceActionItem != null){
                choiceActionItem.Active.BeginUpdate();
                choiceActionItem.Active["ModelNotConfigured"] = ((IModelViewConfigurationView)View.Model).ConfigurationView != null;
                choiceActionItem.Active.EndUpdate();
            }

            ActionModifyModelController.ModifyModelAction.ItemsChanged+=ModifyModelActionOnItemsChanged;
        }

        protected override void OnDeactivated(){
            base.OnDeactivated();
            ActionModifyModelController.ModifyModelAction.ItemsChanged -= ModifyModelActionOnItemsChanged;
        }

        private void ModifyModelActionOnItemsChanged(object sender, ItemsChangedEventArgs e){
            foreach (var itemChangesType in e.ChangedItemsInfo.Where(pair => pair.Value==ChoiceActionItemChangesType.Add)){
                var choiceActionItem = itemChangesType.Key as ChoiceActionItem;
                if (choiceActionItem != null && choiceActionItem.Id==ModifyModelActionChoiceItemsUpdater.ChangeViewModel) {
                    choiceActionItem.Active.BeginUpdate();
                    choiceActionItem.Active["ModelNotConfigured"] = ((IModelViewConfigurationView)View.Model).ConfigurationView != null;
                    choiceActionItem.Active.EndUpdate();
                }
            }
        }

        protected override void ModifyModelActionOnExecute(object sender, SingleChoiceActionExecuteEventArgs e){
            if (e.SelectedChoiceActionItem.Id == ModifyModelActionChoiceItemsUpdater.ChangeViewModel){
                var showViewParameters = e.ShowViewParameters;
                var modelDetailView = ((IModelViewConfigurationView)View.Model).ConfigurationView;
                if (modelDetailView!=null){
                    var objectSpace = Application.CreateObjectSpace(modelDetailView.ModelClass.TypeInfo.Type);
                    var changeViewModel = objectSpace.CreateObject<ModelConfiguration>();
                    showViewParameters.CreatedView = Application.CreateDetailView(objectSpace, modelDetailView, true,
                        changeViewModel);
                    showViewParameters.TargetWindow = TargetWindow.NewModalWindow;
                    var dialogController = e.Application().CreateController<DialogController>();
                    var callingFrame = Frame;
                    dialogController.Accepting += (o, args) =>{
                        var modelMemberInfoController =dialogController.Frame.GetController<XpandModelMemberInfoController>();
                        var currentObject = dialogController.Frame.View.CurrentObject;
                        modelMemberInfoController.SynchronizeModel(callingFrame.View.Model,currentObject);
                    };
                    dialogController.Disposed += (o, args) => {
                        callingFrame.SetView(Application.CreateView(callingFrame.View.Model));
                    };
                    showViewParameters.Controllers.Add(dialogController);
                }
            }
        }

        public void ExtendModelInterfaces(ModelInterfaceExtenders extenders){
            extenders.Add<IModelView,IModelViewConfigurationView>();
        }
    }
}