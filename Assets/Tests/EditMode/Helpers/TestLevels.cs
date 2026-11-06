using SlideJam.Engine;

namespace SlideJam.Tests.EditMode
{
    internal static class TestLevels
    {
        public const int Colors = 5;

        public static Target[] Targets(params int[] colors)
        {
            return TargetsWithCap(3, colors);
        }

        public static Target[] TargetsWithCap(int cap, params int[] colors)
        {
            var targets = new Target[colors.Length];
            for (int i = 0; i < colors.Length; i++) targets[i] = new Target(colors[i], cap);
            return targets;
        }

        public static LevelData Make(int rows, int cols, uint seed, int activeSlots, int queueMax,
            int qOpenStart, bool qGrow, Target[] seq)
        {
            return new LevelData(rows, cols, Colors, seed, activeSlots, queueMax, qOpenStart, qGrow, seq);
        }
    }
}