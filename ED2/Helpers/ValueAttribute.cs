namespace ED2.Helpers;

[AttributeUsage(AttributeTargets.Field)]
public class ValueAttribute<T> : Attribute
{
    public ValueAttribute(T value)
    {
        Value = value;
    }

    public T Value { get; }
}
