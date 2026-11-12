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
            var queueView = Ref<QueueView>(controller, "_queueView");
            if (queueView == null)
            {
                Debug.LogError("Setup Scene: GameController has an empty QueueView reference.");
                return false;
            }

            var white = AssetDatabase.LoadAssetAtPath<Sprite>(Sprites + "cell_ring_white_54.png");
            var orange = AssetDatabase.LoadAssetAtPath<Sprite>(Sprites + "cell_ring_orange_54.png");
            var dashed = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/queue/queue_slot_preview_dashed.png");
            var red = AssetDatabase.LoadAssetAtPath<Sprite>(Sprites + "cell_ring_red_54.png");
            if (white == null || orange == null || dashed == null)
            {
                Debug.LogError("Setup Scene: a ring or the dashed box sprite was not found.");
                return false;
            }

            Transform holder = Child(controller.transform, "Preview");
            var view = holder.GetComponent<PreviewView>();
            if (view == null) view = Undo.AddComponent<PreviewView>(holder.gameObject);

            Transform labelObject = Child(queueView.transform, "PreviewLabel");
            var label = labelObject.GetComponent<TextMeshPro>();
            if (label == null) label = Undo.AddComponent<TextMeshPro>(labelObject.gameObject);

            label.text = string.Empty;
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 3f;
            label.enableWordWrapping = false;
            label.sortingOrder = 40;
            label.rectTransform.sizeDelta = new Vector2(1.5f, 0.6f);

            SetRef(view, "_ringWhite", white);
            SetRef(view, "_ringOrange", orange);
            SetRef(view, "_ringRed", red);
            SetRef(view, "_boxDashed", dashed);
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