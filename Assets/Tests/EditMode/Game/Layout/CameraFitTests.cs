using NUnit.Framework;
using SlideJam.Game;
using UnityEngine;

namespace SlideJam.Tests.EditMode
{
    public class CameraFitTests
    {
        private static readonly Rect Bounds = new Rect(-3f, -5f, 6f, 10f);

        [Test]
        public void Compute_ANarrowScreenIsLimitedByWidth()
        {
            CameraFit.Compute(Bounds, 0.5f, 0.5f, 0f, 0f, out float size, out _);

            Assert.AreEqual(7f, size, 0.001f); // (6 + 1) / (2 * 0.5)
        }

        [Test]
        public void Compute_AWideScreenIsLimitedByHeight()
        {
            CameraFit.Compute(Bounds, 2f, 0.5f, 0f, 0f, out float size, out _);

            Assert.AreEqual(5.5f, size, 0.001f); // (10 + 1) / 2
        }

        [Test]
        public void Compute_PaddingGrowsTheViewAndMovesTheCentre()
        {
            CameraFit.Compute(Bounds, 2f, 0.5f, 1f, 2f, out float size, out Vector2 center);

            Assert.AreEqual(7f, size, 0.001f); // (10 + 1 + 2 + 1) / 2
            Assert.AreEqual(0f, center.x, 0.001f);
            Assert.AreEqual(-0.5f, center.y, 0.001f); // (5 + 1 - 5 - 2) / 2
        }
    }
}