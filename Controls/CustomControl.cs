using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Material_Editor.Controls
{
    public abstract class CustomControl : IDisposable
    {
        public readonly string Name;
        public bool Serialize = true;
        public string BaseToolTip;

        protected Func<CustomControl, bool> VisibilityCallback;
        protected Action<CustomControl> ChangedCallback;

        public CustomControl(string label)
        {
            Name = label;
            CreateControls();
        }

        public virtual Label LabelControl
        {
            get { return null; }
        }

        public virtual Control Control
        {
            get { return null; }
        }

        public virtual Control ExtraControl
        {
            get { return null; }
        }

        public virtual void CreateControls() { }

        public virtual object GetProperty()
        {
            return null;
        }

        public virtual void SetProperty(object value)
        {
        }

        internal virtual void ApplyAppearance(AppearanceDefinition appearance)
        {
            if (appearance == null)
                return;

            if (LabelControl != null)
                LabelControl.Font = appearance.Font;

            if (Control != null)
                Control.Font = appearance.Font;

            if (ExtraControl != null)
                ExtraControl.Font = appearance.Font;
        }

        public void SetVisible(bool visible)
        {
            if (LabelControl != null)
                LabelControl.Visible = visible;

            if (Control != null)
                Control.Visible = visible;

            if (ExtraControl != null)
                ExtraControl.Visible = visible;
        }

        public void SetTooltip(ToolTip parentTooltip, string toolTip)
        {
            BaseToolTip = toolTip;

            if (LabelControl != null)
                parentTooltip.SetToolTip(LabelControl, toolTip);

            if (Control != null)
                parentTooltip.SetToolTip(Control, toolTip);

            if (ExtraControl != null)
                parentTooltip.SetToolTip(ExtraControl, toolTip);
        }

        public bool ShouldBeVisible()
        {
            return VisibilityCallback?.Invoke(this) ?? true;
        }

        public void InvokeChangedCallback()
        {
            ChangedCallback?.Invoke(this);
        }

        public void Dispose()
        {
            LabelControl?.Dispose();
            Control?.Dispose();
            ExtraControl?.Dispose();
            GC.SuppressFinalize(this);
        }
    }

    public static class ControlFactory
    {
        private static readonly Dictionary<string, CustomControl> customControls = [];

        public static Action<CustomControl> DefaultChangedCallback;
        public static Action VisibilityChangedCallback;

        public static void ClearControls()
        {
            var parentTables = new HashSet<TableLayoutPanel>();

            foreach (var control in customControls.Values)
            {
                CollectParentTable(parentTables, control.LabelControl);
                CollectParentTable(parentTables, control.Control);
                CollectParentTable(parentTables, control.ExtraControl);

                control.LabelControl?.Parent?.Controls.Remove(control.LabelControl);
                control.Control?.Parent?.Controls.Remove(control.Control);
                control.ExtraControl?.Parent?.Controls.Remove(control.ExtraControl);

                control.Dispose();
            }

            foreach (var parentTable in parentTables)
            {
                parentTable.Controls.Clear();
                parentTable.RowCount = 0;
                parentTable.RowStyles.Clear();
            }

            customControls.Clear();
        }

        private static void CollectParentTable(HashSet<TableLayoutPanel> parentTables, Control control)
        {
            if (control?.Parent is TableLayoutPanel parentTable)
                parentTables.Add(parentTable);
        }
        
        public static CustomControl Find(string name)
        {
            if (customControls.TryGetValue(name, out CustomControl value))
                return value;

            return null;
        }

        public static string GetTooltip(string name)
        {
            if (customControls.TryGetValue(name, out CustomControl control))
                return control.BaseToolTip;

            return null;
        }

        public static bool GetProperty(string name, out object property)
        {
            if (customControls.TryGetValue(name, out CustomControl control))
            {
                property = control.GetProperty();
                return true;
            }

            property = null;
            return false;
        }

        public static bool SetProperty(string name, object value)
        {
            if (!customControls.TryGetValue(name, out CustomControl control))
                return false;

            control.SetProperty(value);
            return true;
        }

        public static void SetVisible(string name, bool visible, bool serialize = true)
        {
            if (customControls.TryGetValue(name, out CustomControl value))
            {
                var control = value;
                control.SetVisible(visible);
                control.Serialize = serialize;
            }
        }

        public static void SetTooltip(string name, ToolTip parentTooltip, string toolTip)
        {
            if (customControls.TryGetValue(name, out CustomControl value))
            {
                var control = value;
                control.SetTooltip(parentTooltip, toolTip);
            }
        }

        public static void UpdateVisibility()
        {
            UpdateVisibilityCore(customControls.Values);
        }

        public static void UpdateVisibility(string name)
        {
            if (customControls.TryGetValue(name, out CustomControl control))
            {
                UpdateVisibilityCore([control]);
                return;
            }

            VisibilityChangedCallback?.Invoke();
        }

        public static void UpdateVisibility(params string[] names)
        {
            if (names == null || names.Length == 0)
            {
                UpdateVisibility();
                return;
            }

            var controls = names
                .Where(name => !string.IsNullOrEmpty(name))
                .Select(name => customControls.TryGetValue(name, out CustomControl control) ? control : null)
                .Where(control => control != null)
                .Distinct()
                .ToArray();

            if (controls.Length == 0)
            {
                VisibilityChangedCallback?.Invoke();
                return;
            }

            UpdateVisibilityCore(controls);
        }

        internal static void ApplyAppearance(AppearanceDefinition appearance)
        {
            foreach (CustomControl control in customControls.Values)
                control.ApplyAppearance(appearance);
        }

        public static CustomControl CreateControl(TableLayoutPanel parent, string label, object property, Func<CustomControl, bool> visibilityCallback = null, Action<CustomControl> changedCallback = null)
        {
            CustomControl control = CreateCustomControl(label, property, visibilityCallback, changedCallback);

            if (control != null)
            {
                AddCustomControl(parent, label, control);
            }

            return control;
        }

        public static CustomControl CreateDetachedControl(string label, object property, Func<CustomControl, bool> visibilityCallback = null, Action<CustomControl> changedCallback = null)
        {
            CustomControl control = CreateCustomControl(label, property, visibilityCallback, changedCallback);
            if (control != null)
                customControls.Add(label, control);

            return control;
        }

        public static void AddCustomControl(TableLayoutPanel parent, string label, CustomControl control)
        {
            parent.RowCount++;
            parent.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            parent.Controls.Add(control.LabelControl, 0, parent.RowCount - 1);
            parent.Controls.Add(control.Control, 1, parent.RowCount - 1);

            if (control.ExtraControl != null)
                parent.Controls.Add(control.ExtraControl, 2, parent.RowCount - 1);

            customControls.Add(label, control);
        }

        public static CustomControl CreateDropdownControl(TableLayoutPanel parent, string label, object[] entries, int selection, Func<CustomControl, bool> visibilityCallback = null, Action<CustomControl> changedCallback = null)
        {
            var control = new DropdownControl(label, visibilityCallback, changedCallback ?? DefaultChangedCallback, entries, selection);
            AddCustomControl(parent, label, control);
            return control;
        }

        public static CustomControl CreateDetachedDropdownControl(string label, object[] entries, int selection, Func<CustomControl, bool> visibilityCallback = null, Action<CustomControl> changedCallback = null)
        {
            var control = new DropdownControl(label, visibilityCallback, changedCallback ?? DefaultChangedCallback, entries, selection);
            customControls.Add(label, control);
            return control;
        }

        public static CustomControl CreateFlagControl(TableLayoutPanel parent, string label, object[] entries, int flagValue, Func<CustomControl, bool> visibilityCallback = null, Action<CustomControl> changedCallback = null)
        {
            var control = new FlagControl(label, visibilityCallback, changedCallback ?? DefaultChangedCallback, entries, flagValue);
            AddCustomControl(parent, label, control);
            return control;
        }

        public static CustomControl CreateDetachedFlagControl(string label, object[] entries, int flagValue, Func<CustomControl, bool> visibilityCallback = null, Action<CustomControl> changedCallback = null)
        {
            var control = new FlagControl(label, visibilityCallback, changedCallback ?? DefaultChangedCallback, entries, flagValue);
            customControls.Add(label, control);
            return control;
        }

        public static CustomControl CreateFileControl(TableLayoutPanel parent, string label, Font font, FileControl.FileType fileType, string filePath, Func<CustomControl, bool> visibilityCallback = null, Action<CustomControl> changedCallback = null)
        {
            CustomControl control = CreateFileCustomControl(label, font, fileType, filePath, visibilityCallback, changedCallback);

            if (control != null)
            {
                AddCustomControl(parent, label, control);
            }

            return control;
        }

        public static CustomControl CreateDetachedFileControl(string label, Font font, FileControl.FileType fileType, string filePath, Func<CustomControl, bool> visibilityCallback = null, Action<CustomControl> changedCallback = null)
        {
            CustomControl control = CreateFileCustomControl(label, font, fileType, filePath, visibilityCallback, changedCallback);
            if (control != null)
                customControls.Add(label, control);

            return control;
        }

        private static CustomControl CreateCustomControl(string label, object property, Func<CustomControl, bool> visibilityCallback, Action<CustomControl> changedCallback)
        {
            CustomControl control = null;

            var type = property.GetType();
            if (type == typeof(int))
            {
                control = NumberControl.ForInteger(label, visibilityCallback, changedCallback ?? DefaultChangedCallback, (int)property);
            }
            else if (type == typeof(uint))
            {
                control = NumberControl.ForInteger(label, visibilityCallback, changedCallback ?? DefaultChangedCallback, (uint)property, uint.MinValue, uint.MaxValue);
            }
            else if (type == typeof(short))
            {
                control = NumberControl.ForInteger(label, visibilityCallback, changedCallback ?? DefaultChangedCallback, (short)property, short.MinValue, short.MaxValue);
            }
            else if (type == typeof(ushort))
            {
                control = NumberControl.ForInteger(label, visibilityCallback, changedCallback ?? DefaultChangedCallback, (ushort)property, ushort.MinValue, ushort.MaxValue);
            }
            else if (type == typeof(byte))
            {
                control = NumberControl.ForInteger(label, visibilityCallback, changedCallback ?? DefaultChangedCallback, (byte)property, byte.MinValue, byte.MaxValue);
            }
            else if (type == typeof(decimal) || type == typeof(float) || type == typeof(double))
            {
                control = NumberControl.ForDecimal(label, visibilityCallback, changedCallback ?? DefaultChangedCallback, Convert.ToDecimal(property));
            }
            else if (type == typeof(bool))
            {
                control = new CheckControl(label, visibilityCallback, changedCallback ?? DefaultChangedCallback, (bool)property);
            }
            else if (type == typeof(Color))
            {
                control = new ColorControl(label, visibilityCallback, changedCallback ?? DefaultChangedCallback, (Color)property);
            }
            else if (type == typeof(object[]))
            {
                control = new DropdownControl(label, visibilityCallback, changedCallback ?? DefaultChangedCallback, property as object[], 0);
            }

            return control;
        }

        private static CustomControl CreateFileCustomControl(string label, Font font, FileControl.FileType fileType, string filePath, Func<CustomControl, bool> visibilityCallback, Action<CustomControl> changedCallback)
        {
            return fileType switch
            {
                FileControl.FileType.Material => new FileControl(label, font, visibilityCallback, changedCallback ?? DefaultChangedCallback, FileControl.FileType.Material, filePath),
                _ => new FileControl(label, font, visibilityCallback, changedCallback ?? DefaultChangedCallback, FileControl.FileType.Texture, filePath),
            };
        }

        private static void UpdateVisibilityCore(IEnumerable<CustomControl> controls)
        {
            CustomControl[] controlsToUpdate = controls?
                .Where(control => control != null)
                .Distinct()
                .ToArray() ?? Array.Empty<CustomControl>();

            using var _ = new LayoutSuspensionScope(controlsToUpdate);
            foreach (CustomControl control in controlsToUpdate)
                control.SetVisible(control.ShouldBeVisible());

            VisibilityChangedCallback?.Invoke();
        }

        private sealed class LayoutSuspensionScope : IDisposable
        {
            private readonly List<Control> suspendedControls;

            public LayoutSuspensionScope(IEnumerable<CustomControl> controls)
            {
                suspendedControls = controls?
                    .SelectMany(GetLayoutChain)
                    .Distinct()
                    .ToList() ?? [];

                foreach (Control control in suspendedControls)
                    control.SuspendLayout();
            }

            public void Dispose()
            {
                for (int index = suspendedControls.Count - 1; index >= 0; index--)
                    suspendedControls[index].ResumeLayout(true);
            }

            private static IEnumerable<Control> GetLayoutChain(CustomControl control)
            {
                foreach (Control ownedControl in EnumerateOwnedControls(control))
                {
                    for (Control current = ownedControl?.Parent; current != null; current = current.Parent)
                        yield return current;
                }
            }

            private static IEnumerable<Control> EnumerateOwnedControls(CustomControl control)
            {
                if (control?.LabelControl != null)
                    yield return control.LabelControl;

                if (control?.Control != null)
                    yield return control.Control;

                if (control?.ExtraControl != null)
                    yield return control.ExtraControl;
            }
        }
    }
}
