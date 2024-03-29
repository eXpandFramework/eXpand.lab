﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using Xpand.Utils.Helpers;
using Fasterflect;
using Xpand.Extensions.ReflectionExtensions;
using Xpand.Extensions.TypeExtensions;
using Xpand.Persistent.Base.General;
using Platform = Xpand.Extensions.XAF.XafApplicationExtensions.Platform;

namespace Xpand.Persistent.Base.ModelAdapter{
    public class InterfaceBuilderData{
        public InterfaceBuilderData(Type componentType){
            ComponentType = componentType;
            BaseInterface = typeof(IModelNodeEnabled);
        }

        public Type ComponentType { get; }

        public Func<DynamicModelPropertyInfo, bool> Act { get; set; }

        [Description("The interface from which all autogenerated interfaces derive. Default is the IModelNodeEnabled")]
        public Type BaseInterface { get; set; }

        public List<Type> ReferenceTypes { get; } = new();

        public Type RootBaseInterface { get; set; }

        public bool IsAbstract { get; set; }
    }

    public class InterfaceBuilder{
        readonly ModelInterfaceExtenders _extenders;
        readonly List<Type> _usingTypes;

        public InterfaceBuilder(ModelInterfaceExtenders extenders)
            : this(){
            _extenders = extenders;
        }

        InterfaceBuilder(){
            _usingTypes = new List<Type>();
            
        }

        static bool? _runtimeMode;
        private static bool _skipAssemblyCleanup;

        public static bool RuntimeMode{
            get{
                if (_runtimeMode == null){
                    var devProcesses = new[]{".ExpressApp.ModelEditor", "devenv","Xpand.XAF.ModelEditor"};
                    var processName = Process.GetCurrentProcess().ProcessName;
                    var isInProcess = devProcesses.Any(s => processName.IndexOf(s, StringComparison.Ordinal) > -1);
                    _runtimeMode = !isInProcess && LicenseManager.UsageMode != LicenseUsageMode.Designtime&&!DesignerOnlyCalculator.IsRunFromDesigner;
                }
                return _runtimeMode.Value;
            }
            set => _runtimeMode = value;
        }

        public ModelInterfaceExtenders Extenders => _extenders;

        public static bool ExternalModelEditor => Process.GetCurrentProcess().ProcessName.IndexOf(".ExpressApp.ModelEditor", StringComparison.Ordinal) >
                                                  -1;


        public static bool SkipAssemblyCleanup {
            get => _skipAssemblyCleanup;
            set {
                _skipAssemblyCleanup = value;
                if (_skipAssemblyCleanup) {
                    var modelMapperModule = ApplicationHelper.Instance.Application.Modules.FirstOrDefault(m => m.Name=="ModelMapperModule");
                    if (modelMapperModule != null){
                        var typeMappingServiceType = modelMapperModule.GetType().Assembly
                            .GetType("Xpand.XAF.Modules.ModelMapper.Services.TypeMapping.TypeMappingService");
                        typeMappingServiceType.Method("Reset", Flags.StaticPublic).Call(null, true,Platform.Win);
                    }
                }
            }
        }


        public static bool IsDevMachine {
            get {
                try {
                    return  Environment.GetEnvironmentVariable("XpandDevMachine", EnvironmentVariableTarget.User).Change<bool>();
                }
                catch (System.Security.SecurityException) {
                    return false;
                }
            }
        }

        public static bool IsDBUpdater => Process.GetCurrentProcess().ProcessName.StartsWith("DBUpdater"+AssemblyInfo.VSuffix);







        [AttributeUsage(AttributeTargets.Interface)]
        public class ClassNameAttribute : Attribute{
            public ClassNameAttribute(string typeName){
                TypeName = typeName;
            }

            public string TypeName { get; }
        }


        string TypeToString(Type type){
            return HelperTypeGenerator.TypeToString(type, _usingTypes, true);
        }

