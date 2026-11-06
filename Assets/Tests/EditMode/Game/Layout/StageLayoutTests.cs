using NUnit.Framework;
using SlideJam.Game;
using UnityEngine;

namespace SlideJam.Tests.EditMode
{
    public class StageLayoutTests
    {
        private GameObject _go;
        private StageLayout _layout;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("Stage");
            _layout = _go.AddComponent<StageLayout>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
        }

        [Test]
        public void Apply_BoundsCoverEveryBand()
        {
            Rect bounds = _layout.Apply(6, 6);

            float expected = _layout.SlotsHeight + _layout.StripHeight + _layout.QueueHeight + 6f + 3f * _layout.Gap;
            Assert.AreEqual(6f, bounds.width, 0.001f);
            Assert.AreEqual(expected, bounds.height, 0.001f);
            Assert.AreEqual(0f, bounds.center.y, 0.001f);
        }

        [Test]
        public void Apply_AnExtraRowAddsExactlyOneCell()
        {
            float six = _layout.Apply(6, 6).height;
            float seven = _layout.Apply(7, 7).height;

            Assert.AreEqual(1f, seven - six, 0.001f);
        }
    }
}