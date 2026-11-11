using SlideJam.Game;
using UnityEditor;
using UnityEngine;

namespace SlideJam.EditorTools
{
    internal static class QueueBoxBuilder
    {
        private const string BoxSprite = "Assets/Art/Sprites/queue/vault_bin.png";

        internal static bool Build(GameController controller)
        {
            var stage = Ref<StageLayout>(controller, "_stageLayout");
            var queueView = Ref<QueueView>(controller, "_queueView");
            if (stage == null || queueView == null)
            {
                Debug.LogError("Setup Scene: GameController has an empty StageLayout or QueueView reference.");
                return false;
            }

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BoxSprite);
            if (sprite == null)
            {
                Debug.LogError("Setup Scene: sprite not found: " + BoxSprite);
                return false;
            }

            RemoveOldOverflow(stage, queueView);

            Transform boxObject = Child(queueView.transform, "OverflowBox");
            var box = boxObject.GetComponent<SpriteRenderer>();
            if (box == null) box = Undo.AddComponent<SpriteRenderer>(boxObject.gameObject);

            box.sprite = sprite;
            box.sortingOrder = 0;
            box.color = Color.white;

            SetRef(queueView, "_overflowBox", box);
            EditorUtility.SetDirty(queueView);
            return true;
        }

        private static void RemoveOldOverflow(StageLayout stage, QueueView queueView)
        {
            Transform overflow = stage.transform.Find("Overflow");
            if (overflow == null) return;

            Transform label = overflow.Find("PreviewLabel");
            if (label != null) Undo.SetTransformParent(label, queueView.transform, "Setup Scene");

            Undo.DestroyObjectImmediate(overflow.gameObject);
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