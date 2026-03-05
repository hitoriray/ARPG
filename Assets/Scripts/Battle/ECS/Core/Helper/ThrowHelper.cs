using System;

namespace Battle.ECS.Core.Helper
{
    public static class ThrowHelper
    {
        public static void Throw(string msg)
        {
            Throw(new Exception(msg));
        }

        public static void Throw(Exception exception)
        {
#if UNITY_EDITOR
            throw exception;
#else
            RayDebug.Exception(exception);
#endif
        }
    }
}
