using System;
using System.Collections.Generic;
using System.Text;

namespace MahjongCourseWork.Models
{
    public class TileInstance
    {
        public int Id { get; set; }
        public TileKind Kind { get; set; }

        public int Row { get; set; }
        public int Column { get; set; }
        public int Layer { get; set; }

        public bool IsRemoved { get; set; }
        public bool IsSelected { get; set; }

        public string DisplayName => Kind.ToString();
    }
}
