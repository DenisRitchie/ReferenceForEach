using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

using static System.Console;

namespace ForeachRefReadonly
{
    public unsafe struct ReferenceWrapper<TValue>
    {
        private readonly void* Pointer;

        public ReferenceWrapper(ref TValue Value)
        {
            Pointer = Unsafe.AsPointer(ref Value);
        }

        public ref TValue Value => ref Unsafe.AsRef<TValue>(Pointer);
    }

    public readonly ref struct ReferenceList<TValue>
    {
        private readonly TValue[] Items;
        public readonly int Length;

        public ReferenceList(List<TValue> List)
        {
            FieldInfo Field = List.GetType().GetField("_items", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetField);
            Length = List.Count;
            Items = Field.GetValue(List) as TValue[];
        }

        public readonly ref TValue this[int Index] => ref Items[Index];

        public readonly Enumerator GetEnumerator() => new Enumerator(in Items, in Length);

        public ref struct Enumerator
        {
            private TValue[] Items;
            private int Index;
            private int Count;

            public Enumerator(in TValue[] Items, in int Count)
            {
                this.Index = 0;
                this.Items = Items;
                this.Count = Count;
            }

            public ref TValue Current => ref Items[Index++];

            public bool MoveNext() => Index < Count;

            public void Reset() => Index = 0;
        }
    }

    public unsafe readonly ref struct ReferenceDictionary<TKey, TValue>
    {
        private class DictionaryPrivateFields
        {
            private readonly int[] Padding1;

            public Entry[] Entries;
            public int Count;

            private readonly int Padding2;
            private readonly int Padding3;

            public int FreeCount;

            private readonly IEqualityComparer<TKey> Padding4;
            private readonly Dictionary<TKey, TValue>.KeyCollection Padding5;
            private readonly Dictionary<TKey, TValue>.ValueCollection Padding6;
            private readonly Object Padding7;
        }

        private struct Entry
        {
            public int HashCode;
            private readonly int Padding;
            public TKey Key;
            public TValue Value;
        }

        private readonly Func<TKey, int> FindEntry;
        private readonly Dictionary<TKey, TValue> InternalMap;

        public readonly int Count => InternalMap.Count;

        public ReferenceDictionary(Dictionary<TKey, TValue> Map)
        {
            this.InternalMap = Map;
            MethodInfo FindEntryInfo = Map.GetType().GetMethod("FindEntry", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.InvokeMethod);
            FindEntry = FindEntryInfo.CreateDelegate(typeof(Func<TKey, int>), Map) as Func<TKey, int>;
        }

        public readonly ref TValue this[in TKey Key]
        {
            get
            {
                int Index = FindEntry(Key);

                if (Index >= 0)
                {
                    return ref Unsafe.As<DictionaryPrivateFields>(InternalMap).Entries[Index].Value;
                }

                throw new ArgumentException(nameof(Key));
            }
        }

        public readonly Enumerator GetEnumerator()
        {
            Enumerator Enumerator = default;
            byte* Pointer = (byte*)Enumerator.GetAddress(ref Enumerator);

            ref Entry[] Entries = ref Unsafe.AsRef<Entry[]>(Pointer);
            ref uint Count = ref Unsafe.AsRef<uint>(Pointer + Unsafe.SizeOf<Entry[]>());

            DictionaryPrivateFields Fields = Unsafe.As<DictionaryPrivateFields>(InternalMap);
            Entries = Fields.Entries;
            Count = (uint)Fields.Count;

            return Enumerator;
        }

        public readonly struct KeyValue
        {
            private readonly ReferenceWrapper<TKey> ReferenceKey;
            private readonly ReferenceWrapper<TValue> ReferenceValue;

            public KeyValue(ref TKey Key, ref TValue Value)
            {
                ReferenceKey = new ReferenceWrapper<TKey>(ref Key);
                ReferenceValue = new ReferenceWrapper<TValue>(ref Value);
            }

            public readonly ref readonly TKey Key => ref ReferenceKey.Value;

            public readonly ref TValue Value => ref ReferenceValue.Value;
        }

        public ref struct Enumerator
        {
            private Entry[] Entries;
            private readonly uint Count;
            private KeyValue CurrentValue;
            private KeyValue* PointerCurrentValue;
            private uint Index;

            public static void* GetAddress(ref Enumerator Enumerator) => Unsafe.AsPointer(ref Enumerator.Entries);

            public readonly ref KeyValue Current => ref *PointerCurrentValue;

            public bool MoveNext()
            {
                while (Index < Count)
                {
                    if (Entries[Index].HashCode >= 0)
                    {
                        ref Entry Entry = ref Entries[Index++];
                        CurrentValue = new KeyValue(ref Entry.Key, ref Entry.Value);
                        PointerCurrentValue = (KeyValue*)Unsafe.AsPointer(ref CurrentValue);
                        return true;
                    }

                    Index++;
                }

                Index = Count + 1;
                return false;
            }

            public void Reset() => Index = 0;
        }
    }

    public static class CollectionExtensions
    {
        public static ReferenceList<T> ToReference<T>(this List<T> Source)
            => new ReferenceList<T>(Source);

        public static ReferenceDictionary<TKey, TValue> ToReference<TKey, TValue>(this Dictionary<TKey, TValue> Source)
            => new ReferenceDictionary<TKey, TValue>(Source);
    }

    public class Program
    {
        private static void TestReferenceListInForEach()
        {
            List<int> Values = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

            foreach (ref var Value in Values.ToReference())
                Value *= Values.Count;

            int Index = 0;
            foreach (ref readonly var Value in Values.ToReference())
                WriteLine($"[{++Index}]: {Value}");

            WriteLine();
        }

        private class Data
        {
            public Data(int Age, double Salary)
            {
                this.Age = Age;
                this.Salary = Salary;
            }

            public int Age;
            public double Salary;

            public static implicit operator Data((int Age, double Salary) Value)
                => new Data(Value.Age, Value.Salary);

            public override string ToString()
            {
                return $"Edad: {Age}, Salario: {Salary}";
            }
        }

        private static void TestReferenceDictionaryInForEach()
        {
            Dictionary<string, Data> Persons = new Dictionary<string, Data>
            {
                ["Denis"] = (30, 30_000),
                ["Cris"] = (40, 50_000),
                ["David"] = (1, 1_000)
            };

            ReferenceDictionary<string, Data> ReferencePersons = Persons.ToReference();
            PrintMap("Datos Iniciales", in ReferencePersons);

            foreach (ref var Data in ReferencePersons)
                Data.Value.Salary += 450_000;

            ReferencePersons["David"] = (150, int.MaxValue);

            PrintMap("\nMap después de la modificación", in ReferencePersons);

            Persons["Nubia"] = (50, 1_000_000);
            Persons["Diana"] = (60, 6_000_000);
            Persons["Orlando"] = (90, 8_000_000);

            PrintMap("\nMap con nuevos datos", in ReferencePersons);

            static void PrintMap(string Message, in ReferenceDictionary<string, Data> Data)
            {
                WriteLine(Message);

                foreach (ref readonly var Value in Data)
                {
                    WriteLine($"Name: {Value.Key} / Data: {Value.Value}");
                }
            }
        }

        public static void Main(string[] _)
        {
            // https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/struct
            // http://benbowen.blog/post/fun_with_makeref/

            TestReferenceListInForEach();
            TestReferenceDictionaryInForEach();

            ReadKey(true);
        }
    }
}
