using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

namespace MemoPack
{

public class MemoScan : IDisposable
{
    private readonly string type_field_name;
    private int object_count = 0;
    private HashSet<string>? pointer_tags; // YAML allows pointer with document

    public MemoScan(string type_field_name, HashSet<string>? pointer_tags = null)
    {
        this.type_field_name = type_field_name;
        this.pointer_tags = pointer_tags;
    }

    public void Dispose()
    {}

    public Dictionary<string, TypeStats> Types = new();

    public class TypeStats
    {
        public int UseCount;
        public Dictionary<string, AttributeStats> Attributes = new();
    }

    public class AttributeStats
    {
        public int UseCount;

        // Hopefully, that's a singleton
        public Dictionary<string, int> UsedTypes = new();

        // For string, arrays, and dictionary, we report the max size
        public double MaxValue;

        // Are string used as enum ?
        public Dictionary<string, int> UsedValues = new();

        // YAML allow pointer to other objects in the document
        public bool IsPtr = false;
    }


    public void RunScan(object data)
    {
        if (data is JObject)
            RunScanJObject((JObject)data);

        if (data is JArray)
            RunScanJArray((JArray)data);


        /*
        if (val is string)
            return RunScan((string)val);
        if (val is double)
            return RunScan((double)val);
        if (val is long)
            return RunScan((long)val);
        if (val is int)
            return RunScan((int)val);
        if (val is bool)
            return RunScan((bool)val);
        */
    }

    public void RunScan(object[] vec, AttributeStats stats)
    {

    }

    public void RunScanJObject(JObject jsonObj)
    {
        object_count++;

        TypeStats? typeStats = GetTypeStats(jsonObj);
        if (typeStats != null)
            typeStats.UseCount++;

        var dict = jsonObj.ToObject<Dictionary<string, object>>();
        if (dict == null)
            throw new ArgumentNullException();
        foreach (var kv in dict)
        {
            string att = kv.Key;
            object val = kv.Value;
            RunScan(val);

            if (typeStats != null)
            {
                if (!typeStats.Attributes.ContainsKey(att))
                    typeStats.Attributes[att] = new();
                var attrStats = typeStats.Attributes[att];

                UpdateAttributeStats(val, attrStats);
            }
        }
    }

    private void RunScanJArray(JArray jsonArray)
    {
        object[]? vec = jsonArray.ToObject<object[]>();
        if (vec == null)
            throw new ArgumentNullException();

        foreach (var val in vec)
            RunScan(val);
    }

    private TypeStats? GetTypeStats(JObject jsonObj)
    {
        if (!jsonObj.ContainsKey(type_field_name))
            return null;

        string? type_name = jsonObj[type_field_name]?.ToString();

        if (type_name == null)
            return null;

        if (!Types.ContainsKey(type_name))
            Types[type_name] = new ();

        return Types[type_name];
    }

    public static void UpdateAttributeStats(IEnumerable<object> vec, AttributeStats attrStats)
    {
        foreach (var val in vec)
        {
            var valType = val.GetType().Name;
            UpdateAttributeStats(val, attrStats);
        }
    }

   static readonly HashSet<string> IntegerTypes = new() {
        "Int64", "Int32", "Int16", "Int8",
        "UInt64", "UInt32", "UInt16", "UInt8" };

   static readonly HashSet<string> NumericTypes = new() {
        "Double" };

    public static void UpdateAttributeStats(object val, AttributeStats attrStats)
    {
        attrStats.UseCount++;

        var valType = val.GetType().Name;
        if (IntegerTypes.Contains(valType))
            valType = "int";
        else if (NumericTypes.Contains(valType))
            valType = "double";
        else if (valType == "String")
            valType = "string";
        else if (valType == "Boolean")
            valType = "bool";

        if (!attrStats.UsedTypes.ContainsKey(valType))
            attrStats.UsedTypes[valType] = 0;
        attrStats.UsedTypes[valType]++;

        attrStats.MaxValue = Math.Max(
            attrStats.MaxValue,
            GetNumValue(val));

        if (val is string)
        {
            var valStr = (string)val;

            if (!attrStats.UsedValues.ContainsKey(valStr))
                attrStats.UsedValues[valStr] = 0;
            attrStats.UsedValues[valStr]++;

            // attrStats.IsPtr = attrStats.IsPtr ||
            //     ( pointer_tags?.Contains(valStr) ?? false );
        }
    }

    static private double GetNumValue(object val)
    {
        if (val is string)
            return ((string)val).Length;
        if (val is double)
            return (double)val;
        if (val is long)
            return (long)val;
        if (val is int)
            return (int)val;
        if (val is ulong)
            return (ulong)val;
        if (val is uint)
            return (uint)val;
        return 0.0;
    }

    public void DisplayScan()
    {
        foreach (var kvt in this.Types)
        {
            string typeName = kvt.Key;
            TypeStats typeStats = kvt.Value;

            Console.WriteLine($"type =\t{typeName}\t #{typeStats.UseCount}");

            foreach (var kva in typeStats.Attributes)
            {
                string attrName = kva.Key;
                AttributeStats attrStats = kva.Value;

                string types = string.Join(",", attrStats.UsedTypes
                    .OrderBy(kv => -kv.Value)
                    .Select(kv => kv.Key));

                string usedValue = $"#{attrStats.UsedValues.Count()}" +
                    string.Join(",", attrStats.UsedTypes
                        .OrderBy(kv => -kv.Value)
                        .Take(12)
                        .Select(kv => kv.Key));

                Console.WriteLine($"\t\t{attrName}\t types={types}\t #{attrStats.UseCount}\t MaxVal={attrStats.MaxValue}");
            }
        }
    }
}

}