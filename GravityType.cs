namespace GravityHelper
{
    public enum GravityType
    {
        None = -1,
        Normal = 0,
        Inverted,
        Toggle,
    }

    public static class GravityTypeExtensions
    {
        public static GravityType Opposite(this GravityType type) => type switch
        {
            GravityType.Normal => GravityType.Inverted,
            GravityType.Inverted => GravityType.Normal,
            _ => type
        };
    }
}