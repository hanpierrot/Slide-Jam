using TMPro;
using UnityEngine;

namespace SlideJam.Game
{
    public class HudView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _levelText;
        [SerializeField] private string _levelFormat = "Level {0}";
        [SerializeField] private TMP_Text _targetsText;
        [SerializeField] private string _targetsFormat = "Targets left: {0}";

        public void ShowLevel(int number)
        {
            if (_levelText != null) _levelText.text = string.Format(_levelFormat, number);
        }

        public void ShowTargetsLeft(int left)
        {
            if (_targetsText != null) _targetsText.text = string.Format(_targetsFormat, left);
        }
    }
}