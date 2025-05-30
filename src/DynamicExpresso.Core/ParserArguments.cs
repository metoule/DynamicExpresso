using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using DynamicExpresso.Exceptions;
using DynamicExpresso.Parsing;

namespace DynamicExpresso
{
	internal class ParserArguments
	{
		private readonly Dictionary<string, Parameter> _declaredParameters;

		private readonly HashSet<Parameter> _usedParameters = new HashSet<Parameter>();
		private readonly HashSet<ReferenceType> _usedTypes = new HashSet<ReferenceType>();
		private readonly HashSet<Identifier> _usedIdentifiers = new HashSet<Identifier>();

		public ParserArguments(
			string expressionText,
			ParserSettings settings,
			Type expressionReturnType,
			IEnumerable<Parameter> declaredParameters
		)
		{
			ExpressionText = expressionText;
			ExpressionReturnType = expressionReturnType;

			Settings = settings;
			_declaredParameters = new Dictionary<string, Parameter>(settings.KeyComparer);
			foreach (var pe in declaredParameters)
			{
				try
				{
					_declaredParameters.Add(pe.Name, pe);
				}
				catch (ArgumentException)
				{
					throw new DuplicateParameterException(pe.Name);
				}
			}
		}

		public ParserSettings Settings { get; private set; }
		public string ExpressionText { get; private set; }
		public Type ExpressionReturnType { get; private set; }
		public IEnumerable<Parameter> DeclaredParameters { get { return _declaredParameters.Values; } }

		public IEnumerable<Parameter> UsedParameters
		{
			get { return _usedParameters; }
		}

		public IEnumerable<ReferenceType> UsedTypes
		{
			get { return _usedTypes; }
		}

		public IEnumerable<Identifier> UsedIdentifiers
		{
			get { return _usedIdentifiers; }
		}

		public bool TryGetKnownType(string name, out Type type)
		{
			if (Settings.KnownTypes.TryGetValue(name, out var reference))
			{
				_usedTypes.Add(reference);
				type = reference.Type;
				return true;
			}

			type = null;
			return false;
		}

		/// <summary>
		/// Returns true if the known types contain a generic type definition with the given name + any arity (e.g. name`1).
		/// </summary>
		internal bool HasKnownGenericTypeDefinition(string name)
		{
			var regex = new Regex("^" + name + "`\\d+$");
			return Settings.KnownTypes.Values.Any(refType => regex.IsMatch(refType.Name) && refType.Type.IsGenericTypeDefinition);
		}

		public bool TryGetIdentifier(string name, out Expression expression)
		{
			if (Settings.Identifiers.TryGetValue(name, out var identifier))
			{
				_usedIdentifiers.Add(identifier);
				expression = identifier.Expression;
				return true;
			}

			expression = null;
			return false;
		}

		/// <summary>
		/// Get the parameter and mark is as used.
		/// </summary>
		public bool TryGetParameters(string name, out ParameterExpression expression)
		{
			if (_declaredParameters.TryGetValue(name, out var parameter))
			{
				_usedParameters.Add(parameter);
				expression = parameter.Expression;
				return true;
			}

			expression = null;
			return false;
		}

		public IEnumerable<MethodInfo> GetExtensionMethods(string methodName)
		{
			var comparer = Settings.KeyComparer;
			return Settings.ExtensionMethods.Where(p => comparer.Equals(p.Name, methodName));
		}
	}
}
