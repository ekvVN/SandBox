namespace Common.Log.LogProviders
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    [ExcludeFromCodeCoverage]
    public static class TraceEventTypeValues
    {
        public static readonly Type Type;
        public static readonly int Verbose;
        public static readonly int Information;
        public static readonly int Warning;
        public static readonly int Error;
        public static readonly int Critical;

        [SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline")]
        static TraceEventTypeValues()
        {
            var assembly = typeof(Uri).GetAssemblyPortable(); // This is to get to the System.dll assembly in a PCL compatible way.
            if (assembly == null)
            {
                return;
            }
            Type = assembly.GetType("System.Diagnostics.TraceEventType");
            if (Type == null) return;
            Verbose = (int)Enum.Parse(Type, "Verbose", false);
            Information = (int)Enum.Parse(Type, "Information", false);
            Warning = (int)Enum.Parse(Type, "Warning", false);
            Error = (int)Enum.Parse(Type, "Error", false);
            Critical = (int)Enum.Parse(Type, "Critical", false);
        }
    }
}