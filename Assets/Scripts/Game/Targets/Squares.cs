using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SlideJam.Game
{
    public class Squares : MonoBehaviour
    {
        public static SpriteRenderer At(List<SpriteRenderer> list, int index, Transform parent, Sprite sprite, int sortingOrder)
        {
            while (list.Count <= index)
            {
                var go = new GameObject("Square");
#if UNITY_EDITOR
                if (!Application.isPlaying) go.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
#endif
                go.transform.SetParent(parent, false);
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sortingOrder = sortingOrder;
                list.Add(renderer);
            }

            return list[index];
        }
        
        public static void Prepare(Transform parent, params List<SpriteRenderer>[] lists)
        {
            bool stale = false;
            foreach (List<SpriteRenderer> list in lists)
            {
                if (list.Count == 0 || list.Contains(null)) stale = true;
            }

            if (!stale) return;

            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                GameObject child = parent.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }

            foreach (List<SpriteRenderer> list in lists) list.Clear();
        }

        public static void Set(SpriteRenderer square, Vector3 position, float width, float height, Color color)
        {
            square.transform.localPosition = position;
            square.transform.localScale = new Vector3(width, height, 1f);
            square.color = color;
            square.gameObject.SetActive(true);
        }

        public static void HideFrom(List<SpriteRenderer> list, int first)
        {
            for (int i = first; i < list.Count; i++) list[i].gameObject.SetActive(false);
        }
        
        public static void SetFit(SpriteRenderer square, Vector3 position, float width, Color color)
        {
            square.transform.localPosition = position;
            float scale = width / square.sprite.bounds.size.x;
            square.transform.localScale = new Vector3(scale, scale, 1f);
            square.color = color;
            square.gameObject.SetActive(true);
        }
    }
}
