using SlideJam.Game;
using UnityEditor;
using UnityEngine;

namespace SlideJam.EditorTools
{
    internal static class SpawnDotsBuilder
    {
        private const string Sprites = "Assets/Art/Sprites/";
        private const string SkinPath = "Assets/Design/ThemeSkin.asset"; // added

        internal static bool Build(GameController controller)
        {
            var stage = Ref<StageLayout>(controller, "_stageLayout");
            if (stage == null)
            {
                Debug.LogError("Setup Scene: GameController has an empty StageLayout reference.");
                return false;
            }

            var hanger = AssetDatabase.LoadAssetAtPath<Sprite>(Sprites + "scene/branch_hanger.png");
            if (hanger == null) Debug.LogWarning("Setup Scene: the sprite scene/branch_hanger was not found; the dots hang without a hanger.");

            Transform anchor = Ref<Transform>(stage, "_dots");
            if (anchor == null) anchor = Child(stage.transform, "Dots");
            SetRef(stage, "_dots", anchor);

            int copies = 0; // added from here: a second Dots object makes the layout and the references ambiguous
            foreach (Transform child in stage.transform)
            {
                if (child.name == "Dots") copies++;
            }

            if (copies > 1) Debug.LogWarning("Setup Scene: the Stage has " + copies + " objects named Dots. Delete the extra ones; StageLayout uses the one it references.");

            Transform staleTiles = anchor.Find("Pool_Tiles"); // the dots have no tile background any more
            if (staleTiles != null) Undo.DestroyObjectImmediate(staleTiles.gameObject);

            var view = anchor.GetComponent<NextSpawnView>();
            if (view == null) view = Undo.AddComponent<NextSpawnView>(anchor.gameObject);

            var skin = AssetDatabase.LoadAssetAtPath<ThemeSkin>(SkinPath);
            if (skin != null) SetRef(view, "_skin", skin); // changed: the dots need the skin whatever the order of the builders

            SetRef(view, "_hanger", hanger);
            SetRef(controller, "_nextSpawnView", view);

            EditorUtility.SetDirty(stage);
            EditorUtility.SetDirty(view);
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