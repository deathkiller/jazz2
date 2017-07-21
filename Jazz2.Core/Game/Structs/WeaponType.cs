namespace Jazz2.Game.Structs
{
    public enum WeaponType : ushort
    {
        Blaster = 0,
        Bouncer = 1,
        Freezer = 2,
        Seeker = 3,
        RF = 4,
        Toaster = 5,
        TNT = 6,
        Pepper = 7,
        Electro = 8,

        Count,
        Unknown = ushort.MaxValue
    }
}