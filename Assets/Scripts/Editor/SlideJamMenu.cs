using SlideJam.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SlideJam.EditorTools
{
    public static class SlideJamMenu
    {
        [MenuItem("Slide Jam/Setup Scene")]
        public static void SetupScene()
        {
            if (!TryGetController(out GameController controller)) return;

            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Setup Scene");

            StageLayoutTool.CaptureItemsIfMissing(controller); // added: before any builder touches the scene, so hand-made changes are not lost
            SceneBuilder.ApplySpriteQuality();
            bool ok = OverlayBuilder.Build();
            ok &= QueueBoxBuilder.Build(controller);
            ok &= PreviewBuilder.Build(controller);
            ok &= SpawnDotsBuilder.Build(controller); // changed: before BuildArt, so the layout and the skin wiring already see the Dots anchor
            ok &= StageLayoutTool.Ensure(controller) != null; // added: the layout data, created from the scene the first time
            ok &= SceneBuilder.BuildArt(controller);
            if (ok)
            {
                controller.PreviewLayout();
                StageLayoutTool.ApplyItems(controller); // added: the saved layout of every object below the anchors wins over what was just computed
            }

            Undo.CollapseUndoOperations(group);
            EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
            Debug.Log(ok
                ? "Setup Scene: done. Check the Game view, then save the scene (Ctrl+S)."
                : "Setup Scene: stopped at an error, see the messages above.");
        }

        [MenuItem("Slide Jam/Refresh Layout")]
        public static void RefreshLayout()
        {
            if (!TryGetController(out GameController controller)) return;

            StageLayoutTool.CaptureItemsIfMissing(controller); // added
            if (StageLayoutTool.Ensure(controller) != null && SceneBuilder.RefreshBackdrop(controller))
            {
                controller.PreviewLayout();
                StageLayoutTool.ApplyItems(controller); // added
            }

            EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
        }

        // added: writes the current anchor positions of the Stage into the StageLayoutData
        [MenuItem("Slide Jam/Save Stage Layout")]
        public static void SaveStageLayout()
        {
            if (!TryGetController(out GameController controller)) return;

            StageLayoutTool.Save(controller);
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
