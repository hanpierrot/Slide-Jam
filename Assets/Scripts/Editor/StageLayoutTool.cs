using System.Collections.Generic;
using SlideJam.Game;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace SlideJam.EditorTools
{
    // Finds or creates the StageLayoutData, wires it to the StageLayout, and saves the anchor positions of the scene into it
    internal static class StageLayoutTool
    {
        private const string DataPath = "Assets/Design/StageLayoutData.asset";

        // The data the Stage uses. The first time (no asset yet, or an asset without positions) the positions of the scene are saved into it.
        internal static StageLayoutData Ensure(GameController controller)
        {
            var stage = Ref<StageLayout>(controller, "_stageLayout");
            if (stage == null)
            {
                Debug.LogError("Slide Jam: GameController has an empty StageLayout reference.");
                return null;
            }

            StageLayoutData data = stage.Data;
            if (data == null) data = AssetDatabase.LoadAssetAtPath<StageLayoutData>(DataPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<StageLayoutData>();
                AssetDatabase.CreateAsset(data, DataPath);
                Debug.Log("Slide Jam: created " + DataPath);
            }

            if (stage.Data != data) SetRef(stage, "_data", data);

            if (!data.HasPositions)
            {
                Capture(stage, data);
                Debug.Log("Slide Jam: the data had no anchor positions, so the current positions of the scene were saved into it.");
            }

            return data;
        }

        // The Save Stage Layout menu: the current local positions of the six anchors go into the data
        internal static bool Save(GameController controller)
        {
            StageLayoutData data = Ensure(controller);
            if (data == null) return false;

            var stage = Ref<StageLayout>(controller, "_stageLayout");
            Capture(stage, data);
            Debug.Log("Save Stage Layout: the anchors and " + data.Items.Count + " other objects were saved into " + AssetDatabase.GetAssetPath(data) + "."); // changed
            return true;
        }

        private static void Capture(StageLayout stage, StageLayoutData data)
        {
            Undo.RecordObject(data, "Save Stage Layout");
            data.SetPositions(
                Position(stage, "_slots"),
                Position(stage, "_strip"),
                Position(stage, "_queue"),
                Position(stage, "_branch"),
                Position(stage, "_dots"),
                Position(stage, "_grid"));
            data.SetScales(
                Scale(stage, "_slots"),
                Scale(stage, "_strip"),
                Scale(stage, "_queue"),
                Scale(stage, "_branch"),
                Scale(stage, "_dots"),
                Scale(stage, "_grid"));
            CaptureItems(stage, data); // added: everything below the anchors too
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
        }

        // Added: if the data has no items yet (it was saved before items existed), they are taken from the scene as it is now.
        // Setup Scene calls this first, before any builder changes the scene.
        internal static void CaptureItemsIfMissing(GameController controller)
        {
            var stage = Ref<StageLayout>(controller, "_stageLayout");
            if (stage == null || stage.Data == null || stage.Data.Items.Count > 0) return;

            Undo.RecordObject(stage.Data, "Save Stage Layout");
            CaptureItems(stage, stage.Data);
            EditorUtility.SetDirty(stage.Data);
            AssetDatabase.SaveAssets();
            Debug.Log("Slide Jam: the layout data had no item layout, so " + stage.Data.Items.Count + " objects of the scene were saved into it.");
        }

        // Added: puts the saved position, scale, rotation, size and font size back on every object below the anchors.
        // Run last by the menus, so it wins over what the builders and the views compute in Edit Mode.
        internal static void ApplyItems(GameController controller)
        {
            var stage = Ref<StageLayout>(controller, "_stageLayout");
            if (stage == null || stage.Data == null || stage.Data.Items.Count == 0) return;

            int changed = 0;
            int missing = 0;
            foreach (StageLayoutData.ItemLayout item in stage.Data.Items)
            {
                Transform target = stage.transform.Find(item.Path);
                if (target == null)
                {
                    missing++;
                    continue;
                }

                if (Restore(target, item)) changed++;
            }

            Debug.Log("Slide Jam: item layout applied to " + changed + " objects (" + missing + " saved objects are not in the scene).");
        }

        private static void CaptureItems(StageLayout stage, StageLayoutData data)
        {
            var anchors = new HashSet<Transform>();
            foreach (string field in new[] { "_slots", "_strip", "_queue", "_branch", "_dots", "_grid" })
            {
                var anchor = Ref<Transform>(stage, field);
                if (anchor != null) anchors.Add(anchor);
            }

            var items = new List<StageLayoutData.ItemLayout>();
            Collect(stage.transform, stage.transform, anchors, items);
            data.SetItems(items);
        }

        private static void Collect(Transform root, Transform parent, HashSet<Transform> anchors, List<StageLayoutData.ItemLayout> items)
        {
            foreach (Transform child in parent)
            {
                if (child.GetComponent<BoardView>() != null) continue; // the board lays out its own blocks

                if (!anchors.Contains(child)) items.Add(Describe(root, child));
                Collect(root, child, anchors, items);
            }
        }

        private static StageLayoutData.ItemLayout Describe(Transform root, Transform item)
        {
            var layout = new StageLayoutData.ItemLayout
            {
                Path = AnimationUtility.CalculateTransformPath(item, root),
                Position = item.localPosition,
                Scale = item.localScale,
                RotationZ = item.localEulerAngles.z,
            };

            if (item is RectTransform rect) layout.Size = rect.sizeDelta;
            if (item.TryGetComponent(out TMP_Text text)) layout.FontSize = text.fontSize;
            return layout;
        }

        private static bool Restore(Transform target, StageLayoutData.ItemLayout item)
        {
            bool different = target.localPosition != item.Position
                             || target.localScale != item.Scale
                             || !Mathf.Approximately(target.localEulerAngles.z, item.RotationZ);
            var rect = target as RectTransform;
            bool resize = rect != null && item.Size != Vector2.zero && rect.sizeDelta != item.Size;
            bool hasText = target.TryGetComponent(out TMP_Text text) && item.FontSize > 0f;
            bool refont = hasText && !Mathf.Approximately(text.fontSize, item.FontSize);
            if (!different && !resize && !refont) return false;

            Undo.RecordObject(target, "Setup Scene");
            target.localPosition = item.Position;
            target.localScale = item.Scale;
            Vector3 euler = target.localEulerAngles;
            target.localEulerAngles = new Vector3(euler.x, euler.y, item.RotationZ);
            if (resize) rect.sizeDelta = item.Size;

            if (refont)
            {
                Undo.RecordObject(text, "Setup Scene");
                text.fontSize = item.FontSize;
            }

            return true;
        }

        private static Vector3 Position(StageLayout stage, string field)
        {
            var anchor = Ref<Transform>(stage, field);
            if (anchor == null)
            {
                Debug.LogWarning("Slide Jam: the StageLayout has no " + field + " anchor yet; its saved position is zero.");
                return Vector3.zero;
            }

            return anchor.localPosition;
        }

        private static Vector3 Scale(StageLayout stage, string field)
        {
            var anchor = Ref<Transform>(stage, field);
            return anchor == null ? Vector3.one : anchor.localScale;
        }

        private static T Ref<T>(Object owner, string field) where T : Object
        {
            return new SerializedObject(owner).FindProperty(field).objectReferenceValue as T;
        }

        private static void SetRef(Object owner, string field, Object value)
        {
            var so = new SerializedObject(owner);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedProperties();
        }
    }
}
