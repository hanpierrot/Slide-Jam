using SlideJam.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SlideJam.EditorTools
{
    // The only editor menus of the project. Both commands are idempotent: running them again changes nothing that is already right.
    public static class SlideJamMenu
    {
        // Everything the scene needs, in dependency order: sprite import settings, end overlay, overflow row,
        // skin and block prefab and backdrop, then the Edit Mode preview of the slot, queue and strip objects
        [MenuItem("Slide Jam/Setup Scene")]
        public static void SetupScene()
        {
            if (!TryGetController(out GameController controller)) return;

            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Setup Scene");

            SceneBuilder.ApplySpriteQuality();
            bool ok = OverlayBuilder.Build();
            ok &= OverflowBuilder.Build(controller);
            ok &= SceneBuilder.BuildArt(controller);
            if (ok) controller.PreviewLayout();

            Undo.CollapseUndoOperations(group);
            EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
            Debug.Log(ok
                ? "Setup Scene: done. Check the Game view, then save the scene (Ctrl+S)."
                : "Setup Scene: stopped at an error, see the messages above.");
        }

        // After a change to the layout numbers (StageLayout, the level size, GameConfig capacity): lays the Stage out again,
        // moves the static backdrop and rebuilds the slot, queue and strip objects. No asset or prefab is touched.
        [MenuItem("Slide Jam/Refresh Layout")]
        public static void RefreshLayout()
        {
            if (!TryGetController(out GameController controller)) return;

            if (SceneBuilder.RefreshBackdrop(controller)) controller.PreviewLayout();
            EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
        }

        private static bool TryGetController(out GameController controller)
        {
            controller = Object.FindObjectOfType<GameController>();
            if (controller == null)
            {
                Debug.LogError("Slide Jam: no GameController in the open scene.");
                return false;
            }

            if (Application.isPlaying)
            {
                Debug.LogWarning("Slide Jam: leave Play mode before using the setup menus.");
                return false;
            }

            return true;
        }
    }
}
