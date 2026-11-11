using SlideJam.Game;
using UnityEditor;
using UnityEngine;

namespace SlideJam.EditorTools
{
    public static class SceneBuilder
    {
        private const string Sprites = "Assets/Art/Sprites/";
        private const string SkinPath = "Assets/Design/ThemeSkin.asset";
        private const string BlockPrefabPath = "Assets/Prefabs/Block.prefab";
        private const string BackdropName = "Backdrop";
        private const string BranchName = "Branch";

        private static readonly string[] BasketNames = { "cherry", "blueberry", "pear", "orange", "plum" };
        private static readonly string[] FruitFiles = { "cherries", "blueberries", "pear", "orange", "plum" };

        private static readonly Color PageColor = new Color32(234, 248, 230, 255);

        internal static bool BuildArt(GameController controller)
        {
            var boardView = Ref<BoardView>(controller, "_boardView");
            var cameraFit = Ref<CameraFit>(controller, "_cameraFit");
            var stage = Ref<StageLayout>(controller, "_stageLayout");
            var slotsView = Ref<SlotsView>(controller, "_slotsView");
            var queueView = Ref<QueueView>(controller, "_queueView");
            var stripView = Ref<StripView>(controller, "_stripView");
            var levelConfig = Ref<LevelConfig>(controller, "_levelConfig");
            if (boardView == null || cameraFit == null || stage == null || slotsView == null || queueView == null
                || stripView == null || levelConfig == null)
            {
                Debug.LogError("Setup Scene: GameController has an empty view reference. Assign them first.");
                return false;
            }

            ThemeSkin skin = EnsureSkin();
            PatchBlockPrefab();

            SetRef(boardView, "_skin", skin);
            SetRef(slotsView, "_skin", skin);
            SetRef(stripView, "_skin", skin);
            SetRef(queueView, "_skin", skin);
            SetRef(queueView, "_openSprite", skin.QueueOpen);
            SetRef(queueView, "_lockedSprite", skin.QueueLocked);
            var nextSpawn = Ref<NextSpawnView>(controller, "_nextSpawnView");
            if (nextSpawn != null) SetRef(nextSpawn, "_skin", skin);
            
            BuildBackdrop(stage, boardView, levelConfig.Rows, levelConfig.Cols);

            cameraFit.Cam.clearFlags = CameraClearFlags.SolidColor;
            cameraFit.Cam.backgroundColor = PageColor;
            EditorUtility.SetDirty(cameraFit.Cam);
            AssetDatabase.SaveAssets();
            return true;
        }

        internal static bool RefreshBackdrop(GameController controller)
        {
            var boardView = Ref<BoardView>(controller, "_boardView");
            var stage = Ref<StageLayout>(controller, "_stageLayout");
            var levelConfig = Ref<LevelConfig>(controller, "_levelConfig");
            if (boardView == null || stage == null || levelConfig == null)
            {
                Debug.LogError("Refresh Layout: GameController has an empty BoardView, StageLayout or LevelConfig reference.");
                return false;
            }

            BuildBackdrop(stage, boardView, levelConfig.Rows, levelConfig.Cols);
            return true;
        }

        internal static void ApplySpriteQuality()
        {
            int changed = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Art/Sprites" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null || importer.textureType != TextureImporterType.Sprite) continue;

                bool dirty = !importer.mipmapEnabled || importer.filterMode != FilterMode.Trilinear || !importer.alphaIsTransparency;
                if (!dirty) continue;

                importer.mipmapEnabled = true;
                importer.filterMode = FilterMode.Trilinear;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
                changed++;
            }

            Debug.Log("Apply Sprite Quality: " + changed + " textures updated.");
        }

