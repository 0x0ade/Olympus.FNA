using System;

namespace OlympUI {
    public class RotatingPool<T> {

        public readonly T[] Array;
        public readonly Func<T> Generator;
        public int Index = -1;
        public int Allocated;

        public RotatingPool(int size, Func<T> generator) {
            Array = new T[size];
            Generator = generator;
        }

        public T Next() {
            Index = (Index + 1) % Array.Length;
            if (Index < Allocated)
                return Array[Index];
            Allocated = Index + 1;
            return Array[Index] = Generator();
        }

    }

    public class AutoRotatingPool<T> where T : new() {

        public readonly T[] Array;
        public int Index = -1;
        public int Allocated;

        public AutoRotatingPool(int size) {
            Array = new T[size];
        }

        public T Next() {
            Index = (Index + 1) % Array.Length;
            if (Index < Allocated)
                return Array[Index];
            Allocated = Index + 1;
            return Array[Index] = new T();
        }

    }
}
