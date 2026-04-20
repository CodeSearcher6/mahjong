using System.Collections.Generic;
using MahjongCourseWork.Models;

namespace MahjongCourseWork.Services
{
    public class HintService
    {
        private readonly MoveValidator _moveValidator = new();

        public (TileInstance? First, TileInstance? Second) FindAvailablePair(List<TileInstance> tiles)
        {
            for (int i = 0; i < tiles.Count; i++)
            {
                var first = tiles[i];

                if (first.IsRemoved || !_moveValidator.IsTileFree(first, tiles))
                    continue;

                for (int j = i + 1; j < tiles.Count; j++)
                {
                    var second = tiles[j];

                    if (_moveValidator.CanMatch(first, second, tiles))
                    {
                        return (first, second);
                    }
                }
            }

            return (null, null);
        }
    }
}