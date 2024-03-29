﻿using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Utils;
using DevExpress.ExpressApp.Validation;
using DevExpress.Persistent.Validation;
using Xpand.Persistent.Base.Validation;
using Xpand.Utils.Helpers;
using System;
using Xpand.Persistent.Base.General;

namespace Xpand.ExpressApp.Validation.Controllers {
    [ModelAbstractClass]
    public interface IModelMemberPasswordScore:IModelMember {    
        [Category("eXpand")]
        [Description("If bigger is considered valid")]
        PasswordScore? PasswordScore { get; set; }
    }

    public class PasswordScoreController:ObjectViewController,IModelExtender, IPasswordScoreController {
        IEnumerable<IModelMemberPasswordScore> _modelMemberPasswordScores;

        protected override void OnActivated() {
            base.OnActivated();
            _modelMemberPasswordScores = View.Model.ModelClass.AllMembers.Cast<IModelMemberPasswordScore>().Where(member => member.PasswordScore != null);
            if (_modelMemberPasswordScores.Any()) {
                Frame.GetController<PersistenceValidationController>(controller => controller.ContextValidating += OnContextValidating);
            }
        }

        void OnContextValidating(object sender, ContextValidatingEventArgs contextValidatingEventArgs) {
            if (contextValidatingEventArgs.Context==ContextIdentifier.Save.ToString()) {
                Validator.GetService(Site).ValidationCompleted += RuleSetOnValidationCompleted;
            }
        }

        protected override void OnDeactivated() {
            base.OnDeactivated();
            Frame.GetController<PersistenceValidationController>(controller => controller.ContextValidating -= OnContextValidating);
        }

        void RuleSetOnValidationCompleted(object sender, ValidationCompletedEventArgs args) {
            Validator.GetService(Site).ValidationCompleted -= RuleSetOnValidationCompleted;
            var ruleSetValidationResult = new RuleSetValidationResult();
            var validationException = args.Exception;
            if (validationException != null)
                ruleSetValidationResult = validationException.Result;
            foreach (var modelMemberPasswordScore in _modelMemberPasswordScores) {
                var password = View.ObjectTypeInfo.FindMember(modelMemberPasswordScore.Name).GetValue(View.CurrentObject);
                var passwordScore = PasswordAdvisor.CheckStrength(password +"");
                if (passwordScore<modelMemberPasswordScore.PasswordScore) {
                    var messageTemplate = String.Format(CaptionHelper.GetLocalizedText(XpandValidationModule.XpandValidation, "PasswordScoreFailed"), modelMemberPasswordScore.Name, passwordScore, modelMemberPasswordScore.PasswordScore);
                    var validationResult = Validator.GetService(Site).NewRuleSetValidationMessageResult(ObjectSpace, messageTemplate, ContextIdentifier.Save,View.CurrentObject,View.ObjectTypeInfo.Type);
                    ruleSetValidationResult.AddResult(validationResult.Results.First());
                    args.Handled = true;
                }
            }
            if (args.Handled)
                throw validationException??new ValidationException(ruleSetValidationResult);
        }

        public void ExtendModelInterfaces(ModelInterfaceExtenders extenders) {
            extenders.Add<IModelMember,IModelMemberPasswordScore>();
        }
    }
}
