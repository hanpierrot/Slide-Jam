using System;
using System.Collections.Generic;
using NUnit.Framework;
using SlideJam.Engine;
using SlideJam.Game;
using UnityEditor;

namespace SlideJam.Tests.EditMode
{
    public class LevelConverterTests
    {
        private static List<TargetEntry> Entries(params int[] caps)
        {
            var list = new List<TargetEntry>();
            for (int i = 0; i < caps.Length; i++) list.Add(new TargetEntry { Color = i % 5, Cap = caps[i] });
            return list;
        }

        private static LevelData Convert(List<TargetEntry> targets, int capMin = 3, int capMax = 5)
        {
            return LevelConverter.Convert("Test", 6, 7, 5, 20261031, 3, 6, 4, true, targets, capMin, capMax);
        }

        [Test]
        public void Convert_CopiesEveryField()
        {
            LevelData level = Convert(Entries(3, 4, 5));

            Assert.AreEqual(6, level.Rows);
            Assert.AreEqual(7, level.Cols);
            Assert.AreEqual(5, level.Colors);
            Assert.AreEqual(20261031u, level.Seed);
            Assert.AreEqual(3, level.ActiveSlots);
            Assert.AreEqual(6, level.QueueMax);
            Assert.AreEqual(4, level.QOpenStart);
            Assert.IsTrue(level.QGrow);
            Assert.AreEqual(3, level.Seq.Count);
            Assert.AreEqual(1, level.Seq[1].Color);
            Assert.AreEqual(4, level.Seq[1].Cap);
            Assert.AreEqual(5, level.Seq[2].Cap);
        }

        [Test]
        public void Convert_RejectsACapOutsideTheRangeAndNamesTheTarget()
        {
            foreach (int bad in new[] { 2, 6 })
            {
                var ex = Assert.Throws<ArgumentOutOfRangeException>(() => Convert(Entries(3, bad)));
                StringAssert.Contains("target 1", ex.Message);
                StringAssert.Contains("Test", ex.Message);
            }
        }

        [Test]
        public void Convert_ReadsTheRangeFromTheConfigValues()
        {
            Assert.DoesNotThrow(() => Convert(Entries(2, 6), 2, 6));
            Assert.Throws<ArgumentOutOfRangeException>(() => Convert(Entries(3), 4, 5));
        }

        [Test]
        public void Convert_RejectsAnInvertedRange()
        {
            Assert.Throws<ArgumentException>(() => Convert(Entries(3), 5, 3));
        }

        [Test]
        public void ShippedLevels_ConvertWithTheShippedGameConfigAndStartWithoutAMatch()
        {
            // changed: found by type, so the test does not depend on which folder the asset lives in
            string[] configGuids = AssetDatabase.FindAssets("t:GameConfig");
            Assert.AreEqual(1, configGuids.Length, "Expected exactly one GameConfig asset.");
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(AssetDatabase.GUIDToAssetPath(configGuids[0]));

            string[] guids = AssetDatabase.FindAssets("t:LevelConfig");
            Assert.IsNotEmpty(guids, "No LevelConfig asset found.");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var levelConfig = AssetDatabase.LoadAssetAtPath<LevelConfig>(path);

                LevelData level = levelConfig.ToLevelData(config);
                BoardState state = BoardState.New(level);

                Assert.IsFalse(Matcher.HasMatch(state.Grid), path);
            }
        }
    }
}