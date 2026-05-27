// Polyfill for C# 9+ init accessors when the runtime does not ship IsExternalInit.
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit
    {
    }
}
