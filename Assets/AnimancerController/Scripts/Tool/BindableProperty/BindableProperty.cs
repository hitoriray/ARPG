using System;

public class BindableProperty<T>
{
    public Action<T> ValueChanged;
    private T m_value;

    public T Value
    {
        set
        {
            if (!Equals(m_value, value))
            {
                m_value = value;
                ValueChanged?.Invoke(m_value);
            }
        }
        get => m_value;
    }
}