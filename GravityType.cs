namespace GravityHelper
{
    public enum GravityType
    {
        Normal,
        Inverted,
        Toggle,
    }

    public static class GravityTypeExtensions
    {
        public static GravityType Opposite(this GravityType type) => type == GravityType.Normal ? GravityType.Inverted : GravityType.Normal;
    }
}