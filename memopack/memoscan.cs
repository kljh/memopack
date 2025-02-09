using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

namespace MemoPack
{

public class MemoScan : IDisposable
{
    private readonly string type_field_name;
    private int object_count = 0;
    private HashSet<string> pointer_tags; // YAML allows pointer with document

    public MemoScan(string type_field_name, HashSet<string> pointer_tags = null)
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
            RunScanJObject(data as JObject);
        
        if (data is JArray)
            RunScanJArray(data as JArray);
        

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

    public void RunScanJObject(JObject jsonObj)
    {
        object_count++;
        
        TypeStats typeStats = GetTypeStats(jsonObj);
        typeStats.UseCount++;
        
        var dict = jsonObj.ToObject<Dictionary<string, object>>();        
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

                attrStats.UseCount++;

                var valType = val.GetType().Name;
                if (!attrStats.UsedTypes.ContainsKey(valType))
                    attrStats.UsedTypes[val.GetType().Name] = 0;
                attrStats.UsedTypes[val.GetType().Name]++;

                attrStats.MaxValue = Math.Max(
                    attrStats.MaxValue,
                    GetNumValue(val));

                if (val is string)
                {
                    var valStr = val as string;

                    if (!attrStats.UsedValues.ContainsKey(valStr))
                        attrStats.UsedValues[valStr] = 0;
                    attrStats.UsedValues[valStr]++;
                
                    attrStats.IsPtr = attrStats.IsPtr ||
                        ( pointer_tags?.Contains(valStr) ?? false ); 
                }
            }
        }
    }    

    private TypeStats GetTypeStats(JObject jsonObj)
    {
        if (!jsonObj.ContainsKey(type_field_name))
            return null;

        string type_name = jsonObj[type_field_name].ToString();

        if (!Types.ContainsKey(type_name))
            Types[type_name] = new ();
        
        return Types[type_name];
    }

    static private double GetNumValue(object val)
    {
        if (val is string)
            return (val as string).Length;
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
    
    private void RunScanJArray(JArray jsonArray)
    {
        object[] vec = jsonArray.ToObject<object[]>();
        foreach (var val in vec)
            RunScan(val);
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