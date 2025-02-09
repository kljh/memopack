using MemoPack;
using MessagePack;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.CommandLine;
using System.IO;

namespace MemoCLI
{

    internal class Program
    {
        static async Task<int> Main(string[] args)
        {
            var rootCmd = new RootCommand("Memo CLI");

            var inputArg = new Argument<string>
                ("input", "Input file (json, memopack)");
            rootCmd.AddArgument(inputArg);
            var outputArg = new Argument<string>
                ("output", "Output file (json, memopack)");
            // rootCmd.AddArgument(outputArg);

            var rwOpt = new Option<string[]>(
                name: "--read",
                description: "Read a value in the input file (produces memory stats)");
            rwOpt.AddAlias("-r");
            rootCmd.AddOption(rwOpt);

            var outOpt = new Option<string>(
                name: "--out",
                description: "Output file (json, memopack). Explicit path.");
            outOpt.AddAlias("-o");
            rootCmd.AddOption(outOpt);

            var fmtOpt = new Option<string>(
                name: "--fmt",
                description: "Output format (json, memopack). Implicit output path is same as input with new extension.");
            fmtOpt.FromAmong("json", "mempack", "msgpack");
            fmtOpt.AddAlias("-f");
            // fmtOpt.Arity = ArgumentArity.OneOrMore;
            rootCmd.AddOption(fmtOpt);

            var extraPackOpt = new Option<bool>(
                name: "--extra-pack",
                description: "Output : allow object internalisation");
            extraPackOpt.AddAlias("-x");
            rootCmd.AddOption(extraPackOpt);

            var testOpt = new Option<bool>(
                name: "--test",
                description: "Run self test (bijective conversion)");
            testOpt.AddAlias("-t");
            rootCmd.AddOption(testOpt);

            rootCmd.SetHandler((inputPath, rw, outputPath, outputFormat, extra_pack, test) => {
                Console.WriteLine("Hello memopack!");

                object data = null;
                if (!string.IsNullOrEmpty(inputPath) && inputPath != "-") {
                    if (inputPath.StartsWith("~"))
                        inputPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
                            + inputPath.Substring(1);

                    Console.WriteLine($"Reading input file: {inputPath} ...");
                    data = FromFile(inputPath);
                }

                // output path from input path and target extension
                if (string.IsNullOrEmpty(outputPath)
                    && !string.IsNullOrEmpty(outputFormat)
                    && !string.IsNullOrEmpty(inputPath))
                    outputPath = inputPath.Substring(0, inputPath.Length - Path.GetExtension(inputPath).Length) + "." + outputFormat;

                if (!string.IsNullOrEmpty(outputPath) && data != null)
                {
                    Console.WriteLine($"Writing output file: {outputPath} ...");
                    ToFile(outputPath, data, extra_pack);
                }

                if (test)
                {
                    Console.WriteLine($"Running self-test ...");
                    if (data != null)
                        CheckMemo(data, extra_pack);
                    else
                        Test();
                }

                Console.WriteLine($"Done.\n");
            }, inputArg, rwOpt, outOpt, fmtOpt, extraPackOpt, testOpt);

            var retVal = await rootCmd.InvokeAsync(args);
            return retVal;
        }

        const bool extra_pack_default = false;  // string internalisation only

        static void Test()
        {
            TestScan();

            TestPackBaseTypes();
            TestPackFullJson(extra_pack: false);
            TestPackFullJson(extra_pack: true);
        }

        static void TestScan()
        {
            string type_name_field = "type";
            var dataJson = " { \"type\": \"abc\", \"int\": 123, \"txt\": 4.56, \"flag0\": false, \"flag1\": true, \"lst\": [ 1, true, \"love\" ]} ";
            object data = FromJson(dataJson);
            var scanner = new MemoScan(type_name_field);
            scanner.RunScan(data);
            scanner.DisplayScan();

        }

