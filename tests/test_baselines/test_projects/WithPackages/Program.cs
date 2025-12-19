using Newtonsoft.Json;

var obj = new { Name = "Test", Value = 42 };
Console.WriteLine(JsonConvert.SerializeObject(obj));
