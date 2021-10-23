namespace Fp.Collections
{
    //TODO: Want make this not only for unity.
    internal static class Assert
    {
        public static void IsNotNull(object value)
        {
#if UNITY_ASSERTIONS
            UnityEngine.Assertions.Assert.IsNotNull(value);
#endif
        }

        public static void IsTrue(bool condition)
        {
#if UNITY_ASSERTIONS
            UnityEngine.Assertions.Assert.IsTrue(condition);
#endif
        }

        public static void IsTrue(bool condition, string message)
        {
#if UNITY_ASSERTIONS
            UnityEngine.Assertions.Assert.IsTrue(condition, message);
#endif      
        }

        public static void IsFalse(bool condition, string message)
        {
#if UNITY_ASSERTIONS
            UnityEngine.Assertions.Assert.IsFalse(condition, message);
#endif      
        }

        public static void IsNotNull<T>(T value, string message)
            where T : class
        {
#if UNITY_ASSERTIONS
            UnityEngine.Assertions.Assert.IsNotNull<T>(value, message);
#endif
        }
    }
}