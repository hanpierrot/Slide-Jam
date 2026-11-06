using SlideJam.Game;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace SlideJam.EditorTools
{
    public static class OverlayBuilder
    {
        private const string Sprites = "Assets/Art/Sprites/";
        private static readonly Color Ink = new Color32(82, 51, 79, 255);

        [MenuItem("Slide Jam/Build End Overlay")]
        public static void Build()
        {
            var view = Object.FindObjectOfType<EndOverlayView>(true);
            if (view == null)
            {
                Debug.LogError("Build End Overlay: no EndOverlayView in the open scene.");
                return;
            }

            GameObject root = view.gameObject;
            root.SetActive(true); // Awake of the view must run so the buttons get their listeners; the Panel below is what toggles
            Stretch((RectTransform)root.transform);

            RectTransform panel = UiChild(root.transform, "Panel");
            Stretch(panel);

            RectTransform dim = (RectTransform)(root.transform.Find("Dim") ?? panel.Find("Dim"));
            if (dim == null) dim = UiChild(panel, "Dim");
            dim.SetParent(panel, false);
            Stretch(dim);
            var dimImage = dim.GetComponent<Image>();
            if (dimImage == null) dimImage = dim.gameObject.AddComponent<Image>();
            dimImage.sprite = null;
            dimImage.color = new Color(0f, 0f, 0f, 0.6f);
            dimImage.raycastTarget = true;
            dim.SetAsFirstSibling();

            RectTransform card = UiChild(panel, "Card");
            Center(card, new Vector2(230f, 176f), Vector2.zero);
            var cardImage = Ensure<Image>(card);
            cardImage.sprite = Load("ui/card_9slice");
            cardImage.type = Image.Type.Simple;
            cardImage.preserveAspect = true;
            cardImage.color = Color.white;

            TextMeshProUGUI title = Label(card, "TitleText", "Level complete", 24f, FontStyles.Bold);
            Center(title.rectTransform, new Vector2(200f, 36f), new Vector2(0f, 52f));
            TextMeshProUGUI detail = Label(card, "DetailText", "0 slides", 16f, FontStyles.Normal);
            Center(detail.rectTransform, new Vector2(200f, 26f), new Vector2(0f, 14f));

            RectTransform row = UiChild(card, "Buttons");
            Center(row, new Vector2(210f, 46f), new Vector2(0f, -46f));
            var layout = Ensure<HorizontalLayoutGroup>(row);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 8f;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            Button revive = MakeButton(row, "ReviveButton", "Revive");
            Button restart = MakeButton(row, "RestartButton", "Restart");

            SetRef(view, "_panel", panel.gameObject);
            SetRef(view, "_titleText", title);
            SetRef(view, "_detailText", detail);
            SetRef(view, "_reviveButton", revive);
            SetRef(view, "_restartButton", restart);

            var controller = Object.FindObjectOfType<GameController>();
            if (controller != null) SetRef(controller, "_endOverlay", view);

            panel.gameObject.SetActive(false);
            EditorSceneManager.MarkSceneDirty(root.scene);
            Debug.Log("Build End Overlay: done. Save the scene (Ctrl+S).");
        }

        private static Button MakeButton(Transform parent, string name, string text)
        {
            RectTransform rect = UiChild(parent, name);
            rect.sizeDelta = new Vector2(100f, 46f);

            var image = Ensure<Image>(rect);
            image.sprite = Load("ui/button_9slice_normal");
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.color = Color.white;

            var button = Ensure<Button>(rect);
            button.targetGraphic = image;
            button.transition = Selectable.Transition.SpriteSwap;
            SpriteState states = button.spriteState;
            states.pressedSprite = Load("ui/button_9slice_pressed");
            button.spriteState = states;

            TextMeshProUGUI label = Label(rect, "Label", text, 16f, FontStyles.Bold);
            Stretch(label.rectTransform);
            return button;
        }

        private static TextMeshProUGUI Label(Transform parent, string name, string text, float size, FontStyles style)
        {
            RectTransform rect = UiChild(parent, name);
            var tmp = Ensure<TextMeshProUGUI>(rect);
            tmp.text = text;
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Ink;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static RectTransform UiChild(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null) return (RectTransform)existing;

            var go = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "Build End Overlay");
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static T Ensure<T>(Component owner) where T : Component
        {
            var existing = owner.GetComponent<T>();
            return existing != null ? existing : owner.gameObject.AddComponent<T>();
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void Center(RectTransform rect, Vector2 size, Vector2 position)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        private static Sprite Load(string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Sprites + path + ".png");
            if (sprite == null) Debug.LogWarning("Build End Overlay: sprite not found: " + Sprites + path + ".png");
            return sprite;
        }

        private static void SetRef(Object owner, string field, Object value)
        {
            var so = new SerializedObject(owner);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedProperties();
        }
    }
}
