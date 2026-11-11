using NUnit.Framework;
using SlideJam.Game;
using UnityEngine;

namespace SlideJam.Tests.EditMode
{
    public class StageLayoutTests
    {
        private GameObject _go;
        private StageLayout _layout;
        private StageLayoutData _data;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("Stage");
            _layout = _go.AddComponent<StageLayout>();
            _data = ScriptableObject.CreateInstance<StageLayoutData>();
            _data.SetPositions(new Vector3(0f, 5f, 0f), Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero, new Vector3(0f, -1f, 0f));
            _layout.SetData(_data);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_data);
        }

        // Changed with the layout data: the rectangle runs from the top of the slots band to the bottom of the board
        [Test]
        public void Measure_BoundsRunFromTheTopOfTheSlotsToTheBottomOfTheBoard()
        {
            Rect bounds = _layout.Measure(6, 6);

            float top = 5f + _data.SlotsHeight / 2f;
            float bottom = -1f - 3f;
            Assert.AreEqual(6f, bounds.width, 0.001f);
            Assert.AreEqual(top - bottom, bounds.height, 0.001f);
            Assert.AreEqual(bottom, bounds.yMin, 0.001f);
            Assert.AreEqual(top, bounds.yMax, 0.001f);
        }

        // Scales are saved with the positions and Apply puts them on the anchors; data without scales leaves the anchors' scales alone
        [Test]
        public void Apply_PutsSavedScalesOnTheAnchors_AndSkipsThemWhenTheDataHasNone()
        {
            var slots = new GameObject("Slots").transform;
            var so = new UnityEditor.SerializedObject(_layout);
            so.FindProperty("_slots").objectReferenceValue = slots;
            so.ApplyModifiedProperties();
            slots.localScale = new Vector3(2f, 2f, 1f);

            _layout.Apply(6, 6);
            Assert.AreEqual(new Vector3(2f, 2f, 1f), slots.localScale);

            _data.SetScales(new Vector3(0.5f, 0.5f, 1f), Vector3.one, Vector3.one, Vector3.one, Vector3.one, Vector3.one);
            _layout.Apply(6, 6);
            Assert.AreEqual(new Vector3(0.5f, 0.5f, 1f), slots.localScale);

            Object.DestroyImmediate(slots.gameObject);
        }

        // Changed: the Grid anchor is the board's centre, so an extra row lowers the bottom edge by half a cell
        [Test]
        public void Measure_AnExtraRowLowersTheBottomByHalfACell()
        {
            float six = _layout.Measure(6, 6).height;
            float seven = _layout.Measure(7, 7).height;

            Assert.AreEqual(0.5f, seven - six, 0.001f);
        }
    }
}
