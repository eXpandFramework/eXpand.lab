﻿using System;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;
using DevExpress.Xpo;
using Xpand.Extensions.XAF.Attributes.Custom;
using Xpand.Extensions.XAF.Xpo.ValueConverters;
using Xpand.Persistent.Base.General.CustomAttributes;
using Xpand.Persistent.Base.JobScheduler.Triggers;

namespace Xpand.Persistent.BaseImpl.JobScheduler.Triggers {
    [NavigationItem("JobScheduler")]
    [System.ComponentModel.DisplayName("SimpleTrigger")]
    public class XpandSimpleTrigger : XpandJobTrigger, IXpandSimpleTrigger {
        public XpandSimpleTrigger(Session session)
            : base(session) {
        }
        private SimpleTriggerMisfireInstruction _misfireInstruction;
        void IXpandSimpleTrigger.SetFinalFireTimeUtc(DateTimeOffset? dateTime) {
            _finalFireTimeUtc = dateTime;
        }

        [DisplayDateAndTime]
        public DateTime Now => DateTime.UtcNow;

        [Tooltip("Get or set the instruction the IScheduler should be given for handling misfire situations for this Trigger- the concrete Trigger type that you are using will have defined a set of additional MISFIRE_INSTRUCTION_XXX constants that may be passed to this method. ")]
        public SimpleTriggerMisfireInstruction MisfireInstruction {
            get => _misfireInstruction;
            set => SetPropertyValue("MisfireInstruction", ref _misfireInstruction, value);
        }
        private int? _repeatCount;
        [Tooltip("Get or set thhe number of times the SimpleTrigger should repeat, after which it will be automatically deleted. ")]
        [RuleRequiredField(TargetCriteria = "RepeatInterval is not null")]
        public int? RepeatCount {
            get => _repeatCount;
            set => SetPropertyValue("RepeatCount", ref _repeatCount, value);
        }
        private TimeSpan? _repeatInterval;
        [RuleRequiredField(TargetCriteria = "RepeatCount is not null")]
        [Tooltip("Get or set the the time interval at which the SimpleTrigger should repeat. ")]
        public TimeSpan? RepeatInterval {
            get => _repeatInterval;
            set => SetPropertyValue("RepeatInterval", ref _repeatInterval, value);
        }
        private int _timesTriggered;
        [Tooltip("Get or set the number of times the SimpleTrigger has already fired. ")]
        public int TimesTriggered {
            get => _timesTriggered;
            set => SetPropertyValue("TimesTriggered", ref _timesTriggered, value);
        }
        [Persistent("FinalFireTimeUtc")]
        [ValueConverter(typeof(SqlDateTimeOffSetOverFlowValueConverter))]
        private DateTimeOffset? _finalFireTimeUtc;
        [Tooltip("Returns the final UTC time at which the SimpleTrigger will fire, if repeatCount is RepeatIndefinitely, null will be returned. Note that the return time may be in the past. ")]

        [DisplayDateAndTime]
        public DateTimeOffset? FinalFireTimeUtc => _finalFireTimeUtc;
    }
}