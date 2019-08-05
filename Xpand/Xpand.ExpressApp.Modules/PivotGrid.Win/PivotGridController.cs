﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.PivotGrid;
using DevExpress.ExpressApp.PivotGrid.Win;
using DevExpress.ExpressApp.Win.Layout;
using DevExpress.Utils;
using DevExpress.XtraLayout;
using DevExpress.XtraLayout.Utils;
using DevExpress.XtraPivotGrid;
using System.Linq;
using DevExpress.ExpressApp.Model;
using DevExpress.XtraEditors.Repository;
using Xpand.ExpressApp.PivotGrid.Win.Model;
using Xpand.ExpressApp.Win.SystemModule.ToolTip;
using ListView = DevExpress.ExpressApp.ListView;
using Fasterflect;
using Xpand.XAF.Modules.ModelMapper.Services;

namespace Xpand.ExpressApp.PivotGrid.Win {
    public abstract class PivotGridControllerBase : ViewController<ListView> {
        public PivotGridListEditor PivotGridListEditor => View?.Editor as PivotGridListEditor;

//        public IModelListViewOptionsPivotGrid Model {
//            get { return ((IModelListViewOptionsPivotGrid)View.Model); }
//        }

        protected override void OnActivated() {
            base.OnActivated();
            Active["ListEditorType"] = IsActive();
            if (Active && PivotGridListEditor != null) {
                PivotGridListEditor.ModelApplied+=PivotGridListEditorOnModelApplied;
            }
        }

        protected virtual bool IsActive() {
            return PivotGridListEditor != null;
        }

        protected override void OnDeactivated() {
            base.OnDeactivated();
            if (PivotGridListEditor != null) {
                PivotGridListEditor.ModelApplied -= PivotGridListEditorOnModelApplied;
            }
        }

        private void PivotGridListEditorOnModelApplied(object sender, EventArgs eventArgs){
            AttachToControlEvents();
        }

        protected abstract void AttachToControlEvents();

        protected LayoutControlItem PivotLayoutControlItem {
            get {
                var layoutControl = (XafLayoutControl)PivotGridListEditor.Control;
                return (LayoutControlItem)((LayoutGroup)layoutControl.Root.Items[0]).Items.FindByName("PivotGrid");
            }
        }
    }
    public class PivotGridController : PivotGridControllerBase {
        bool _inSelectionUpdate;
        bool _criteriaApplying;
        RuleCollector _ruleCollector;
        readonly Dictionary<string, IObjectToolTipController> _toolTipControllers = new Dictionary<string, IObjectToolTipController>();
        protected override void OnActivated() {
            base.OnActivated();
            if (PivotGridListEditor != null) {
                _ruleCollector = new RuleCollector(View.Model);
            }
        }

        protected override void AttachToControlEvents() {
            var pivotGridControl = PivotGridListEditor.PivotGridControl;
            foreach (var toolTipRule in _ruleCollector.ToolTipRules()) {
                var objectToolTipController = (IObjectToolTipController)toolTipRule.ToolTipController.CreateInstance(pivotGridControl);
                _toolTipControllers.Add(toolTipRule.GetValue<string>("Id"), objectToolTipController);
            }
            pivotGridControl.CustomSummary += PivotGridControlOnCustomSummary;
            pivotGridControl.CustomGroupInterval += PivotGridControlOnCustomGroupInterval;
            pivotGridControl.CustomFieldSort += PivotGridControlOnCustomFieldSort;
            pivotGridControl.CustomDrawCell += PivotGridControlOnCustomDrawCell;
            pivotGridControl.MouseMove += PivotGridControlOnMouseMove;
        }

