using System.Collections.Generic;
using System.Linq;
using MahjongCourseWork.Models;

namespace MahjongCourseWork.Services
{
    public class MoveValidator
    {
        public bool IsTileFree(TileInstance tile, List<TileInstance> tiles)
        {
            if (tile.IsRemoved)
                return false;

            bool hasLeftNeighbor = tiles.Any(t =>
                !t.IsRemoved &&
                t.Id != tile.Id &&
                t.Layer == tile.Layer &&
                t.Row == tile.Row &&
                t.Column == tile.Column - 1);

            bool hasRightNeighbor = tiles.Any(t =>
                !t.IsRemoved &&
                t.Id != tile.Id &&
                t.Layer == tile.Layer &&
                t.Row == tile.Row &&
                t.Column == tile.Column + 1);

            return !hasLeftNeighbor || !hasRightNeighbor;
        }

        public bool CanMatch(TileInstance first, TileInstance second, List<TileInstance> tiles)
        {
            if (first.Id == second.Id)
                return false;

            if (first.IsRemoved || second.IsRemoved)
                return false;

            if (first.Kind != second.Kind)
                return false;

            return IsTileFree(first, tiles) && IsTileFree(second, tiles);
        }
    }
}