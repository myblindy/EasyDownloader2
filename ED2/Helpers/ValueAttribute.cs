namespace ED2.Helpers;

[AttributeUsage(AttributeTargets.Field)]
public class ValueAttribute<T>(T value) : Attribute
{
    public T Value { get; } = value;
}