        void PivotGridControlOnMouseMove(object sender, MouseEventArgs e) {

            var pivotGridControl = PivotGridListEditor.PivotGridControl;
            var pivotGridHitInfo = pivotGridControl.CalcHitInfo(e.Location);
            if (pivotGridHitInfo != null && pivotGridHitInfo.HitTest == PivotGridHitTest.Value) {
                var pivotGridField = pivotGridHitInfo.ValueInfo.Field;
                if (pivotGridField != null) {
                    var toolTipRule = _ruleCollector.ToolTipRules(pivotGridField, Frame).FirstOrDefault();
                    if (toolTipRule != null) {
                        var hitInfo = pivotGridControl.CalcHitInfo(e.Location);
                        var key = toolTipRule.GetValue<string>("Id");
                        var objectToolTipController = _toolTipControllers[key];
                        var value = hitInfo.ValueInfo.Value;
                        var isInstanceOfType = pivotGridField.DataType.IsInstanceOfType(value);
                        if (isInstanceOfType) {
                            Point location = e.Location;
                            location.Offset(20, 20);
                            objectToolTipController.ShowHint(value, location, ObjectSpace);
                        }
                    }
                } else {
                    foreach (var objectToolTipController in _toolTipControllers) {
                        if (objectToolTipController.Value.HintIsShown)
                            objectToolTipController.Value.HideHint(true);
                    }
                }
            }
        }

        void PivotGridControlOnCustomFieldSort(object sender, PivotGridCustomFieldSortEventArgs e) {
            var pivotFieldSortRules = _ruleCollector.FieldSortRules(e.Field, Frame);
            foreach (var pivotFieldSort in pivotFieldSortRules) {
                pivotFieldSort.Calculate(e);
            }
        }

        void PivotGridControlOnCustomGroupInterval(object sender, PivotCustomGroupIntervalEventArgs e) {
            var groupIntervalRules = _ruleCollector.GroupIntervalRules(e.Field, Frame);
            foreach (var intervalRule in groupIntervalRules) {
                intervalRule.Calculate(e);
            }
        }

        void PivotGridControlOnCustomSummary(object sender, PivotGridCustomSummaryEventArgs e) {
            throw new NotImplementedException();
//            var customSummaryType = ((IModelListViewOptionsPivotGrid)View.Model).OptionsPivotGrid.General.CustomSummaryType;
//            if (customSummaryType != null) {
//                var pivotCustomSummaryEvent = _ruleCollector.CreateInstance<IPivotCustomSummaryEvent>(Frame, customSummaryType);
//                pivotCustomSummaryEvent.Calculate(e);
//            }
        }

        protected override void OnViewControlsCreated() {
            base.OnViewControlsCreated();
            if (PivotGridListEditor != null) {
                var pivotGridControl = PivotGridListEditor.PivotGridControl;
                pivotGridControl.CellSelectionChanged += PivotGridControlOnCellSelectionChanged;
                View.CollectionSource.CriteriaApplying += CollectionSourceOnCriteriaApplying;
                View.CollectionSource.CriteriaApplied += CollectionSourceOnCriteriaApplied;
                ArrangeLayout();
                PivotVisibility();
                SubscribeToEvents();
                SelectCells();
            }
        }

        void PivotVisibility() {
            var layoutControlItem = PivotLayoutControlItem;
            layoutControlItem.Visibility = ((IModelPivotSettingsEx)((IModelPivotListView)View.Model).PivotSettings).HideGrid ? LayoutVisibility.OnlyInCustomization : LayoutVisibility.Always;
            UpdateSplitterVisibility(layoutControlItem.Visibility);
        }

        private void UpdateSplitterVisibility(LayoutVisibility visibility) {
            SplitterItem splitterItem = null;
            foreach (BaseLayoutItem item in ((XafLayoutControl)PivotGridListEditor.Control).Items) {
                splitterItem = item as SplitterItem;
                if (splitterItem != null) {
                    break;
                }
            }
            if (splitterItem != null) {
                splitterItem.Visibility = visibility;
            }
        }

        void CollectionSourceOnCriteriaApplied(object sender, EventArgs eventArgs) {
            _criteriaApplying = false;
            SelectCells();
        }

        void CollectionSourceOnCriteriaApplying(object sender, EventArgs eventArgs) {
            _criteriaApplying = true;
        }