        private static ThemeSkin EnsureSkin()
        {
            var skin = AssetDatabase.LoadAssetAtPath<ThemeSkin>(SkinPath);
            if (skin == null)
            {
                skin = ScriptableObject.CreateInstance<ThemeSkin>();
                AssetDatabase.CreateAsset(skin, SkinPath);
            }

            var so = new SerializedObject(skin);
            so.FindProperty("_queueOpen").objectReferenceValue = Load("queue/queue_slot_open");
            so.FindProperty("_queueLocked").objectReferenceValue = Load("queue/queue_slot_locked");
            so.FindProperty("_basketShadow").objectReferenceValue = Load("basket/basket_shadow");
            so.FindProperty("_basketFront").objectReferenceValue = Load("basket/basket_front");

            SerializedProperty fruits = so.FindProperty("_fruits");
            fruits.arraySize = BasketNames.Length;
            for (int i = 0; i < BasketNames.Length; i++)
            {
                SerializedProperty fruit = fruits.GetArrayElementAtIndex(i);
                string name = BasketNames[i];
                fruit.FindPropertyRelative("Icon").objectReferenceValue = Load("fruits/" + FruitFiles[i]);
                fruit.FindPropertyRelative("BasketBack").objectReferenceValue = Load("basket/basket_" + name + "_back");
                fruit.FindPropertyRelative("BasketEmpty").objectReferenceValue = Load("basket/basket_" + name + "_empty");
                fruit.FindPropertyRelative("BasketLocked").objectReferenceValue = Load("basket/basket_" + name + "_locked");
                fruit.FindPropertyRelative("PipEmpty").objectReferenceValue = Load("basket/pip_empty_" + name);
                fruit.FindPropertyRelative("Chip").objectReferenceValue = Load("shelf/shelf_chip_" + name);
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(skin);
            return skin;
        }

        private static void PatchBlockPrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(BlockPrefabPath);
            try
            {
                var tile = root.GetComponent<SpriteRenderer>();
                tile.sprite = Load("board/tile_bg_48");
                tile.color = Color.white;
                root.transform.localScale = Vector3.one * 0.96f;

                Transform iconTransform = root.transform.Find("Icon");
                if (iconTransform == null)
                {
                    var created = new GameObject("Icon");
                    created.transform.SetParent(root.transform, false);
                    iconTransform = created.transform;
                }

                iconTransform.localPosition = Vector3.zero;
                iconTransform.localScale = Vector3.one;

                var icon = iconTransform.GetComponent<SpriteRenderer>();
                if (icon == null) icon = iconTransform.gameObject.AddComponent<SpriteRenderer>();
                icon.sortingLayerID = tile.sortingLayerID;
                icon.sortingOrder = tile.sortingOrder + 1;
                icon.maskInteraction = tile.maskInteraction;
                icon.enabled = false;

                SetRef(root.GetComponent<BlockView>(), "_icon", icon);
                PrefabUtility.SaveAsPrefabAsset(root, BlockPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void BuildBackdrop(StageLayout stage, BoardView boardView, int rows, int cols)
        {
            if (rows != 6) Debug.LogWarning("Build Scene Art: only the 6x6 board art is wired (PPU 216). Rows = " + rows);

            var slots = Ref<Transform>(stage, "_slots");
            var strip = Ref<Transform>(stage, "_strip");
            var grid = Ref<Transform>(stage, "_grid");
            var branch = Ref<Transform>(stage, "_branch");
            if (branch == null)
            {
                branch = Child(stage.transform, BranchName);
                SetRef(stage, "_branch", branch);
            }

            stage.Apply(rows, cols);

            Transform backdrop = Child(stage.transform, BackdropName);
            backdrop.localPosition = Vector3.zero;

            Put(backdrop, "Sky", "scene/sky_panel", new Vector3(0f, slots.localPosition.y - 0.55f, 0f), -20);
            Put(backdrop, "Plank", "shelf/shelf_plank", new Vector3(0f, strip.localPosition.y - 0.3f, 0f), -10);
            Put(branch, "BranchArt", "scene/branch", Vector3.zero, -5);

            string size = rows + "x" + rows;
            Put(grid, "Panel", "board/board_panel_" + size, Vector3.zero, -10);
            Put(grid, "Vines", "board/board_vines_" + size, Vector3.zero, 10);

            Transform background = boardView.transform.Find("Background");
            if (background != null && background.TryGetComponent(out SpriteRenderer plain)) plain.enabled = false;
        }

        private static Transform Child(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null) return child;

            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Build Scene Art");
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static void Put(Transform parent, string name, string spritePath, Vector3 localPosition, int order)
        {
            Transform t = Child(parent, name);
            var renderer = t.GetComponent<SpriteRenderer>();
            if (renderer == null) renderer = t.gameObject.AddComponent<SpriteRenderer>();

            renderer.sprite = Load(spritePath);
            renderer.sortingOrder = order;
            t.localPosition = localPosition;
            t.localScale = Vector3.one;
        }
        
        private static void PutLeaves(Transform branch, Sprite art)
        {
            float half = art != null ? art.bounds.size.x / 2f : 3f;
            PutLeaf(branch, "LeafLeft", new Vector3(-half + 0.15f, 0.08f, 0f), 1f);
            PutLeaf(branch, "LeafRight", new Vector3(half - 0.15f, 0.08f, 0f), -1f);
        }

        private static void PutLeaf(Transform branch, string name, Vector3 position, float side)
        {
            Put(branch, name, "scene/branch_leaf", position, -4);
            branch.Find(name).localScale = new Vector3(side, 1f, 1f);
        }

        private static Sprite Load(string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Sprites + path + ".png");
            if (sprite == null) Debug.LogWarning("Build Scene Art: sprite not found: " + Sprites + path + ".png");
            return sprite;
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

        private static void SetFloat(Object owner, string field, float value)
        {
            var so = new SerializedObject(owner);
            so.FindProperty(field).floatValue = value;
            so.ApplyModifiedProperties();
        }

        private static void SetInt(Object owner, string field, int value)
        {
            var so = new SerializedObject(owner);
            so.FindProperty(field).intValue = value;
            so.ApplyModifiedProperties();
        }
    }
}
