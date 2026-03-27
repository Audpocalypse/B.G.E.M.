using MaterialLib;
using Material_Editor.Services;
using System;
using System.Linq;
using System.Reflection;

namespace Material_Editor.Forms
{
    internal partial class Main
    {
        private bool CanUndoSingleEditor => IsSingleMode && singleEditorUndoStates.Count > 0;
        private bool CanRedoSingleEditor => IsSingleMode && singleEditorRedoStates.Count > 0;

        private void ResetSingleEditorUndoState(BaseMaterialFile state = null)
        {
            singleEditorUndoStates.Clear();
            singleEditorRedoStates.Clear();
            lastSingleEditorUndoState = CloneMaterial(state ?? CaptureCurrentSingleEditorState());
        }

        private void RecordSingleEditorUndoStateIfNeeded()
        {
            if (suppressSingleEditorUndoTracking || !IsSingleMode || currentMaterial == null)
                return;

            BaseMaterialFile currentState = CaptureCurrentSingleEditorState();
            if (currentState == null)
                return;

            if (lastSingleEditorUndoState != null && MaterialStatesEqual(lastSingleEditorUndoState, currentState))
            {
                UpdateSingleEditorDirtyState(currentState);
                return;
            }

            if (lastSingleEditorUndoState != null)
                singleEditorUndoStates.Push(CloneMaterial(lastSingleEditorUndoState));

            singleEditorRedoStates.Clear();
            lastSingleEditorUndoState = CloneMaterial(currentState);
            UpdateSingleEditorDirtyState(currentState);
        }

        private void UndoSingleEditorChange()
        {
            if (!CanUndoSingleEditor)
                return;

            BaseMaterialFile currentState = CaptureCurrentSingleEditorState();
            if (currentState != null)
                singleEditorRedoStates.Push(CloneMaterial(currentState));

            BaseMaterialFile targetState = CloneMaterial(singleEditorUndoStates.Pop());
            RestoreSingleEditorHistoryState(targetState, "Undoing change...");
        }

        private void RedoSingleEditorChange()
        {
            if (!CanRedoSingleEditor)
                return;

            BaseMaterialFile currentState = CaptureCurrentSingleEditorState();
            if (currentState != null)
                singleEditorUndoStates.Push(CloneMaterial(currentState));

            BaseMaterialFile targetState = CloneMaterial(singleEditorRedoStates.Pop());
            RestoreSingleEditorHistoryState(targetState, "Redoing change...");
        }

        private void RestoreSingleEditorHistoryState(BaseMaterialFile targetState, string loadingText)
        {
            BaseMaterialFile currentState = CaptureCurrentSingleEditorState();
            SingleEditorSectionState sectionState = CaptureSingleEditorSectionState();
            string[] changedControls = GetChangedSingleEditorControlNames(currentState, targetState);
            BaseMaterialFile baseline = CloneMaterial(originalMaterial) ?? CloneMaterial(targetState);
            bool markDirty = !MaterialStatesEqual(targetState, baseline);

            suppressSingleEditorUndoTracking = true;
            try
            {
                if (!TryRestoreSingleEditorHistoryStateInPlace(targetState, baseline, changedControls))
                    OpenMaterialState(workFilePath, targetState, baseline, markDirty, loadingText);
            }
            finally
            {
                suppressSingleEditorUndoTracking = false;
            }

            ApplySingleEditorSectionState(sectionState);
            ExpandSingleEditorSectionsForControls(changedControls);
            lastSingleEditorUndoState = CloneMaterial(targetState);
            UpdateSingleEditorDirtyState(targetState);
        }

        private bool TryRestoreSingleEditorHistoryStateInPlace(BaseMaterialFile targetState, BaseMaterialFile baseline, string[] changedControls)
        {
            if (targetState == null
                || currentMaterial == null
                || currentMaterial.GetType() != targetState.GetType()
                || !HasReusableControlTree(targetState))
            {
                return false;
            }

            bool versionChanged = currentMaterial.Version != targetState.Version;
            workspaceMode = WorkspaceMode.Single;

            SuspendAll();
            try
            {
                CopyMaterialState(targetState, currentMaterial);
                ApplyMaterialToExistingControls(currentMaterial);

                if (versionChanged)
                {
                    Game targetGame = currentMaterial.Version > 2 && currentMaterial.Version <= 22
                        ? Game.FO76
                        : Game.FO4;

                    if (CurrentGame != targetGame)
                        SetGameSelection(targetGame);
                    else
                        FillVersionDropdown();
                }
                else
                {
                    SelectVersionInDropdown(currentMaterial.Version);
                }

                RefreshReusedControlVisibility(changedControls, versionChanged);
                UpdateTopLevelSectionVisibility();
            }
            finally
            {
                ResumeAll();
            }

            if (baseline != null)
                originalMaterial = CloneMaterial(baseline);

            saveToolStripMenuItem.Enabled = true;
            saveAsToolStripMenuItem.Enabled = true;
            closeToolStripMenuItem.Enabled = true;
            layoutGeneral.Enabled = true;
            layoutMaterial.Enabled = true;
            layoutEffect.Enabled = true;
            return true;
        }

        private static string[] GetChangedSingleEditorControlNames(BaseMaterialFile currentState, BaseMaterialFile targetState)
        {
            if (targetState == null)
                return Array.Empty<string>();

            MaterialType materialType = targetState is BGEM
                ? MaterialType.Effect
                : MaterialType.Material;

            if (currentState == null || currentState.GetType() != targetState.GetType())
            {
                return MaterialFieldRegistry.GetDescriptors(targetState)
                    .Select(descriptor => descriptor.Label)
                    .ToArray();
            }

            return MaterialFieldRegistry.GetDescriptors(materialType)
                .Where(descriptor => descriptor.IsSupported(currentState) || descriptor.IsSupported(targetState))
                .Where(descriptor => !BulkMaterialEditValueComparer.ValuesEqual(
                    descriptor.GetValue(currentState),
                    descriptor.GetValue(targetState)))
                .Select(descriptor => descriptor.Label)
                .ToArray();
        }

        private void UpdateSingleEditorDirtyState(BaseMaterialFile state = null)
        {
            BaseMaterialFile currentState = state ?? CaptureCurrentSingleEditorState();
            changed = currentState != null && !MaterialStatesEqual(currentState, originalMaterial);
            UpdateWorkspaceCommandState();
            UpdateWindowTitle();
        }

        private void EditUndoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (IsBulkMode)
            {
                if (bulkEditorView?.ExecuteUndo() == true)
                {
                    UpdateWorkspaceCommandState();
                    UpdateWindowTitle();
                }

                return;
            }

            UndoSingleEditorChange();
        }

        private void EditRedoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (IsBulkMode)
            {
                if (bulkEditorView?.ExecuteRedo() == true)
                {
                    UpdateWorkspaceCommandState();
                    UpdateWindowTitle();
                }

                return;
            }

            RedoSingleEditorChange();
        }

        private static bool MaterialStatesEqual(BaseMaterialFile left, BaseMaterialFile right)
        {
            if (ReferenceEquals(left, right))
                return true;

            if (left == null || right == null || left.GetType() != right.GetType())
                return false;

            PropertyInfo[] properties = left.GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(property => property.CanRead && property.CanWrite && property.GetIndexParameters().Length == 0)
                .ToArray();

            foreach (PropertyInfo property in properties)
            {
                object leftValue = property.GetValue(left);
                object rightValue = property.GetValue(right);
                if (!BulkMaterialEditValueComparer.ValuesEqual(leftValue, rightValue))
                    return false;
            }

            return true;
        }
    }
}