        void PivotGridControlOnCellSelectionChanged(object sender, EventArgs eventArgs) {
            if (!_criteriaApplying) {
                var gridSelection = ((IModelPivotGridExtender) View.Model.GetNode(XpandPivotGridWinModule.PivotGridControlModelName)).Selection;
                if (gridSelection.RowSelection)
                    RowSelection();
                if (gridSelection.Synchronize) {
                    gridSelection.Rectangle = PivotGridListEditor.PivotGridControl.Cells.Selection;
                }
            }
        }

        void RowSelection() {
            if (_inSelectionUpdate)
                return;
            _inSelectionUpdate = true;
            var selectedRows = GetSelectedRows();
            SetSelection(selectedRows);
            _inSelectionUpdate = false;
        }

        List<int> GetSelectedRows() {
            var selectedRows = new List<int>();
            var readOnlyCells = PivotGridListEditor.PivotGridControl.Cells.MultiSelection.SelectedCells;
            foreach (Point t in readOnlyCells.Where(t => !selectedRows.Contains(t.Y)))
                selectedRows.Add(t.Y);
            return selectedRows;
        }

        void SetSelection(List<int> selectedRows) {
            var selectedCells = new List<Point>();
            var pivotGridCells = PivotGridListEditor.PivotGridControl.Cells;
            for (var x = 0; x < pivotGridCells.ColumnCount; x++) {
                int x1 = x;
                selectedCells.AddRange(selectedRows.Select(t => new Point(x1, t)));
            }
            pivotGridCells.MultiSelection.SetSelection(selectedCells.ToArray());
        }

        void ArrangeLayout() {
            var modelPivotGridGeneral = ((IModelPivotGridExtender) View.Model.GetNode(XpandPivotGridWinModule.PivotGridControlModelName)).General;
            var direction = modelPivotGridGeneral.Direction;
            var layoutControl = (XafLayoutControl)PivotGridListEditor.Control;
            layoutControl.Root.BeginUpdate();
            if (direction.HasValue) {
                if (layoutControl.Root.DefaultLayoutType != direction.Value) {
                    layoutControl.Root.DefaultLayoutType = direction.Value;
                    layoutControl.Root.RotateLayout(true);
                }
            }
            if (modelPivotGridGeneral.FlipLayout) {
                modelPivotGridGeneral.FlipLayout = false;
                layoutControl.Root.FlipLayout(true);
            }
            layoutControl.Root.Update();
            layoutControl.Root.EndUpdate();
        }

        void SelectCells() {
            var pivotGridControl = PivotGridListEditor.PivotGridControl;
            var modelPivotGridSelection = ((IModelPivotGridExtender) View.Model.GetNode(XpandPivotGridWinModule.PivotGridControlModelName)).Selection;
            var rectangle = modelPivotGridSelection.Rectangle;
            if (rectangle.Width == -1)
                rectangle.Width = pivotGridControl.Cells.ColumnCount;
            if (rectangle.Height == -1)
                rectangle.Height = pivotGridControl.Cells.RowCount;
            pivotGridControl.Cells.Selection = rectangle;
        }

        void SubscribeToEvents() {
            var pivotGridControl = PivotGridListEditor.PivotGridControl;
            if (View.AllowEdit) {
                pivotGridControl.ShowingEditor += pivotGridControl_ShowingEditor;
                pivotGridControl.EditValueChanged += pivotGridControl_EditValueChanged;
                pivotGridControl.CustomCellEditForEditing += PivotGridControlOnCustomCellEditForEditing;
            }
            pivotGridControl.FieldValueDisplayText += PivotGridControlOnFieldValueDisplayText;
            pivotGridControl.CustomCellDisplayText += PivotGridControlOnCustomCellDisplayText;
            pivotGridControl.RefreshData();
        }

        void PivotGridControlOnCustomDrawCell(object sender, PivotCustomDrawCellEventArgs e) {
            var point = new Point(e.ColumnIndex, e.RowIndex);
            var events = _ruleCollector.DrawCellRules(point).Select(DrawCell);
            foreach (var cellEvent in events) {
                cellEvent.Calculate(e);
            }
        }

