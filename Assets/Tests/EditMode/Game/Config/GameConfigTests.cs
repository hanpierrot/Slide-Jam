using NUnit.Framework;
using SlideJam.Game;
using UnityEngine;

namespace SlideJam.Tests.EditMode
{
    public class GameConfigTests
    {
        private GameConfig _config;
        private ThemeSkin _skin;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<GameConfig>();
            _skin = ScriptableObject.CreateInstance<ThemeSkin>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
            Object.DestroyImmediate(_skin);
        }

        [Test]
        public void Defaults_KeepTheRuleLimits()
        {
            Assert.AreEqual(3, _config.CapMin);
            Assert.AreEqual(5, _config.CapMax);
        }

        [Test]
        public void Defaults_KeepTheTimingsTheSceneUsedBefore()
        {
            Assert.AreEqual(0.13f, _config.SnapSeconds, 0.0001f);
            Assert.AreEqual(0.14f, _config.SnapBackSeconds, 0.0001f);
            Assert.AreEqual(0.17f, _config.ClearSeconds, 0.0001f);
            Assert.AreEqual(1.15f, _config.PulseScale, 0.0001f);
            Assert.AreEqual(0.16f, _config.FallBaseSeconds, 0.0001f);
            Assert.AreEqual(0.09f, _config.FallPerCellSeconds, 0.0001f);
            Assert.AreEqual(0.35f, _config.ReviveSeconds, 0.0001f);
        }

        [Test]
        public void Defaults_KeepTheInputAndCapacityValues()
        {
            Assert.AreEqual(8f, _config.LockDistance, 0.0001f);
            Assert.AreEqual(360f, _config.ReferenceWidth, 0.0001f);
            Assert.AreEqual(6, _config.QueueUnitBuffer);
            Assert.AreEqual(5, _config.StripVisible);
        }

        [Test]
        public void ThemeSkin_DefaultsHoldFiveColoursAndTheBasketLayout()
        {
            Assert.AreEqual(5, _skin.PaletteLength);
            Assert.AreEqual(new Color(0.90f, 0.25f, 0.30f), _skin.ColorOf(0));
            Assert.AreEqual(0.6f, _skin.Basket.InteriorWidth, 0.0001f);
            Assert.AreEqual(0.053f, _skin.Basket.PipY, 0.0001f);
            Assert.AreEqual(0.213f, _skin.Basket.PipWidth, 0.0001f);
            Assert.AreEqual(0.95f, _skin.Basket.PipFill, 0.0001f);
            Assert.AreEqual(0.9f, _skin.Basket.FruitInPip, 0.0001f);
        }
    }
}
