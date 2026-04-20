using System.Collections.Generic;

namespace MahjongCourseWork.Core
{
    public static class LayoutFactory
    {
        public const string Classic = "Класична";
        public const string Fortress = "Фортеця";

        public static List<(int Row, int Column)> GetLayoutPositions(string layoutName)
        {
            if (layoutName == Fortress)
            {
                return GetFortressLayout();
            }

            return GetClassicLayout();
        }

        private static List<(int Row, int Column)> GetClassicLayout()
        {
            return new List<(int Row, int Column)>
            {
                // Row 0
                (0, 2), (0, 3), (0, 4), (0, 5),

                // Row 1
                (1, 1), (1, 2), (1, 3), (1, 4), (1, 5), (1, 6),

                // Row 2
                (2, 0), (2, 1), (2, 2), (2, 3), (2, 4), (2, 5), (2, 6), (2, 7),

                // Row 3
                (3, 1), (3, 2), (3, 3), (3, 4), (3, 5), (3, 6)
            };
        }

        private static List<(int Row, int Column)> GetFortressLayout()
        {
            return new List<(int Row, int Column)>
            {
                // Row 0
                (0, 1), (0, 2), (0, 3), (0, 4), (0, 5), (0, 6),

                // Row 1
                (1, 0), (1, 1), (1, 6), (1, 7),

                // Row 2
                (2, 0), (2, 1), (2, 6), (2, 7),

                // Row 3
                (3, 0), (3, 1), (3, 6), (3, 7),

                // Row 4
                (4, 1), (4, 2), (4, 3), (4, 4), (4, 5), (4, 6)
            };
        }
    }
}
