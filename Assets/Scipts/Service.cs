public static class Service<T>
{ 
    private static T instance;
    public static T Instance => instance;
    public static void Set(T that) => instance = that;
}
