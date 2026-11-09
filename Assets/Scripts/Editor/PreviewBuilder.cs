using SlideJam.Game;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace SlideJam.EditorTools
{
    internal static class PreviewBuilder
    {
        private const string Sprites = "Assets/Art/Sprites/board/";

        internal static bool Build(GameController controller)
        {
            var stage = Ref<StageLayout>(controller, "_stageLayout");
            Transform overflow = stage != null ? Ref<Transform>(stage, "_overflow") : null;
            if (overflow == null)
            {
                Debug.LogError("Setup Scene: the Overflow anchor is missing, so the preview label has no place.");
                return false;
            }

            var white = AssetDatabase.LoadAssetAtPath<Sprite>(Sprites + "cell_ring_white_54.png");
            var orange = AssetDatabase.LoadAssetAtPath<Sprite>(Sprites + "cell_ring_orange_54.png");
            if (white == null || orange == null)
            {
                Debug.LogError("Setup Scene: the cell_ring_white_54 or cell_ring_orange_54 sprite was not found in " + Sprites);
                return false;
            }

            Transform holder = Child(controller.transform, "Preview");
            var view = holder.GetComponent<PreviewView>();
            if (view == null) view = Undo.AddComponent<PreviewView>(holder.gameObject);

            Transform labelObject = Child(overflow, "PreviewLabel");
            var label = labelObject.GetComponent<TextMeshPro>();
            if (label == null) label = Undo.AddComponent<TextMeshPro>(labelObject.gameObject);

            label.text = string.Empty;
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 2.4f;
            label.enableWordWrapping = false;
            label.sortingOrder = 40;
            label.rectTransform.sizeDelta = new Vector2(6f, 0.7f);
            label.rectTransform.localPosition = Vector3.zero;

            SetRef(view, "_ringWhite", white);
            SetRef(view, "_ringOrange", orange);
            SetRef(view, "_label", label);
            SetRef(controller, "_previewView", view);

            EditorUtility.SetDirty(view);
            EditorUtility.SetDirty(label);
            return true;
        }

        private static Transform Child(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null) return child;

            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Setup Scene");
            go.transform.SetParent(parent, false);
            return go.transform;
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