        public void ExtendInterface(Type targetType, Type extenderType, Assembly assembly){
            extenderType = CalcType(extenderType, assembly);
            targetType = CalcType(targetType, assembly);
            _extenders.Add(targetType, extenderType);
        }

        public Type CalcType(Type extenderType, Assembly assembly){
            if (!extenderType.IsInterface){
                var type = assembly.GetTypes().FirstOrDefault(type1 => AttributeLocatorMatches(extenderType, type1));
                if (type == null){
                    throw new NullReferenceException("Cannot locate the dynamic interface for " + extenderType.FullName);
                }
                return type;
            }
            return extenderType;
        }

        bool AttributeLocatorMatches(Type extenderType, Type type1){
            return
                type1.GetCustomAttributes(typeof(Attribute), false)
                    .Any(attribute => AttributeMatch(extenderType, (Attribute) attribute));
        }

        bool AttributeMatch(Type extenderType, Attribute attribute){
            Type type = attribute.GetType();
            if (type.Name == nameof(ClassNameAttribute)){
                var value = attribute.GetPropertyValue("TypeName") + "";
                value = value.Substring(1 + value.LastIndexOf(".", StringComparison.Ordinal));
                return extenderType.Name == value;
            }
            return false;
        }

        public void ExtendInterface<TTargetInterface, TComponent>(Assembly assembly) where TComponent : class{
            ExtendInterface(typeof(TTargetInterface), typeof(TComponent), assembly);
        }



        public string GeneratedDisplayNameCode(string arg3){
            var interfaceBuilder = new InterfaceBuilder();
            return $@"[{interfaceBuilder.TypeToString(typeof(ModelDisplayNameAttribute))}(""{arg3}"")]";
        }
    }

    public class ModelValueCalculatorWrapperAttribute : Attribute{
        public ModelValueCalculatorWrapperAttribute(Type calculatorType){
            CalculatorType = calculatorType;
        }

        public ModelValueCalculatorWrapperAttribute(ModelValueCalculatorAttribute modelValueCalculatorAttribute,
            Type calculatorType)
            : this(calculatorType){
            LinkValue = modelValueCalculatorAttribute.LinkValue;
            NodeName = modelValueCalculatorAttribute.NodeName;
            if (modelValueCalculatorAttribute.NodeType != null)
                NodeTypeName = modelValueCalculatorAttribute.NodeType.Name;
            PropertyName = modelValueCalculatorAttribute.PropertyName;
        }

        public Type CalculatorType { get; }

        public string LinkValue { get; }

        public string NodeName { get; }

        public string NodeTypeName { get; }

        public string PropertyName { get; }
    }

    public static class InterfaceBuilderExtensions{
        public static Type MakeNullAble(this Type generic, params Type[] args){
            return args[0].IsNullableType() ? args[0] : typeof(Nullable<>).MakeGenericType(args);
        }


        public static bool FilterAttributes(this DynamicModelPropertyInfo info, Type[] attributes){
            return attributes.SelectMany(type => info.GetCustomAttributes(type, false)).Any();
        }



        public static readonly HashSet<string> ExcludedReservedNames = new() {"IsReadOnly"};



        public static bool Filter(this DynamicModelPropertyInfo info, Type componentBaseType,
            Type[] filteredPropertyBaseTypes, Type[] attributes){
            return info.IsBrowseAble() && info.HasAttributes(attributes) && !ExcludedReservedNames.Contains(info.Name) &&
                   FilterCore(info, componentBaseType, filteredPropertyBaseTypes);
        }

        static bool FilterCore(DynamicModelPropertyInfo info, Type componentBaseType,
            IEnumerable<Type> filteredPropertyBaseTypes){
            var behaveLikeValueType = info.PropertyType.BehaveLikeValueType();
            var isBaseViewProperty = componentBaseType.IsAssignableFrom(info.DeclaringType);
            var propertyBaseTypes = filteredPropertyBaseTypes as Type[] ?? filteredPropertyBaseTypes.ToArray();
            var filterCore = propertyBaseTypes.Any(type => type.IsAssignableFrom(info.PropertyType)) ||
                             behaveLikeValueType;
            var core = propertyBaseTypes.Any(type => type.IsAssignableFrom(info.DeclaringType)) && behaveLikeValueType;
            return isBaseViewProperty ? filterCore : core;
        }

