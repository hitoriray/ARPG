using System.Collections.Generic;
using JetBrains.Annotations;

namespace Framework
{
    /// <summary>
    /// MutableLiveData是一个LiveData的实现类，他持有一个T类型数据，并且可以获取/修改此数据的原始值。
    /// </summary>
    public class MutableLiveData<T> : LiveData<T>, IReadonlyLiveData<T>
    {
        [CanBeNull] private T _data;
        private readonly IEqualityComparer<T> _comparer;

        /// <summary>
        ///
        /// </summary>
        /// <param name="value">原始数据</param>
        /// <param name="comparer">自定义的相等性对比器</param>
        public MutableLiveData(T value = default, IEqualityComparer<T> comparer = null)
        {
            _data = value;
            _comparer = comparer ?? EqualityComparer<T>.Default;
        }

        public T Value
        {
            get => _data;
            set => SetData(value);
        }

        /// <summary>
        /// 设置新值，如果新值和旧值不相等，会通知所有观察者。
        /// </summary>
        /// <param name="value"></param>
        public override bool SetData(T value)
        {
            //没变化
            if (CompareValue(_data, value)) return false;
            _data = value;
            OnDataUpdate();
            return true;
        }

        /// <summary>
        /// 设置新值，不通知观察者。
        /// </summary>
        public void SetDataSilently(T value)
        {
            _data = value;
        }

        /// <summary>
        /// 对比两个值是否相等
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        private bool CompareValue(T lhs, T rhs)
        {
            return _comparer.Equals(lhs, rhs);
        }

        public static implicit operator T(MutableLiveData<T> param)
        {
            return param.Value;
        }
    }
}