        IPivotDrawCellEvent DrawCell(IModelDrawCellRule modelDrawCellRule) {
            return _ruleCollector.CreateInstance<IPivotDrawCellEvent>(Frame, modelDrawCellRule.DrawCellType);
        }

        void PivotGridControlOnCustomCellDisplayText(object sender, PivotCellDisplayTextEventArgs e) {
            var point = new Point(e.ColumnIndex, e.RowIndex);
            var formatInfos = _ruleCollector.FormatInfos(point, PivotArea.DataArea);
            var displayText = formatInfos.Select(info => info.GetDisplayText(e.Value)).LastOrDefault();
            if (displayText != null) e.DisplayText = displayText;
        }

        void PivotGridControlOnFieldValueDisplayText(object sender, PivotFieldDisplayTextEventArgs e) {
            if ((e.Field != null && (e.Field.Area == PivotArea.DataArea || e.Field.Area == PivotArea.FilterArea)) || (e.Field == null && e.IsColumn)) {
                return;
            }

            var pivotArea = e.Field?.Area ?? PivotArea.RowArea;
            var point = new Point(0, e.MinIndex);
            var formatInfos = _ruleCollector.FormatInfos(point, pivotArea);
            foreach (var formatInfo in formatInfos) {
                if (formatInfo.FormatString.IndexOf("{", StringComparison.Ordinal) > -1)
                    e.DisplayText = formatInfo.GetDisplayText(e.Value);
                else if (!string.IsNullOrEmpty(formatInfo.FormatString)) {
                    e.DisplayText = formatInfo.FormatString;
                }
            }
        }

        void PivotGridControlOnCustomCellEditForEditing(object sender, PivotCustomCellEditEventArgs e) {
            var focusedCell = ((PivotGridControl)sender).Cells.FocusedCell;
            var modelRepositoryItemSpinEdits = _ruleCollector.RepositoryItems(focusedCell).ToArray();
            if (modelRepositoryItemSpinEdits.Any()) {
                var repositoryItemSpinEdit = new RepositoryItemSpinEdit();
                e.RepositoryItem = repositoryItemSpinEdit;
                foreach (var modelRepositoryItemSpinEdit in modelRepositoryItemSpinEdits) {
                    modelRepositoryItemSpinEdit.BindTo(repositoryItemSpinEdit);
//                    new PivotDataFieldRepositoryItemSpinEditSynchronizer(repositoryItemSpinEdit, modelRepositoryItemSpinEdit).ApplyModel();
                }
            }
        }

        void pivotGridControl_ShowingEditor(object sender, CancelPivotCellEditEventArgs e) {
            var pivotGridControl = ((PivotGridControl)sender);
            var cellInfo = pivotGridControl.Cells.GetFocusedCellInfo();
            if (cellInfo.RowValueType == PivotGridValueType.GrandTotal || cellInfo.ColumnValueType == PivotGridValueType.GrandTotal)
                e.Cancel = true;
        }

        void pivotGridControl_EditValueChanged(object sender, EditValueChangedEventArgs e) {
            PivotDrillDownDataSource ds = e.CreateDrillDownDataSource();
            for (int j = 0; j < ds.RowCount; j++) {
                ds[j][e.DataField] = Convert.ChangeType(e.Editor.EditValue, ds[j][e.DataField].GetType());
            }
        }
    }

    class RuleCollector {
        public RuleCollector(IModelListView viewModel) {
            _optionsPivotGrid = viewModel.GetNode(XpandPivotGridWinModule.PivotGridControlModelName);

        }
        readonly IModelNode _optionsPivotGrid;

//        public RuleCollector(IModelListViewOptionsPivotGrid optionsPivotGrid) {
//            _optionsPivotGrid = optionsPivotGrid;
//        }

        public IEnumerable<FormatInfo> FormatInfos(Point point, PivotArea pivotArea) {
            return FormatRuleModels(point, pivotArea).Select(FormatInfo);
        }

