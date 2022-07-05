﻿using System;
using System.Diagnostics.CodeAnalysis;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Utils;
using DevExpress.ExpressApp.Xpo;
using DevExpress.Persistent.Base;
using Xpand.Persistent.Base.General;

namespace Xpand.ExpressApp.PropertyEditors {
    public interface ISupportControl {
        object Control { get; set; }
    }
    public interface ISupportEditControl {
        ISupportControl GetControl();
    }

    public class SerializableObjectPropertyEditorBuilder {
        PropertyEditor _propertyEditor;

        public static SerializableObjectPropertyEditorBuilder Create() {
            return new SerializableObjectPropertyEditorBuilder();
        }

        public SerializableObjectPropertyEditorBuilder WithPropertyEditor(PropertyEditor propertyEditor) {
            _propertyEditor = propertyEditor;
            return this;
        }
        void OnControlCreated(object sender, EventArgs eventArgs) {
            UpdateEditor(((ISupportEditControl)_propertyEditor).GetControl());
        }

        void OnValueStoring(object sender, ValueStoringEventArgs valueStoringEventArgs) {
            _propertyEditor.PropertyValue = _parameter.CurrentValue;
        }

        void OnCurrentObjectChanged(object sender, EventArgs eventArgs) {
            IObjectSpace objectSpace = XPObjectSpace.FindObjectSpaceByObject(_propertyEditor.CurrentObject);
            objectSpace.ObjectChanged += ObjectSpaceOnObjectChanged;
            UpdateEditor(((ISupportEditControl)_propertyEditor).GetControl());
        }
        public void Build(Func<PropertyEditor, object> findControl) {
            _findControl = findControl;
            _propertyEditor.CurrentObjectChanged += OnCurrentObjectChanged;
            _propertyEditor.ValueStoring += OnValueStoring;
            _propertyEditor.ControlCreated += OnControlCreated;
        }
        void ObjectSpaceOnObjectChanged(object sender, ObjectChangedEventArgs objectChangedEventArgs) {
            UpdateEditor(((ISupportEditControl)_propertyEditor).GetControl());
        }
        MyParameter _parameter;
        PropertyEditor _detailViewItems;
        Func<XafApplication> _getApplicationAction;
        Func<PropertyEditor, object> _findControl;

        sealed class MyParameter : ParameterBase {
            private object _currentValue;

            public MyParameter(string name, Type valueType, object value = null)
                : base(name, valueType) {
                Visible = false;
                _currentValue = value;
            }
            protected override void SetCurrentValue(object value) {
                _currentValue = value;
            }
            protected override object GetCurrentValue() { return _currentValue; }
            public override bool IsReadOnly => false;

            public object CurrentValue {
                get => _currentValue;
                set {
                    _currentValue = value;
                    if ((_currentValue is DateTime time) && (time == DateTime.MinValue)) {
                        _currentValue = null;
                    }
                }
            }
        }
        [SuppressMessage("Design", "XAF0012:Avoid calling the XafApplication.CreateObjectSpace() method without Type parameter")]
        void UpdateEditor(ISupportControl supportControl) {
            if (supportControl == null)
                return;
            bool isChanged = false;
            var memberType = GetMemberType() ?? typeof(object);
            bool editObjectChanged = (_parameter != null) && (_parameter.Type != memberType);
            if (_propertyEditor.CurrentObject != null) {
                if ((_parameter == null) || (editObjectChanged) || supportControl.Control == null) {
                    var application = _getApplicationAction.Invoke();
                    isChanged = true;
                    _parameter = new MyParameter(memberType.Name, memberType) { Visible = true };
                    var paramList = new ParameterList { _parameter };
                    ParametersObject parametersObject = ParametersObject.CreateBoundObject(paramList);
                    DetailView detailView = parametersObject.CreateDetailView(application.CreateObjectSpace(), application, true);
                    detailView.ViewEditMode = GetViewEditMode();
                    _detailViewItems = ((PropertyEditor)detailView.Items[0]);
                    _detailViewItems.CreateControl();
                    _detailViewItems.ControlValueChanged += detailViewItems_ControlValueChanged;
                }
                _parameter.CurrentValue = _propertyEditor.PropertyValue;
            }
            if ((isChanged || (supportControl.Control == null)) && (_detailViewItems != null)) {
                _detailViewItems.Refresh();
                supportControl.Control = _findControl.Invoke(_detailViewItems);
            }
        }

        ViewEditMode GetViewEditMode() => _propertyEditor.View is DetailView view ? view.ViewEditMode : throw new NotImplementedException();

        Type GetMemberType() {
            string propertyName = _propertyEditor.MemberInfo.FindAttribute<PropertyEditorProperty>().PropertyName;
            object ownerInstance = _propertyEditor.MemberInfo.GetOwnerInstance(_propertyEditor.CurrentObject);
            if (ownerInstance != null) {
                IMemberInfo memberInfo = XafTypesInfo.Instance.FindTypeInfo(ownerInstance.GetType()).FindMember(propertyName);
                return (Type)memberInfo.GetValue(ownerInstance);
            }
            return null;
        }

        void detailViewItems_ControlValueChanged(object sender, EventArgs e) {
            _propertyEditor.PropertyValue = _detailViewItems.ControlValue;
        }

        public SerializableObjectPropertyEditorBuilder WithApplication(Func<XafApplication> getApplicationAction) {
            _getApplicationAction = getApplicationAction;
            return this;
        }
    }
}