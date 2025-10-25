using System;
using Newtonsoft.Json;

var obj = new { Name = "Test", Value = 42 };
var json = JsonConvert.SerializeObject(obj);
Console.WriteLine(json);
