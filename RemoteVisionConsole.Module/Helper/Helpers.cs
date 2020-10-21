using RemoteVisionConsole.Interface;
using System;

namespace RemoteVisionConsole.Module.Helper
{
    public static class Helpers
    {

        public static bool IsVisionProcessor<TData>(Type type)
        {
            return type.IsSubclassOf(typeof(IVisionProcessor<TData>));
        }


        public static bool IsVisionAdapter<TData>(Type type)
        {
            return type.IsSubclassOf(typeof(IVisionAdapter<TData>));
        }
    }
}