        FormatInfo FormatInfo(IModelFormatRule rule) {
            return new FormatInfo { FormatType = rule.FormatType, FormatString = rule.FormatString };
        }

        IEnumerable<IModelFormatRule> FormatRuleModels(Point point, PivotArea pivotArea) {
            return SelectionRuleModels(point).OfType<IModelFormatRule>().Where(rule => rule.PivotArea == pivotArea);
        }

        public IEnumerable<IModelDrawCellRule> DrawCellRules(Point point) {
            return SelectionRuleModels(point).OfType<IModelDrawCellRule>();
        }

        public IEnumerable<IModelNode> RepositoryItems(Point point) {
            return SelectionRuleModels(point).OfType<IModelPivotSpinEditRule>().Where(rule => !rule.NodeDisabled).Select(value => value);
        }

        IEnumerable<IModelPivotSelectionRule> SelectionRuleModels(Point point) {
            return PivotRules().OfType<IModelPivotSelectionRule>().Where(pivotRule => InsideArea(pivotRule, point));
        }

        IEnumerable<IModelPivotRule> PivotRules() {
            return ((IModelPivotGridExtender) _optionsPivotGrid).Rules.Where(rule => !rule.NodeDisabled);
        }

        bool InsideArea(IModelPivotSelectionRule modelPivotArea, Point point) {
            return CalcStart(point, modelPivotArea) && CalcEnd(point, modelPivotArea);
        }

        bool CalcStart(Point point, IModelPivotSelectionRule rule) {
            return rule.Start.X <= point.X && rule.Start.Y <= point.Y;
        }
        bool CalcEnd(Point point, IModelPivotSelectionRule rule) {
            return rule.End == new Point(-1, -1) || ((rule.End.X == -1 || rule.End.X >= point.X) && (rule.End.Y == -1 || rule.End.Y >= point.Y));
        }

        public IEnumerable<IPivotGroupIntervalEvent> GroupIntervalRules(PivotGridField field, Frame frame) {
            var modelPivotGroupIntervalRules = FieldRules<IModelPivotGroupIntervalRule>(field).Where(rule => rule.GroupIntervalType != null);
            return PivotEvents<IPivotGroupIntervalEvent, IModelPivotGroupIntervalRule>(frame, modelPivotGroupIntervalRules, rule => rule.GroupIntervalType);
        }

        IEnumerable<T> PivotEvents<T, TV>(Frame frame, IEnumerable<IModelPivotFieldRule> modelPivotFieldRules, Func<TV, Type> action)
            where T : IPivotEvent
            where TV : IModelPivotFieldRule {
            return modelPivotFieldRules.Select(intervalRule => action.Invoke((TV)intervalRule)).Select(type => CreateInstance<T>(frame, type));
        }

        public T CreateInstance<T>(Frame frame, Type type) where T : IPivotEvent {
            return typeof(Controller).IsAssignableFrom(type) ? frame.Controllers.Values.OfType<T>().First() : (T)type.CreateInstance();
        }

        IEnumerable<T> FieldRules<T>(PivotGridField field) where T : IModelPivotFieldRule {
            return PivotRules().OfType<T>().Where(rule => rule.FieldName == field.FieldName);
        }

        public IEnumerable<IPivotFieldSortEvent> FieldSortRules(PivotGridField field, Frame frame) {
            var modelPivotFieldSortRules = FieldRules<IModelPivotFieldSortRule>(field).Where(rule => rule.CustomSortType != null);
            return PivotEvents<IPivotFieldSortEvent, IModelPivotFieldSortRule>(frame, modelPivotFieldSortRules, rule => rule.CustomSortType);
        }
        public IEnumerable<IModelPivotFieldToolTipRule> ToolTipRules() {
            return PivotRules().OfType<IModelPivotFieldToolTipRule>();
        }
        public IEnumerable<IModelPivotFieldToolTipRule> ToolTipRules(PivotGridField field, Frame frame) {
            return FieldRules<IModelPivotFieldToolTipRule>(field);
        }

    }

}
