using NUnit.Framework;
using SlideJam.Game;
using UnityEngine;

namespace SlideJam.Tests.EditMode
{
    public class SquarePoolTests
    {
        private GameObject _owner;
        private Sprite _twoUnits;

        [SetUp]
        public void SetUp()
        {
            SquarePool.TrackEdits = false;
            _owner = new GameObject("Owner");
            var texture = new Texture2D(4, 4);
            _twoUnits = Sprite.Create(texture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 2f);
        }

        [TearDown]
        public void TearDown()
        {
            SquarePool.TrackEdits = true;
            Object.DestroyImmediate(_owner);
        }

        [Test]
        public void Warm_CreatesInactiveSquaresUpToTheCount()
        {
            SquarePool pool = SquarePool.Obtain(null, _owner.transform, "T", 3);

            pool.Warm(5);

            Assert.AreEqual(5, pool.Count);
            Assert.AreEqual(5, pool.Warmed);
            for (int i = 0; i < 5; i++) Assert.IsFalse(pool.Get(i).gameObject.activeSelf);
        }

        [Test]
        public void Get_ReturnsTheSameSquareForTheSameIndex()
        {
            SquarePool pool = SquarePool.Obtain(null, _owner.transform, "T", 3);

            Assert.AreSame(pool.Get(2), pool.Get(2));
            Assert.AreEqual(3, pool.Count);
        }

        [Test]
        public void Fit_KeepsTheAspectAndSetsTheOrder()
        {
            SquarePool pool = SquarePool.Obtain(null, _owner.transform, "T", 4);

            SpriteRenderer square = pool.Fit(0, _twoUnits, new Vector3(1f, 2f, 0f), 1f, Color.red);

            Assert.IsTrue(square.gameObject.activeSelf);
            Assert.AreEqual(0.5f, square.transform.localScale.x, 0.0001f);
            Assert.AreEqual(0.5f, square.transform.localScale.y, 0.0001f);
            Assert.AreEqual(new Vector3(1f, 2f, 0f), square.transform.localPosition);
            Assert.AreEqual(4, square.sortingOrder);
            Assert.AreEqual(Color.red, square.color);
        }

        [Test]
        public void Stretch_UsesTheGivenWidthAndHeight()
        {
            SquarePool pool = SquarePool.Obtain(null, _owner.transform, "T", 0);

            SpriteRenderer square = pool.Stretch(0, _twoUnits, Vector3.zero, 3f, 2f, Color.white);

            Assert.AreEqual(new Vector3(3f, 2f, 1f), square.transform.localScale);
        }

        [Test]
        public void Fit_WithoutASpriteHidesTheSquare()
        {
            SquarePool pool = SquarePool.Obtain(null, _owner.transform, "T", 0);
            pool.Fit(0, _twoUnits, Vector3.zero, 1f, Color.white);

            pool.Fit(0, null, Vector3.zero, 1f, Color.white);

            Assert.IsFalse(pool.Get(0).gameObject.activeSelf);
        }

        [Test]
        public void HideFrom_HidesOnlyTheSquaresAfterTheIndex()
        {
            SquarePool pool = SquarePool.Obtain(null, _owner.transform, "T", 0);
            for (int i = 0; i < 4; i++) pool.Stretch(i, _twoUnits, Vector3.zero, 1f, 1f, Color.white);

            pool.HideFrom(2);

            Assert.IsTrue(pool.Get(0).gameObject.activeSelf);
            Assert.IsTrue(pool.Get(1).gameObject.activeSelf);
            Assert.IsFalse(pool.Get(2).gameObject.activeSelf);
            Assert.IsFalse(pool.Get(3).gameObject.activeSelf);
        }

        [Test]
        public void Obtain_ReusesALivePool()
        {
            SquarePool first = SquarePool.Obtain(null, _owner.transform, "T", 0);

            SquarePool second = SquarePool.Obtain(first, _owner.transform, "T", 0);

            Assert.AreSame(first, second);
            Assert.AreEqual(1, _owner.transform.childCount);
        }

        [Test]
        public void Obtain_AdoptsTheContainerAndSquaresAlreadyInTheScene()
        {
            SquarePool first = SquarePool.Obtain(null, _owner.transform, "T", 0);
            first.Warm(3);
            SpriteRenderer second = first.Get(1);

            SquarePool again = SquarePool.Obtain(null, _owner.transform, "T", 0);

            Assert.AreEqual(3, again.Count);
            Assert.AreSame(second, again.Get(1));
            Assert.AreEqual(1, _owner.transform.childCount);
        }

        [Test]
        public void Warm_OnlyAddsWhatTheAdoptedPoolIsMissing()
        {
            SquarePool.Obtain(null, _owner.transform, "T", 0).Warm(3);

            SquarePool again = SquarePool.Obtain(null, _owner.transform, "T", 0);
            again.Warm(5);

            Assert.AreEqual(5, again.Count);
            Assert.AreEqual(5, _owner.transform.Find("Pool_T").childCount);
        }

        [Test]
        public void Obtain_NeverTouchesOtherChildren()
        {
            var decoration = new GameObject("Decoration");
            decoration.transform.SetParent(_owner.transform, false);

            SquarePool.Obtain(null, _owner.transform, "T", 0);
            SquarePool.Obtain(null, _owner.transform, "T", 0);

            Assert.IsNotNull(_owner.transform.Find("Decoration"));
        }
    }
}