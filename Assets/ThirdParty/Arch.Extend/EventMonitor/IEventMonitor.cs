using System;
using Arch.Core;
namespace Arch.Extend.EventMonitor
{
    public interface IEventMonitor
    {
        public event Action<Entity> OnEvent;
    }
}
