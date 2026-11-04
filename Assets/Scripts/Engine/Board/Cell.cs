namespace SlideJam.Engine
{
    public readonly struct Cell
    {
        public readonly int Row;
        public readonly int Col;

        public Cell(int row, int col)
        {
            Row = row;
            Col = col;
        }
    }
}
