namespace Afterbunny.Windows.Helpers
{
    public static class MiscHelper
    {
        public static T CopyObject<T>(T o) where T : class, new()
        {
            var copy = new T();

            var type = copy.GetType();
            var props = type.GetProperties();

            foreach (var prop in props)
            {
                if (prop.CanWrite)
                    prop.SetValue(copy, prop.GetValue(o));
            }

            return copy;

        }
    }
}