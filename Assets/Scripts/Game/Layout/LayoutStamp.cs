using System.Globalization;
using System.Text;
using UnityEngine;

namespace SlideJam.Game
{
    // A stamp of the numbers a view laid its pools out with. The Edit Mode builds (Setup Scene, Refresh Layout) store it in the scene;
    // in Play mode a view compares it with the numbers of the level it is about to draw, and keeps the scene as it is when they match.
    public static class LayoutStamp
    {
        public static string Of(params float[] values)
        {
            var text = new StringBuilder();
            foreach (float value in values)
            {
                if (text.Length > 0) text.Append('|');
                text.Append(value.ToString("0.###", CultureInfo.InvariantCulture));
            }

            return text.ToString();
        }

        // Play mode only: true when the scene was set up for exactly this layout, so the pools keep the transforms they have
        public static bool CanKeep(Object owner, string saved, string current)
        {
            if (!Application.isPlaying) return false;
            if (!string.IsNullOrEmpty(saved) && saved == current) return true;

            string was = string.IsNullOrEmpty(saved) ? "no saved layout" : "the layout " + saved;
            Debug.LogWarning(owner.name + ": the scene was set up for " + was + ", this level needs " + current
                             + ". The layout is computed for this run only; run Slide Jam > Setup Scene to save it.", owner);
            return false;
        }

        // Edit Mode only: the Setup Scene and Refresh Layout builds store the stamp in the view
        public static void Record(Object owner, ref string saved, string current)
        {
#if UNITY_EDITOR
            if (Application.isPlaying || saved == current) return;

            saved = current;
            UnityEditor.EditorUtility.SetDirty(owner);
#endif
        }
    }
}
