using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SlideJam.Game
{
    public class HudView : MonoBehaviour
    {
        [SerializeField] private GameController _controller;
        [SerializeField] private TMP_Text _movesText;
        [SerializeField] private string _movesFormat = "Moves: {0}";
        
        private void Start()
        {
            _controller.StateChanged += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (_controller != null) _controller.StateChanged -= Refresh;
        }

        private void Refresh()
        {
            if (_controller.State == null) return;

            _movesText.text = string.Format(_movesFormat, _controller.State.Moves);
        }
    }
}
