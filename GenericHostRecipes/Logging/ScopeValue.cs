namespace GenericHostBuilderRecipes.Logging
{
    public class ScopeValue
    {
        public ScopeValue(string key, object value, bool destructure)
        {
            Key = key;
            Value = value;
            Destructure = destructure;
        }

        public string Key { get; private set; }
        public object Value { get; private set; }
        public bool Destructure { get; private set; }
    }
}