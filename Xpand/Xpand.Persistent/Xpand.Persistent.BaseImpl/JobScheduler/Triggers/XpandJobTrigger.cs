﻿using System;
using System.Collections.Generic;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;
using DevExpress.Xpo;
using Xpand.ExpressApp.Attributes;
using Xpand.Extensions.XAF.Attributes.Custom;
using Xpand.Extensions.XAF.Xpo.ValueConverters;
using Xpand.Persistent.Base.General;
using Xpand.Persistent.Base.General.CustomAttributes;
using Xpand.Persistent.Base.JobScheduler;
using Xpand.Persistent.Base.JobScheduler.Calendars;
using Xpand.Persistent.Base.JobScheduler.Triggers;
using Xpand.Persistent.BaseImpl.JobScheduler.Calendars;
using Xpand.Xpo;

namespace Xpand.Persistent.BaseImpl.JobScheduler.Triggers {
    [Appearance("Disable_Name_For_XpandJobTrigger_ExistingObjects", AppearanceItemType.ViewItem, "IsNewObject=false", TargetItems = "Name", Enabled = false)]
    public abstract class XpandJobTrigger : XpandCustomObject, IXpandJobTrigger, IFastManyToMany, IRequireSchedulerInitialization {
        protected XpandJobTrigger(Session session)
            : base(session) {
        }
        public override void AfterConstruction() {
            base.AfterConstruction();
            Name = "tr";
            StartTimeUtc = DateTime.UtcNow;
        }

        private string _name;
        [RuleRequiredField]
        public string Name {
            get => _name;
            set => SetPropertyValue("Name", ref _name, value);
        }


        private string _description;
        [Size(SizeAttribute.Unlimited)]
        public string Description {
            get => _description;
            set => SetPropertyValue("Description", ref _description, value);
        }
        [Tooltip("Get or set the ICalendar with the given name with this Trigger. Use null when setting to dis-associate a Calendar. ")]
        private XpandTriggerCalendar _calendar;
        [ProvidedAssociation("XpandTriggerCalendar-XpandJobTriggers")]
        public XpandTriggerCalendar Calendar {
            get => _calendar;
            set => SetPropertyValue("Calendar", ref _calendar, value);
        }
        ITriggerCalendar IXpandJobTrigger.Calendar {
            get => Calendar;
            set => Calendar = value as XpandTriggerCalendar;
        }



        private DateTime? _endTimeUtc;
        [Tooltip("Returns the date/time on which the trigger must stop firing. This defines the final boundary for trigger firings 舒 the trigger will not fire after to this date and time. If this value is null, no end time boundary is assumed, and the trigger can continue indefinitely. Sets the date/time on which the trigger must stop firing. This defines the final boundary for trigger firings 舒 the trigger will not fire after to this date and time. If this value is null, no end time boundary is assumed, and the trigger can continue indefinitely. ")]
        [DisplayDateAndTime]
        public DateTime? EndTimeUtc {
            get => _endTimeUtc;
            set => SetPropertyValue("EndTimeUtc", ref _endTimeUtc, value);
        }
        private DateTime _startTimeUtc;
        [Tooltip(@"The time at which the trigger's scheduling should start. May or may not be the first actual fire time of the trigger, depending upon the type of trigger and the settings of the other properties of the trigger. However the first actual first time will not be before this date. 
Remarks:
Setting a value in the past may cause a new trigger to compute a first fire time that is in the past, which may cause an immediate misfire of the trigger.")]
        //        [RuleRequiredField]
        [DisplayDateAndTime]
        [ValueConverter(typeof(SqlDateTimeOverFlowValueConverter))]
        public DateTime StartTimeUtc {
            get => _startTimeUtc;
            set => SetPropertyValue("StartTimeUtc", ref _startTimeUtc, value);
        }

        private TriggerPriority _priority=TriggerPriority.Default;
        public TriggerPriority Priority {
            get => _priority;
            set => SetPropertyValue("Priority", ref _priority, value);
        }


        [Association("JobDetailTriggerLink-JobDetails"), Aggregated]
        protected IList<JobDetailTriggerLink> JobDetailTriggerLinks => GetList<JobDetailTriggerLink>();

        [ManyToManyAlias("JobDetailTriggerLinks", "JobDetail")]
        public IList<XpandJobDetail> JobDetails => GetList<XpandJobDetail>();

        IList<IXpandJobDetail> IJobDetails.JobDetails => new ListConverter<IXpandJobDetail, XpandJobDetail>(JobDetails);

        [Association("JobSchedulerGroupTriggerLink-JobSchedulerGroups"), Aggregated]
        protected IList<JobSchedulerGroupTriggerLink> JobSchedulerGroupTriggerLinks => GetList<JobSchedulerGroupTriggerLink>();

        [ManyToManyAlias("JobSchedulerGroupTriggerLinks", "JobSchedulerGroup")]
        public IList<JobSchedulerGroup> JobSchedulerGroups => GetList<JobSchedulerGroup>();

        [Association("XpandJobTrigger-JobTriggerTriggerListenerTriggerLinks"), AggregatedAttribute]
        protected IList<JobTriggerTriggerListenerTriggerLink> JobTriggerTriggerListenerTriggerLinks => GetList<JobTriggerTriggerListenerTriggerLink>();

        [ManyToManyAlias("JobTriggerTriggerListenerTriggerLinks", "TriggerListenerTrigger")]
        public IList<TriggerListenerTrigger> TriggerListenerTriggers => GetList<TriggerListenerTrigger>();

        IList<ITriggerListenerTrigger> IXpandJobTrigger.TriggerListenerTriggers => new ListConverter<ITriggerListenerTrigger, TriggerListenerTrigger>(TriggerListenerTriggers);
    }
}