using System.Diagnostics.CodeAnalysis;

// The entry point is defined by top-level statements in Program.cs; this partial declaration only
// attaches [ExcludeFromCodeCoverage] to the compiler-generated Program class. The composition root
// (host wiring and Spectre command tree) is exercised end-to-end, not by unit tests.
// S3903 is suppressed because the top-level Program class must live in the global namespace to
// match the entry point the compiler generates from Program.cs.
#pragma warning disable S3903
[ExcludeFromCodeCoverage]
internal partial class Program
{
    private Program() { }
}
#pragma warning restore S3903
