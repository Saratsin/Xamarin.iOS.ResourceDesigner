using System.ComponentModel;

// NOTE This is a bugfix of C# compile, it doesn't affect anything,
// just enables the usage of init keyword
namespace System.Runtime.CompilerServices
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal class IsExternalInit { }
}