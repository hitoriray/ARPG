using Arch.Core;
using Arch.Core.Extensions;
using Arch.Core.Utils;
namespace Arch.Extend.Matcher
{
    public sealed class EntityMatcher
    {
        private readonly QueryDescription? _filter;
        private readonly BitSet _any;
        private readonly BitSet _all;
        private readonly BitSet _none;
        private readonly BitSet _exclusive;
        private readonly bool _isExclusive;

        public EntityMatcher(QueryDescription? filter)
        {
            _filter = filter;
            if (_filter != null)
            {
                _all = _filter.Value.All;
                _any = _filter.Value.Any;
                _none = _filter.Value.None;
                _exclusive = _filter.Value.Exclusive;
                if (_filter.Value.Exclusive.Count != 0)
                    _isExclusive = true;
            }
        }

        public bool Matches(in Entity entity)
        {
            if (_filter == null) return true;
            var bitset = entity.GetArchetype().BitSet;
            return _isExclusive ? _exclusive.Exclusive(bitset) : _all.All(bitset) && _any.Any(bitset) && _none.None(bitset);
        }
    }
}