        static void TestPackBaseTypes()
        {
            CheckMemo(123);
            CheckMemo(4.56);
            CheckMemo("abcd");
            CheckMemo(new string[] { "abc", "def", "xyz" });
            CheckMemo(new double[] { 1.23, 4.56 });
            CheckMemo(new int[] { 123, 456, 789 });
            CheckMemo(new bool[] { true, true, false });
            CheckMemo(new bool[] { true, true, false, true, true, false, true, true, false, true, true, false, true, true, false, true, true, false, true, true, false, true, true, false,  });

            CheckMemoOnJson("123");
            CheckMemoOnJson("4.56");
            CheckMemoOnJson(" \"abc\" ");
            CheckMemoOnJson(" [ 123, 456 ] ");
            CheckMemoOnJson(" [ 12.3, 4.56 ] ");
            CheckMemoOnJson(" [ 123, 4.56 ] ");

            {
            string json0 = " [ 1, 2, 3, 567890123 ] ";
            string json1 = " [ 1, 2, 3, true ] ";
            CheckMemoCompress(json0, json1, extra_pack: false);
            }

            {
            string json0 = " [ 1.1, 2.2, 3.3, 567890123.4 ] ";
            string json1 = " [ 1.1, 2.2, 3.3, true ] ";
            CheckMemoCompress(json0, json1, extra_pack: false);
            }

            {
            // array of int and double => homogeneous array of double
            string json0 = " [ 1.1, 2, 3456789.0123 ] ";
            string json1 = " [ 1.1, 2, true ] ";
            CheckMemoCompress(json0, json1, extra_pack: false);
            }

            {
            string json0 = " [ \"a\", \"b\", \"c\", \"d\", \"efghijk\" ] ";
            string json1 = " [ \"a\", \"b\", \"c\", \"d\", true ] ";
            CheckMemoCompress(json0, json1, extra_pack: false);
            }

            CheckMemoOnJson(" { \"txt\": \"abc\", \"int\": 123, \"txt\": 4.56, \"flag0\": false, \"flag1\": true, \"lst\": [ 1, true, \"love\" ]} ");
        }

        static void TestPackFullJson(bool extra_pack)
        {

            {
            string json0 = " { \"abc\": [ \"abc\", \"abc\" ] } ";
            string json1 = " { \"txt\": [ \"abc\", \"abc\" ] } ";
            string json2 = " { \"txt\": [ \"abc\", \"def\" ] } ";
            CheckMemoCompress(json0, json1, extra_pack);
            CheckMemoCompress(json1, json2, extra_pack);
            }

            {
            extra_pack = true;  // allow_object_internalisation
            string jsonWithClone0 = " { \"item\": [ 0, \"abc\" ], \"again\": [ 0, \"abc\" ] } ";
            string jsonWithClone1 = " { \"item\": [ 0, \"abc\" ], \"again\": [ 1, \"abc\" ] } ";
            CheckMemoCompress(jsonWithClone0, jsonWithClone1, extra_pack);
            }
        }

        static void CheckMemo<T>(T val, bool extra_pack = extra_pack_default)
        {
            byte[] bytes = ToMemo(val, extra_pack);
            T res = FromMemo<T>(bytes);
            if (!val.Equals(res))
                throw new Exception($"CheckMemo diff {res} <> {val} ");
        }

        static int CheckMemoOnJson(string dataJson, bool extra_pack = extra_pack_default)
        {
            object data = FromJson(dataJson);
            string refJson = ToJson(data);

            byte[] bytes = ToMemo(data, extra_pack);
            object res = FromMemo<object>(bytes);
            string resJson = ToJson(res);

            if (!refJson.Equals(resJson))
                throw new Exception($"CheckMemo diff {resJson} <> {refJson} ");

            return bytes.Count();
        }

        static void CheckMemoCompress(string smallJson, string bigJson, bool extra_pack)
        {
            int resSize = CheckMemoOnJson(smallJson, extra_pack);
            int refSize = CheckMemoOnJson(bigJson, extra_pack);

            if (!(resSize < refSize))
                throw new Exception($"CheckMemo compress {resSize} !< {refSize} ");
        }

