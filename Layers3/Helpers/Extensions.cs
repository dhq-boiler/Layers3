using System.Collections.ObjectModel;

namespace Layers3.Helpers
{
    public static class Extensions
    {
        public static void AddTo<T>(this IEnumerable<T> array, ObservableCollection<T> target)
        {
            array.ToList().ForEach(target.Add);
        }

        public static T CastStruct<T>(this object obj) where T : struct
        {
            return (T)obj;
        }

        public static bool CastIntegerToBool(this object obj)
        {
            return (bool)obj;
        }

        public static T? CastNullableStruct<T>(this object obj) where T : struct
        {
            return (T?)obj ?? null;
        }

        public static T CastClass<T>(this object obj) where T : class
        {
            return (T)obj;
        }

        public static T As<T>(this object obj) where T : class
        {
            return obj as T;
        }
    }
}
