namespace SlideJam.Engine
{
    public enum PlaceKind
    {
        Grid,
        Slot,
        Queue,
        Vault,
        Vanish,
    }
    
    public readonly struct Place
    {
        public readonly PlaceKind Kind;
        public readonly int A;
        public readonly int B;

        private Place(PlaceKind kind, int a, int b)
        {
            Kind = kind;
            A = a;
            B = b;
        }

        public static Place InGrid(Cell cell) => new Place(PlaceKind.Grid, cell.Row, cell.Col);
        public static Place InSlot(int slot, int pip) => new Place(PlaceKind.Slot, slot, pip);
        public static Place InQueue(int index) => new Place(PlaceKind.Queue, index, 0);
        public static Place InVault(int index) => new Place(PlaceKind.Vault, index, 0);
        public static Place ToVanish => new Place(PlaceKind.Vanish, 0, 0);
    }
}