        public static bool IsValidEnum(this Type propertyType, object value){
            return !propertyType.IsEnum || Enum.IsDefined(value.GetType(), value);
        }

        public static bool IsStruct(this Type type){
            if (type.IsNullableType())
                type = type.GetGenericArguments()[0];
            return type.IsValueType && !type.IsEnum && !type.IsPrimitive;
        }

        public static bool IsBrowseAble(this PropertyInfo propertyInfo){
            return
                propertyInfo.GetCustomAttributes(typeof(BrowsableAttribute), false)
                    .OfType<BrowsableAttribute>()
                    .All(o => o.Browsable);
        }

        public static bool BehaveLikeValueType(this Type type){
            return type == typeof(string) || type.IsValueType;
        }

        public static IEnumerable<PropertyInfo> GetValidProperties(this Type type, params Type[] attributes){
            if (type != null){
                var propertyInfos =
                    type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
                        .Distinct(new PropertyInfoEqualityComparer());
                var infos = propertyInfos.Where(info => HasAttributes(info, attributes)).ToArray();
                if (infos.Any(info => info.Name.StartsWith("TextFormatSt"))){
                    // PropertyInfo[] array;
                    // Debug.Print(array.Length.ToString());
                }
                return infos.Where(IsValidProperty).ToArray();
            }
            return new PropertyInfo[0];
        }

        static bool HasAttributes(this PropertyInfo propertyInfo, params Type[] attributes){
            var hasAttributes = (attributes == null || attributes == Type.EmptyTypes) || (attributes.Any()) ||
                                (attributes.All(type => propertyInfo.GetCustomAttributes(type, false).Any()));
            return hasAttributes;
        }

        class PropertyInfoEqualityComparer : IEqualityComparer<PropertyInfo>{
            public bool Equals(PropertyInfo x, PropertyInfo y){
                return y != null && (x != null && x.Name.Equals(y.Name));
            }

            public int GetHashCode(PropertyInfo obj){
                return obj.Name.GetHashCode();
            }
        }

        static bool IsValidProperty(PropertyInfo info){
            if (IsObsolete(info))
                return false;
            var isNotEnumerable = !typeof(IEnumerable).IsAssignableFrom(info.PropertyType)||typeof(string)==info.PropertyType;
            return (!info.PropertyType.BehaveLikeValueType() ||
                   info.GetSetMethod() != null && info.GetGetMethod() != null)&&(isNotEnumerable);
        }

        static bool IsObsolete(PropertyInfo info){
            return info.GetCustomAttributes(typeof(ObsoleteAttribute), true).Length > 0;
        }

        public static void SetBrowsable(this DynamicModelPropertyInfo info, Dictionary<string, bool> propertyNames){
            if (propertyNames.ContainsKey(info.Name)){
                info.RemoveAttributes(typeof(BrowsableAttribute));
                info.AddAttribute(new BrowsableAttribute(propertyNames[info.Name]));
            }
        }

        public static void SetCategory(this DynamicModelPropertyInfo info, Dictionary<string, string> propertyNames){
            if (propertyNames.ContainsKey(info.Name)){
                info.RemoveAttributes(typeof(BrowsableAttribute));
                info.AddAttribute(new CategoryAttribute(propertyNames[info.Name]));
            }
        }

        public static void CreateValueCalculator(this DynamicModelPropertyInfo info,
            IModelValueCalculator modelValueCalculator){
            CreateValueCalculatorCore(info);
            info.AddAttribute(new ModelValueCalculatorWrapperAttribute(modelValueCalculator.GetType()));
        }

        public static void CreateValueCalculator(this DynamicModelPropertyInfo info, string expressionPath = null){
            info.AddAttribute(new BrowsableAttribute(false));
        }

