using System;

namespace OlympUI {
    public static class Extensions {

        public static string? NotEmpty(this string? s)
            => string.IsNullOrEmpty(s) ? null : s;

    }

    public class CompilerSatisfactionException : Exception {

        public CompilerSatisfactionException()
            : base("This member merely exists to satisfy the compiler. Sorry!") {
        }

    }

    public interface IGenericValueSource {

        T GetValue<T>();

    }
}
