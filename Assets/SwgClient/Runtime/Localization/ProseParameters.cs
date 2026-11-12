namespace Swg.Client.Runtime.Localization
{
    // Resolved names and numeric values used by an SWG prose template.
    public readonly struct ProseParameters
    {
        public readonly string User;
        public readonly string Target;
        public readonly string Object;
        public readonly int Integer;
        public readonly float Float;

        public ProseParameters(string user, string target, string objectText, int integer = 0, float floatValue = 0)
        {
            User = user;
            Target = target;
            Object = objectText;
            Integer = integer;
            Float = floatValue;
        }
    }
}