        static byte[] ToMemo(object val, bool allow_object_internalisation)
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter bw = new BinaryWriter(ms))
            using (MemoWriter mw = new MemoWriter(bw, allow_object_internalisation))
            {
                mw.WriteTagged(val);
                return ms.ToArray();
            }
        }

        static void ToMemoFile(string outputPath, object val, bool allow_object_internalisation)
        {
            using (FileStream fs = new FileStream(outputPath, FileMode.Create))
            using (BinaryWriter bw = new BinaryWriter(fs))
            using (MemoWriter mw = new MemoWriter(bw, allow_object_internalisation))
            {
                mw.WriteTagged(val);
            }
        }

        static T FromMemo<T>(byte[] bytes)
        {
            using (MemoryStream ms = new MemoryStream(bytes))
            using (BinaryReader br = new BinaryReader(ms))
            using (MemoReader mr = new MemoReader(br))
            {
                var res = mr.ReadTagged();
                return (T)res;
            }
        }

        static T FromMemoFile<T>(string inputPath)
        {
            using (FileStream fs = new FileStream(inputPath, FileMode.Open))
            using (BinaryReader br = new BinaryReader(fs))
            using (MemoReader mr = new MemoReader(br))
            {
                var res = mr.ReadTagged();
                return (T)res;
            }
        }

        static string ToJson(object val)
        {
            // JsonSerializer serializer = new JsonSerializer();
            // serializer.Formatting = Formatting.Indented;

            // using (MemoryStream ms = new MemoryStream())
            // using (TextWriter tw = new StreamWriter(ms))
            // using (JsonWriter jw = new JsonTextWriter(tw))
            // {
            //    serializer.Serialize(jw, val);
            //    return ms.ToString();
            //    // return ms.ToArray();  // byte[]
            //}

            return JsonConvert.SerializeObject(val);
        }

        static void ToJsonFile(string jsonPath, object val)
        {
            JsonSerializer serializer = new JsonSerializer();
            serializer.Formatting = Formatting.Indented;

            using (StreamWriter sw = new StreamWriter(jsonPath))
            using (JsonWriter jw = new JsonTextWriter(sw))
            {
                serializer.Serialize(jw, val);
            }
        }

        // Dictionary<string, object>
        static object FromJson(string json)
        {
            return JsonConvert.DeserializeObject<object>(json);
        }

        static object FromJsonFile(string jsonPath)
        {
            JsonSerializer serializer = new JsonSerializer();
            using (StreamReader sr = File.OpenText(jsonPath))
            using (JsonReader jr = new JsonTextReader(sr))
            {
                return serializer.Deserialize<object>(jr);
            }
        }


        static object FromMsgPackFile(string inputPath)
        {
            using (FileStream fs = new FileStream(inputPath, FileMode.Open))
            using (BinaryReader br = new BinaryReader(fs))
            {
                // byte[] bytes = br.ReadBytes(fs.Length);
                // return MessagePackSerializer.Deserialize<object>(bytes);

                return MessagePackSerializer.Deserialize<object>(fs);
            }
        }

        static void ToMsgPackFile(string outputPath, object data)
        {
            using (FileStream fs = new FileStream(outputPath, FileMode.Create))
            {
                MessagePackSerializer.Serialize(fs, data);
            }
        }

        static object FromFile(string inputPath)
        {
            var ext = Path.GetExtension(inputPath).ToLower();
            if (ext == ".json")
                return FromJsonFile(inputPath);
            else if (ext == ".mempack")
                return FromMemoFile<object>(inputPath);
            else if (ext == ".msgpack")
                return FromMsgPackFile(inputPath);
            else
                throw new NotSupportedException($"Unhandled exception {ext}");
        }

        static void ToFile(string outputPath, object data, bool extra_pack)
        {
            var ext = Path.GetExtension(outputPath).ToLower();
            if (ext == ".json")
                ToJsonFile(outputPath, data);
            else if (ext == ".mempack")
                ToMemoFile(outputPath, data, extra_pack);
            else if (ext == ".msgpack")
                ToMsgPackFile(outputPath, data);
             else
                throw new NotSupportedException($"Unhandled exception {ext}");
        }
    }
}
