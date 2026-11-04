using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SlideJam.Game
{
    public class HudView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _movesText;
        [SerializeField] private string _movesFormat = "Moves: {0}";

        public void ShowMoves(int moves)
        {
            _movesText.text = string.Format(_movesFormat, moves);
        }
    }
}
