using SlideJam.Game;
using UnityEditor;
using UnityEngine;

namespace SlideJam.EditorTools
{
    public static class OverflowBuilder
    {
        private const string PlankSprite = "Assets/Art/Sprites/shelf/shelf_plank.png";
        private const string OverflowName = "Overflow";
        private const string PlankName = "OverflowPlank";
        private const float PlankWidth = 5f; // six overflow units at 0.8 spacing are about 4.8 wide
        private const float PlankOffsetY = -0.3f; // under the units, which are centred on the anchor

        // The Overflow anchor, its plank and the two references. Run by the Setup Scene menu (SlideJamMenu).
        internal static bool Build(GameController controller)
        {
            var stage = Ref<StageLayout>(controller, "_stageLayout");
            var queueView = Ref<QueueView>(controller, "_queueView");
            if (stage == null || queueView == null)
            {
                Debug.LogError("Setup Scene: GameController has an empty StageLayout or QueueView reference.");
                return false;
            }

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PlankSprite);
            if (sprite == null)
            {
                Debug.LogError("Setup Scene: sprite not found: " + PlankSprite);
                return false;
            }

            // The band anchor, placed by StageLayout under the queue
            Transform overflow = Ref<Transform>(stage, "_overflow");
            if (overflow == null) overflow = Child(stage.transform, OverflowName);
            SetRef(stage, "_overflow", overflow);

            // The plank is a child of the anchor, lower than the units; the strip plank sprite is reused
            Transform plankTransform = Child(overflow, PlankName);
            var plank = plankTransform.GetComponent<SpriteRenderer>();
            if (plank == null) plank = plankTransform.gameObject.AddComponent<SpriteRenderer>();

            plank.sprite = sprite;
            plank.sortingOrder = -10;
            plank.color = Color.white;
            plank.enabled = true;
            plankTransform.localPosition = new Vector3(0f, PlankOffsetY, 0f);
            plankTransform.localScale = Vector3.one * (PlankWidth / sprite.bounds.size.x);
            plankTransform.gameObject.SetActive(true); // shown here for the layout; QueueView hides it when a run starts

            SetRef(queueView, "_overflowAnchor", overflow);
            SetRef(queueView, "_overflowPlank", plank);

            EditorUtility.SetDirty(stage);
            EditorUtility.SetDirty(queueView);
            return true;
        }

        private static Transform Child(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null) return child;

            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Build Overflow Row");
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
