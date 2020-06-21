# ReferenceForEach
Ejemplo Implementación del foreach por referencia, recreando la misma funcionalidad de for(const type &amp; : values) de C++

# Ejemplo
```
using System;
using System.Collections.Generic;

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
            ["Cris"]  = (40, 50_000),
            ["David"] = ( 1,  1_000)
        };

        ReferenceDictionary<string, Data> ReferencePersons = Persons.ToReference();
        PrintMap("Datos Iniciales", in ReferencePersons);

        foreach (ref var Data in ReferencePersons)
            Data.Value.Salary += 450_000;

        ReferencePersons["David"] = (150, int.MaxValue);

        PrintMap("\nMap después de la modificación", in ReferencePersons);

        Persons["Nubia"]   = (50, 1_000_000);
        Persons["Diana"]   = (60, 6_000_000);
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
}
 
```

