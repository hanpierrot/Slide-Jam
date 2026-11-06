using System;
using System.Collections;
using System.Collections.Generic;
using SlideJam.Engine;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SlideJam.Game
{
    public class EndOverlayView : MonoBehaviour
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _detailText;
        [SerializeField] private Button _reviveButton;
        [SerializeField] private Button _restartButton;
        [SerializeField] private string _winTitle = "Level complete";
        [SerializeField] private string _winDetail = "{0} slides";
        [SerializeField] private string _lossTitle = "Out of room";
        [SerializeField] private string _lossDetail = "The queue is full";

        public event Action ReviveClicked;
        public event Action RestartClicked;

        private void Awake()
        {
            _reviveButton.onClick.AddListener(() => ReviveClicked?.Invoke());
            _restartButton.onClick.AddListener(() => RestartClicked?.Invoke());
            Hide();
        }
        
        public void Show(EndReason reason, int moves, bool canRevive)
        {
            bool win = reason == EndReason.Win;
            _titleText.text = win ? _winTitle : _lossTitle;
            _detailText.text = win ? string.Format(_winDetail, moves) : _lossDetail;
            _reviveButton.gameObject.SetActive(!win && canRevive);
            _panel.SetActive(true);
        }

        public void Hide()
        {
            _panel.SetActive(false);
        }
    }
}
