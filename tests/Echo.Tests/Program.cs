using Echo.Contracts;
var valid = new[] { "one", "two", "three" }; if (!Validation.ValidSamples(valid) || Validation.ValidSamples(new[] { "one", "two" }) || Validation.WordCount(" a  b ") != 2) throw new Exception("Contract validation failed."); Console.WriteLine("Echo contract checks passed.");
