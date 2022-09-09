﻿using System.Diagnostics.CodeAnalysis;
using DevExpress.Persistent.Validation;
using DevExpress.Xpo;
using Xpand.Persistent.Base;

namespace Xpand.ExpressApp.ModelDifference.DataStore.BaseObjects {
    [RuleCombinationOfPropertiesIsUnique("AspectObject_Name_MDO",DefaultContexts.Save,"Name,ModelDifferenceObject" )]
    [SuppressMessage("Design", "XAF0023:Do not implement IObjectSpaceLink in the XPO types")]
    public class AspectObject:XpandBaseCustomObject {
        private const string XmlDefaultValue = @"<?xml version=""1.0"" ?><Application />";
        public AspectObject(Session session) : base(session) {
        }
        private string _name;

        public override void AfterConstruction(){
            base.AfterConstruction();
            if (string.IsNullOrWhiteSpace(Xml))
                Xml = XmlDefaultValue;
        }

        [RuleRequiredField]
        public string Name {
            get { return _name; }
            set { SetPropertyValue("Name", ref _name, value); }
        }
        private ModelDifferenceObject _modelDifferenceObject;

        [Association("ModelDifferenceObject-AspectObjects")]
        public ModelDifferenceObject ModelDifferenceObject {
            get { return _modelDifferenceObject; }
            set { SetPropertyValue("ModelDifferenceObject", ref _modelDifferenceObject, value); }
        }
        private string _xml;
        [Size(SizeAttribute.Unlimited)]
        public string Xml {
            get { return _xml; }
            set{
                SetPropertyValue("Xml", ref _xml, value);
                ModelDifferenceObject?.NotifyXmlContent();
            }
        }
    }
}