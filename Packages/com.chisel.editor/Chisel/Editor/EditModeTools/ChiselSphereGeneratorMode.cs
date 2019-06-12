﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Chisel.Core;
using Chisel.Components;
using UnitySceneExtensions;
using UnityEditor.ShortcutManagement;

namespace Chisel.Editors
{
    public sealed class ChiselSphereGeneratorMode : IChiselToolMode
    {
        #region Keyboard Shortcut
        const string kToolShotcutName = ChiselKeyboardDefaults.ShortCutCreateBase + ChiselSphere.kNodeTypeName;
        [Shortcut(kToolShotcutName, ChiselKeyboardDefaults.SphereBuilderModeKey, ChiselKeyboardDefaults.SphereBuilderModeModifiers, displayName = kToolShotcutName)]
        public static void Enable() { ChiselEditModeManager.EditMode = ChiselEditMode.Sphere; }
        #endregion
        
        public void OnEnable()
        {
            // TODO: shouldn't just always set this param
            Tools.hidden = true;
            Reset();
        }

        public void OnDisable()
        {
            Reset();
        }

        void Reset()
        {
            BoxExtrusionHandle.Reset();
            sphere = null;
        }

        // TODO: Handle forcing operation types
        CSGOperationType? forceOperation = null;

        // TODO: Ability to modify default settings
        // TODO: Store/retrieve default settings
        bool generateFromCenterXZ   = true;
        bool generateFromCenterY    = ChiselSphereDefinition.kDefaultGenerateFromCenter;
        bool isSymmetrical          = true;
        int verticalSegments        = ChiselSphereDefinition.kDefaultVerticalSegments;
        int horizontalSegments      = ChiselSphereDefinition.kDefaultHorizontalSegments;


        ChiselSphere sphere;

        public void OnSceneGUI(SceneView sceneView, Rect dragArea)
        {
            Bounds    bounds;
            ChiselModel  modelBeneathCursor;
            Matrix4x4 transformation;
            float     height;

            var flags = (generateFromCenterY  ? BoxExtrusionFlags.GenerateFromCenterY  : BoxExtrusionFlags.None) |
                        (isSymmetrical        ? BoxExtrusionFlags.IsSymmetricalXZ      : BoxExtrusionFlags.None) |
                        (generateFromCenterXZ ? BoxExtrusionFlags.GenerateFromCenterXZ : BoxExtrusionFlags.None);

            switch (BoxExtrusionHandle.Do(dragArea, out bounds, out height, out modelBeneathCursor, out transformation, flags, Axis.Y))
            {
                case BoxExtrusionState.Create:
                {
                    sphere = ChiselComponentFactory.Create<ChiselSphere>("Sphere",
                                                                ChiselModelManager.GetActiveModelOrCreate(modelBeneathCursor),
                                                                transformation);

                    sphere.definition.Reset();
                    sphere.Operation            = forceOperation ?? CSGOperationType.Additive;
                    sphere.VerticalSegments     = verticalSegments;
                    sphere.HorizontalSegments   = horizontalSegments;
                    sphere.GenerateFromCenter   = generateFromCenterY;
                    sphere.DiameterXYZ          = bounds.size;
                    sphere.UpdateGenerator();
                    break;
                }

                case BoxExtrusionState.Modified:
                {
                    sphere.Operation    = forceOperation ??
                                          ((height < 0 && modelBeneathCursor) ?
                                            CSGOperationType.Subtractive :
                                            CSGOperationType.Additive);
                    sphere.DiameterXYZ  = bounds.size;
                    break;
                }

                case BoxExtrusionState.Commit:
                {
                    UnityEditor.Selection.activeGameObject = sphere.gameObject;
                    ChiselEditModeManager.EditMode = ChiselEditMode.ShapeEdit;
                    Reset();
                    break;
                }

                case BoxExtrusionState.Cancel:
                {
                    Reset();
                    Undo.RevertAllInCurrentGroup();
                    EditorGUIUtility.ExitGUI();
                    break;
                }

                case BoxExtrusionState.BoxMode:
                case BoxExtrusionState.SquareMode: { ChiselOutlineRenderer.VisualizationMode = VisualizationMode.SimpleOutline; break; }
                case BoxExtrusionState.HoverMode: { ChiselOutlineRenderer.VisualizationMode = VisualizationMode.Outline; break; }
            }

            // TODO: Make a RenderSphere method
            HandleRendering.RenderCylinder(transformation, bounds, horizontalSegments);
        }
    }
}