using DynamicExpresso.Exceptions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DynamicExpresso.UnitTest
{
	[TestFixture]
	public class LambdaExpressionTest
	{
		private const InterpreterOptions _options = InterpreterOptions.Default | InterpreterOptions.LambdaExpressions;

		[Test]
		public void Invalid_Lambda_Should_Produce_a_ParseException()
		{
			var target = new Interpreter(_options);
			var list = new List<string> { "abc", "dfe", "test" };
			target.SetVariable("list", list);

			Assert.Throws<ParseException>(() => target.Parse("list.Select(str => str.Legnth)") );
		}

		[Test]
		public void Check_Lambda_Return_Type()
		{
			var target = new Interpreter(_options);
			var list = new List<string> { "abc", "dfe", "test" };
			target.SetVariable("list", list);

			var lambda = target.Parse("list.Select(str => str.Length)");

			Assert.That(lambda.ReturnType, Is.EqualTo(typeof(IEnumerable<int>)));
		}

		[Test]
		public void Where_inferred_parameter_type()
		{
			var target = new Interpreter(_options);
			var list = new List<int> { 1, 10, 19, 21 };
			target.SetVariable("myList", list);

			var results = target.Eval<IEnumerable<int>>("myList.Where(x => x >= 19)");

			Assert.That(results.Count(), Is.EqualTo(2));
			Assert.That(results, Is.EqualTo(new[] { 19, 21 }));
		}

		[Test]
		public void Where_explicit_parameter_type()
		{
			var target = new Interpreter(_options);
			var list = new List<int> { 1, 10, 19, 21 };
			target.SetVariable("myList", list);

			var results = target.Eval<IEnumerable<int>>("myList.Where((int x) => x >= 19)");

			Assert.That(results.Count(), Is.EqualTo(2));
			Assert.That(results, Is.EqualTo(new[] { 19, 21 }));
		}

		[Test]
		public void Select_inferred_return_type()
		{
			var target = new Interpreter(_options);
			var list = new List<int> { 1, 10, 19, 21 };
			target.SetVariable("myList", list);

			var results = target.Eval<IEnumerable<char>>("myList.Select(i => i.ToString()).Select(str => str[0])");

			Assert.That(results.Count(), Is.EqualTo(4));
			Assert.That(results, Is.EqualTo(new[] { '1', '1', '1', '2' }));
		}

		[Test]
		public void Where_select()
		{
			var target = new Interpreter(_options);
			var list = new List<string> { "this", "is", "awesome" };
			target.SetVariable("myList", list);

			var results = target.Eval<IEnumerable<string>>("myList.Where(str => str.Length > 5).Select(str => str.ToUpper())");

			Assert.That(results.Count(), Is.EqualTo(1));
			Assert.That(results, Is.EqualTo(new[] { "AWESOME" }));
		}

		[Test]
		public void Lambda_expression_to_delegate()
		{
			var target = new Interpreter(_options);
			var lambda = target.Eval<Func<string, string>>("str => str.ToUpper()");
			Assert.That(lambda.Invoke("test"), Is.EqualTo("TEST"));
		}

		[Test]
		public void Lambda_expression_no_arguments()
		{
			var target = new Interpreter(_options);
			var lambda = target.Eval<Func<int>>("() => 5 + 6");
			Assert.That(lambda.Invoke(), Is.EqualTo(11));
		}

		[Test]
		public void Lambda_expression_to_delegate_multi_params()
		{
			var target = new Interpreter(_options);
			target.SetVariable("increment", 3);
			var lambda = target.Eval<Func<int, string, string>>("(i, str) => str.ToUpper() + (i + increment)");
			Assert.That(lambda.Invoke(5, "test"), Is.EqualTo("TEST8"));
		}

		[Test]
		public void Select_many_str()
		{
			var target = new Interpreter(_options);
			var list = new List<string> { "ab", "cd" };
			target.SetVariable("myList", list);

			var results = target.Eval<IEnumerable<char>>("myList.SelectMany(str => str)");

			Assert.That(results.Count(), Is.EqualTo(4));
			Assert.That(results, Is.EqualTo(new[] { 'a', 'b', 'c', 'd' }));
		}

		[Test]
		public void Select_many()
		{
			var target = new Interpreter(_options);
			var list = new[]{
				new { Strings = new[] { "ab", "cd" } },
				new { Strings = new[] { "ef", "gh" } },
			};

			target.SetVariable("myList", list);

			var results = target.Eval<IEnumerable<string>>("myList.SelectMany(obj => obj.Strings)");

			Assert.That(results.Count(), Is.EqualTo(4));
			Assert.That(results, Is.EqualTo(new[] { "ab", "cd", "ef", "gh" }));
		}

		[Test]
		public void Nested_lambda()
		{
			var target = new Interpreter(_options);
			var list = new List<string> { "ab", "cd" };
			target.SetVariable("myList", list);

			var results = target.Eval<IEnumerable<char>>("myList.Select(str => str.SingleOrDefault(c => c == 'd')).Where(c => c != '\0')");

			Assert.That(results.Count(), Is.EqualTo(1));
			Assert.That(results, Is.EqualTo(new[] { 'd' }));
		}

		[Test]
		public void Lambda_candidate_is_generic_parameter()
		{
			var target = new Interpreter(_options).Reference(typeof(ExtensionMethodExt));
			var str = "cd";
			target.SetVariable("str", str);

			var result = target.Eval<char>("str.MySingleOrDefault(c => c == 'd')");
			Assert.That(result, Is.EqualTo(str.SingleOrDefault(c => c == 'd')));
		}

		[Test]
		public void Lambda_candidate_with_multiple_parameters()
		{
			var target = new Interpreter(_options).Reference(typeof(ExtensionMethodExt));
			var str = "cd";
			target.SetVariable("str", str);

			var result = target.Eval<char>("str.WithSeveralParams((c) => c == 'd')");
			Assert.That(result, Is.EqualTo('d'));

			result = target.Eval<char>("str.WithSeveralParams((c, i) => c == 'd')");
			Assert.That(result, Is.EqualTo('d'));

			result = target.Eval<char>("str.WithSeveralParams((c, i, str2) => c == 'd')");
			Assert.That(result, Is.EqualTo('d'));
		}

		[Test]
		public void Sum()
		{
			var target = new Interpreter(_options);
			var list = new List<int> { 1, 2, 3 };
			target.SetVariable("myList", list);

			var results = target.Eval<int>("myList.Sum()");

			Assert.That(results, Is.EqualTo(6));
		}

		[Test]
		public void Max()
		{
			var target = new Interpreter(_options);
			var list = new List<int> { 1, 2, 3 };
			target.SetVariable("myList", list);

			var results = target.Eval<int>("myList.Max()");

			Assert.That(results, Is.EqualTo(3));
		}

		[Test]
		public void Sum_string_length()
		{
			var target = new Interpreter(_options);
			var list = new List<string> { "abc", "dfe", "test" };
			target.SetVariable("myList", list);

			var results = target.Eval<int>("myList.Sum(str => str.Length)");

			Assert.That(results, Is.EqualTo(10));
		}

		[Test]
		public void Parent_scope_variable()
		{
			var target = new Interpreter(_options);
			var list = new List<int> { 1, 2, 3 };
			target.SetVariable("myList", list);
			target.SetVariable("increment", 3);

			var results = target.Eval<IEnumerable<int>>("myList.Select(i => i + increment)");

			Assert.That(results.Count(), Is.EqualTo(3));
			Assert.That(results, Is.EqualTo(new[] { 4, 5, 6 }));
		}

		[Test]
		public void Lambda_with_multiple_params()
		{
			var target = new Interpreter(_options);
			var list = new List<string> { "aaaaa", "bbbb", "ccc", "ddd" };
			target.SetVariable("myList", list);

			var results = target.Eval<IEnumerable<string>>("myList.TakeWhile((item, idx) => idx <= 2 && item.Length >= 3)");

			Assert.That(results.Count(), Is.EqualTo(3));
			Assert.That(results, Is.EqualTo(new[] { "aaaaa", "bbbb", "ccc" }));
		}

		[Test]
		public void Two_lambda_parameters()
		{
			var target = new Interpreter(_options);
			target.Reference(typeof(WithProp));

			var list = new List<string> { "aaaaa", "bbbb", "ccc", "ddd" };
			target.SetVariable("myList", list);
			var results = target.Eval<Dictionary<string, int>>("myList.ToDictionary(str => new WithProp { MyStr = str }.MyStr, str => str.Length)");

			Assert.That(results.Count, Is.EqualTo(4));
			Assert.That(results, Is.EqualTo(list.ToDictionary(str => new WithProp { MyStr = str }.MyStr, str => str.Length)));
		}

		private class WithProp
		{
			public string MyStr { get; set; }
		}

		[Test]
		public void Zip()
		{
			var target = new Interpreter(_options);
			var strList = new List<string> { "aa", "bb", "cc", "dd" };
			var intList = new List<int> { 1, 2, 3 };
			target.SetVariable("strList", strList);
			target.SetVariable("intList", intList);
			var results = target.Eval<IEnumerable<string>>("strList.Zip(intList, (str, i) => str + i)");

			Assert.That(results.Count(), Is.EqualTo(3));
			Assert.That(results, Is.EqualTo(strList.Zip(intList, (str, i) => str + i)));
		}

		[Test]
		public void Lambda_with_parameter()
		{
			var target = new Interpreter(InterpreterOptions.Default | InterpreterOptions.LambdaExpressions);
			var listInt = target.Eval<IEnumerable<int>>("list.Where(n => n > x)", new Parameter("list", new[] { 1, 2, 3 }), new Parameter("x", 1));
			Assert.That(listInt, Is.EqualTo(new[] { 2, 3 }));

			// ensure the parameters can be reused with different values
			listInt = target.Eval<IEnumerable<int>>("list.Where(n => n > x)", new Parameter("list", new[] { 2, 4, 5 }), new Parameter("x", 2));
			Assert.That(listInt, Is.EqualTo(new[] { 4, 5 }));
		}

		[Test]
		public void Lambda_with_parameter_AsCompiledLambda()
		{
			var target = new Interpreter(InterpreterOptions.Default | InterpreterOptions.LambdaExpressions);
			var parm = new Parameter("x", 1);
			var list = new Parameter("list", new[] { 1, 2, 3 });
			var listLamba = target.Parse("list.Where(n => n > x)", list, parm).Compile<Func<int[], int, IEnumerable<int>>>();
			var result = listLamba(list.Value as int[], 2);
			Assert.That(result, Is.EqualTo(new[] { 3 }));

			var listInt = listLamba(list.Value as int[], 1);
			Assert.That(listInt, Is.EqualTo(new[] { 2, 3 }));

			// ensure the parameters can be reused with different values
			listInt = target.Eval<IEnumerable<int>>("list.Where(n => n > x)", new Parameter("list", new[] { 2, 4, 5 }), new Parameter("x", 2));
			Assert.That(listInt, Is.EqualTo(new[] { 4, 5 }));
		}

		[Test]
		public void Lambda_with_parameter_2()
		{
			var target = new Interpreter(InterpreterOptions.Default | InterpreterOptions.LambdaExpressions);
			var listInt = target.Eval<IEnumerable<int>>("list.Select(n => n - 1).Where(n => n > x).Select(n => n + x)", new Parameter("list", new[] { 1, 2, 3 }), new Parameter("x", 1));
			Assert.That(listInt, Is.EqualTo(new[] { 3 }));
		}

		[Test]
		public void Lambda_with_variable()
		{
			var target = new Interpreter(InterpreterOptions.Default | InterpreterOptions.LambdaExpressions);
			target.SetVariable("list", new[] { 1, 2, 3 });
			target.SetVariable("x", 1);

			var listInt = target.Eval<IEnumerable<int>>("list.Where(n => n > x)");
			Assert.That(listInt, Is.EqualTo(new[] { 2, 3 }));
		}

		public class NestedLambdaTestClass
		{
			public NestedLambdaTestClass()
			{
			}

			public List<NestedLambdaTestClass> Children
			{
				get; set;
			}

			public string Name
			{
				get; set;
			}

			// TODO
			// Add support for non generics with our lambda evaluation
			// The below fails to compile
			// public string GetChildrenIdentifiers<T>(Func<NestedLambdaTestClass, T> f)
			public string GetChildrenIdentifiers<T>(Func<NestedLambdaTestClass, T> f)
			{
				if (Children == null)
				{
					return string.Empty;
				}
				return string.Join(",", Children.Select(f));
			}
		}

		[Test]
		public void Lambda_WithMultipleNestedExpressions()
		{
			var root = BuildNestedTestClassHierarchy();
			var expectedResult = root.GetChildrenIdentifiers(
				// root
				l1 => l1.Name + l1.GetChildrenIdentifiers(
					// level 2, references my parameter, plus original lamda
					l2 => l2.Name + l1.Name + l2.GetChildrenIdentifiers(
						// level 3, references my parameter, plus parameter from l1 lamda
						l3 => l3.Name + l2.Name + l3.GetChildrenIdentifiers(
							// level 4, references my parameter, plus all parameters that have been used 
							l4 => l4.Name + l2.Name + l3.Name + l1.Name + root.Name)
							)));

			var target = new Interpreter(InterpreterOptions.Default | InterpreterOptions.LambdaExpressions);

			var evalResult = target.Eval<string>(@"root.GetChildrenIdentifiers(
				l1 => l1.Name + l1.GetChildrenIdentifiers(
					l2 => l2.Name + l1.Name + l2.GetChildrenIdentifiers(
						l3 => l3.Name + l2.Name + l3.GetChildrenIdentifiers(
							l4 => l4.Name + l2.Name + l3.Name + l1.Name + root.Name)
							)))", new Parameter(nameof(root), root));
			Assert.That(evalResult, Is.EqualTo(expectedResult));
		}

		[Test]
		public void Lambda_SameParameterNameInDifferentLambdas()
		{
			var root = BuildNestedTestClassHierarchy();
			var expectedResult = root.GetChildrenIdentifiers(
				// root
				l1 => l1.Name + l1.GetChildrenIdentifiers(
					// level 2, references my parameter, plus original lamda
					l2 => l2.Name + l1.Name + l2.GetChildrenIdentifiers(l3 => l2.Name) + l2.GetChildrenIdentifiers(
						// level 3, references my parameter, plus parameter from l1 lamda
						l3 => l3.Name + l2.Name + l3.GetChildrenIdentifiers(
							// level 4, references my parameter, plus all parameters that have been used 
							l4 => l4.Name + l2.Name + l3.Name + l1.Name + root.Name)
							)));

			var target = new Interpreter(InterpreterOptions.Default | InterpreterOptions.LambdaExpressions);

			var evalResult = target.Eval<string>(@"root.GetChildrenIdentifiers(
				l1 => l1.Name + l1.GetChildrenIdentifiers(
					l2 => l2.Name + l1.Name + l2.GetChildrenIdentifiers(l3 => l2.Name) + + l2.GetChildrenIdentifiers(
						l3 => l3.Name + l2.Name + l3.GetChildrenIdentifiers(
							l4 => l4.Name + l2.Name + l3.Name + l1.Name + root.Name)
							)))", new Parameter(nameof(root), root));
			Assert.That(evalResult, Is.EqualTo(expectedResult));
		}

		[Test]
		public void Lambda_CannotUseDuplicateParameterInSubLambda()
		{
			var target = new Interpreter(InterpreterOptions.Default | InterpreterOptions.LambdaExpressions);
			Assert.Throws<ParseException>(() => target.Parse(@"root.GetChildrenIdentifiers(
				l1 => l1.Name + l1.GetChildrenIdentifiers(
					l2 => l2.Name + l1.Name + l2.GetChildrenIdentifiers(l2 => l2.Name) + l2.GetChildrenIdentifiers(
						l3 => l3.Name + l2.Name + l3.GetChildrenIdentifiers(
							l4 => l4.Name + l2.Name + l3.Name + l1.Name + root.Name)
							)))", new Parameter("root", typeof(NestedLambdaTestClass))));
		}

		private class Npc
		{
			public int Money { get; set; }
			public void AddMoney(Action<Npc, int, string> action)
			{
				action(this, 10, "test");
			}
		}

		[Test]
		public void Lambda_ShouldAllowActionLambda()
		{
			var target = new Interpreter(InterpreterOptions.LambdaExpressions);

			var list = new List<Npc>() { new Npc { Money = 10 } };
			target.SetVariable("list", list);

			var result = target.Eval(@"list.ForEach(n => n.Money = 5)");
			Assert.That(result, Is.Null);
			Assert.That(list[0].Money, Is.EqualTo(5));
		}

		[Test]
		public void Lambda_MultipleActionLambdaParameters()
		{
			var target = new Interpreter(InterpreterOptions.LambdaExpressions);

			var npc = new Npc { Money = 10 };
			target.SetVariable("npc", npc);

			var result = target.Eval(@"npc.AddMoney((n, i, str) => n.Money = i + str.Length)");
			Assert.That(result, Is.Null);
			Assert.That(npc.Money, Is.EqualTo(14));
		}

		private static NestedLambdaTestClass BuildNestedTestClassHierarchy()
		{
			return new NestedLambdaTestClass()
			{
				Name = "Root",
				Children = new List<NestedLambdaTestClass>()
				{
					new NestedLambdaTestClass()
					{
						Name = "A",
						Children = new List<NestedLambdaTestClass>()
						{
							new NestedLambdaTestClass()
							{
								Name = "B",
								Children = new List<NestedLambdaTestClass>()
								{
									new NestedLambdaTestClass()
									{
										Name = "C",
								Children = new List<NestedLambdaTestClass>()
								{
									new NestedLambdaTestClass()
									{
										Name = "C"
									},
									new NestedLambdaTestClass()
									{
										Name = "F"
									}
								}
									},
									new NestedLambdaTestClass()
									{
										Name = "F",
								Children = new List<NestedLambdaTestClass>()
								{
									new NestedLambdaTestClass()
									{
										Name = "C"
									},
									new NestedLambdaTestClass()
									{
										Name = "F"
									}
								}
									}
								}
							},
							new NestedLambdaTestClass()
							{
								Name = "D",
								Children = new List<NestedLambdaTestClass>()
								{
									new NestedLambdaTestClass()
									{
										Name = "E",
								Children = new List<NestedLambdaTestClass>()
								{
									new NestedLambdaTestClass()
									{
										Name = "C"
									},
									new NestedLambdaTestClass()
									{
										Name = "F"
									}
								}
									},
									new NestedLambdaTestClass()
									{
										Name = "G",
								Children = new List<NestedLambdaTestClass>()
								{
									new NestedLambdaTestClass()
									{
										Name = "C"
									},
									new NestedLambdaTestClass()
									{
										Name = "F"
									}
								}
									}
								}
							}
						}
					},
					new NestedLambdaTestClass()
					{
						Name = "B",
						Children = new List<NestedLambdaTestClass>()
						{
							new NestedLambdaTestClass()
							{
								Name = "B",
								Children = new List<NestedLambdaTestClass>()
								{
									new NestedLambdaTestClass()
									{
										Name = "C",
								Children = new List<NestedLambdaTestClass>()
								{
									new NestedLambdaTestClass()
									{
										Name = "C"
									},
									new NestedLambdaTestClass()
									{
										Name = "F"
									}
								}
									},
									new NestedLambdaTestClass()
									{
										Name = "F",
								Children = new List<NestedLambdaTestClass>()
								{
									new NestedLambdaTestClass()
									{
										Name = "C"
									},
									new NestedLambdaTestClass()
									{
										Name = "F"
									}
								}
									}
								}
							},
							new NestedLambdaTestClass()
							{
								Name = "D",
								Children = new List<NestedLambdaTestClass>()
								{
									new NestedLambdaTestClass()
									{
										Name = "E",
								Children = new List<NestedLambdaTestClass>()
								{
									new NestedLambdaTestClass()
									{
										Name = "C"
									},
									new NestedLambdaTestClass()
									{
										Name = "F",
								Children = new List<NestedLambdaTestClass>()
								{
									new NestedLambdaTestClass()
									{
										Name = "C"
									},
									new NestedLambdaTestClass()
									{
										Name = "F"
									}
								}
									}
								}
									},
									new NestedLambdaTestClass()
									{
										Name = "G"
									}
								}
							}
						}
					}
				}
			};
		}
	}

	/// <summary>
	/// Ensure that a lambda expression is matched to a parameter of type delegate
	/// (so the 1st overload shouldn't be considered during resolution)
	/// </summary>
	internal static class ExtensionMethodExt
	{
		public static TSource MySingleOrDefault<TSource>(this IEnumerable<TSource> source, TSource defaultValue)
		{
			return source.SingleOrDefault();
		}

		public static TSource MySingleOrDefault<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
		{
			return source.SingleOrDefault(predicate);
		}

		public static TSource WithSeveralParams<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
		{
			return source.SingleOrDefault(predicate);
		}

		public static TSource WithSeveralParams<TSource>(this IEnumerable<TSource> source, Func<TSource, int, bool> predicate)
		{
			return source.SingleOrDefault(_ => predicate(_, 0));
		}

		public static TSource WithSeveralParams<TSource>(this IEnumerable<TSource> source, Func<TSource, int, string, bool> predicate)
		{
			return source.SingleOrDefault(_ => predicate(_, 0, string.Empty));
		}
	}
}
