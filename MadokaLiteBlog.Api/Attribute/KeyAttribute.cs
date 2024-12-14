[AttributeUsage(AttributeTargets.Property)]
public class KeyAttribute : Attribute
{
    public string Name { get; }
    public KeyAttribute()
    {
        var stackFrame = new System.Diagnostics.StackFrame(2); // 第二层调用堆栈
        var property = stackFrame?.GetMethod()?.DeclaringType?.GetProperties().FirstOrDefault();

        if (property != null)
        {
            Name = property.Name;
        }
        else
        {
            throw new ArgumentNullException(nameof(Name), "Property name cannot be null.");
        }
    }
    public KeyAttribute(string name) => Name = name;
}