        static void CreateValueCalculatorCore(DynamicModelPropertyInfo info){
            info.RemoveAttributes(typeof(DefaultValueAttribute));
            info.AddAttribute(new ReadOnlyAttribute(true));
        }

        public static void SetDefaultValues(this DynamicModelPropertyInfo info, Dictionary<string, object> propertyNames){
            if (propertyNames.ContainsKey(info.Name)){
                info.RemoveAttributes(typeof(DefaultValueAttribute));
                info.AddAttribute(new DefaultValueAttribute(propertyNames[info.Name]));
            }
        }
    }

    public sealed class DynamicModelPropertyInfo : PropertyInfo{
        readonly List<object> _attributesCore = new();
        readonly PropertyInfo _targetPropertyInfo;
        private Type _propertyType;
        private string _name;

        public DynamicModelPropertyInfo(string name, Type propertyType, Type declaringType, bool canRead, bool canWrite,
            PropertyInfo targetPropertyInfo){
            _name = name;
            _propertyType = propertyType;
            DeclaringType = declaringType;
            CanRead = canRead;
            CanWrite = canWrite;
            _targetPropertyInfo = targetPropertyInfo;
            var collection = targetPropertyInfo.GetCustomAttributes(false).Where(o => !(o is DefaultValueAttribute));
            _attributesCore.AddRange(collection);
        }


        public override string Name => _name;

        public override Type PropertyType => _propertyType;

        public override Type DeclaringType { get; }

        public override bool CanRead { get; }

        public override bool CanWrite { get; }

        public override PropertyAttributes Attributes => throw new NotImplementedException();

        public override Type ReflectedType => _targetPropertyInfo.ReflectedType;

        public override string ToString(){
            return _targetPropertyInfo.ToString();
        }

        public override MethodInfo[] GetAccessors(bool nonPublic){
            return _targetPropertyInfo.GetAccessors(nonPublic);
        }

        public override MethodInfo GetGetMethod(bool nonPublic){
            return _targetPropertyInfo.GetSetMethod(nonPublic);
        }

        public override ParameterInfo[] GetIndexParameters(){
            return _targetPropertyInfo.GetIndexParameters();
        }

        public override MethodInfo GetSetMethod(bool nonPublic){
            return _targetPropertyInfo.GetSetMethod(nonPublic);
        }

        public override object GetValue(object obj, BindingFlags invokeAttr, Binder binder, object[] index,
            CultureInfo culture){
            return _targetPropertyInfo.GetValue(obj, invokeAttr, binder, index, culture);
        }

        public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, object[] index,
            CultureInfo culture){
            _targetPropertyInfo.SetValue(obj, value, invokeAttr, binder, index, culture);
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit){
            return _attributesCore.Where(attributeType.IsInstanceOfType).ToArray();
        }

        public override object[] GetCustomAttributes(bool inherit){
            return _attributesCore.ToArray();
        }

        public override bool IsDefined(Type attributeType, bool inherit){
            return _targetPropertyInfo.IsDefined(attributeType, inherit);
        }


        public void SetName(string name) {
            _name = name;
        }

        public void SetPropertyType(Type type) {
            _propertyType = type;
        }

        public void RemoveAttribute(Attribute attribute){
            _attributesCore.Remove(attribute);
        }

        public void AddAttribute(Attribute attribute){
            _attributesCore.Add(attribute);
        }

        public void RemoveInvalidTypeConverterAttributes(string nameSpace){
            var customAttributes =
                GetCustomAttributes(typeof(TypeConverterAttribute), false).OfType<TypeConverterAttribute>();
            var attributes = customAttributes.Where(attribute => attribute.ConverterTypeName.StartsWith(nameSpace));
            foreach (var customAttribute in attributes){
                _attributesCore.Remove(customAttribute);
            }
        }

        public void RemoveAttributes(Type type){
            foreach (var customAttribute in GetCustomAttributes(type, false).OfType<Attribute>()){
                _attributesCore.Remove(customAttribute);
            }
        }
    }
}