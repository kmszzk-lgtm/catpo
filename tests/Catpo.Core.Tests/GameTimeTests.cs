using NUnit.Framework;

namespace Catpo.Core.Tests
{
    public class GameTimeTests
    {
        [TestCase(1, Season.Spring, 1)]
        [TestCase(30, Season.Spring, 1)]
        [TestCase(31, Season.Summer, 1)]
        [TestCase(61, Season.Autumn, 1)]
        [TestCase(91, Season.Winter, 1)]
        [TestCase(120, Season.Winter, 1)]
        [TestCase(121, Season.Spring, 2)]
        public void 계절은_봄부터_30일씩_바뀐다(int day, Season season, int year)
        {
            var t = new GameTime(day, 12);
            Assert.That(t.Season, Is.EqualTo(season));
            Assert.That(t.Year, Is.EqualTo(year));
        }

        [TestCase(1, 8, 59, false)]
        [TestCase(1, 9, 0, true)]
        [TestCase(1, 17, 59, true)]
        [TestCase(1, 18, 0, false)]
        [TestCase(31, 8, 0, true)]
        [TestCase(31, 19, 59, true)]
        [TestCase(31, 20, 0, false)]
        [TestCase(91, 9, 59, false)]
        [TestCase(91, 10, 0, true)]
        [TestCase(91, 16, 0, false)]
        public void 외출_가능_시간은_계절마다_다르다(int day, int hour, int minute, bool expected)
        {
            Assert.That(new GameTime(day, hour, minute).IsWithinOutingWindow, Is.EqualTo(expected));
        }

        [Test]
        public void 좀비는_정오에_가장_느리고_자정에_가장_빠르다()
        {
            Assert.That(GameTime.ZombieSpeedAt(12 * 60), Is.EqualTo(Balance.ZombieSpeedAtNoon).Within(1e-9));
            Assert.That(GameTime.ZombieSpeedAt(0), Is.EqualTo(Balance.ZombieSpeedAtMidnight).Within(1e-9));
            Assert.That(GameTime.ZombieSpeedAt(6 * 60), Is.EqualTo(1.0).Within(1e-9));
            Assert.That(GameTime.ZombieSpeedAt(18 * 60), Is.EqualTo(1.0).Within(1e-9));
            Assert.That(GameTime.ZombieSpeedAt(21 * 60), Is.GreaterThan(GameTime.ZombieSpeedAt(18 * 60)));
        }

        [Test]
        public void 시간을_돌리면_지난_자정_수를_알려준다()
        {
            var t = new GameTime(1, 23);
            int midnights = t.Advance(120);
            Assert.That(midnights, Is.EqualTo(1));
            Assert.That(t.Day, Is.EqualTo(2));
            Assert.That(t.Hour, Is.EqualTo(1));
        }
    }
}
