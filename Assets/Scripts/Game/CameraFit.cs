using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SlideJam.Game
{
    [RequireComponent(typeof(Camera))]
    public class CameraFit : MonoBehaviour
    {
        [SerializeField] private float _margin = 0.5f;
        
        private Camera _camera;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
        }

        public void Fit(int cols, int rows)
        {
            float byWidth = (cols + 2f * _margin) / (2f * _camera.aspect);
            float byHeight = (rows + 2f * _margin) / 2f;
            _camera.orthographicSize = Mathf.Max(byWidth, byHeight);
        }
    }
}
