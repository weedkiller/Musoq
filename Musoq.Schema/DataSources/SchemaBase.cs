﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Musoq.Plugins.Attributes;
using Musoq.Schema.Helpers;
using Musoq.Schema.Managers;
using Musoq.Schema.Reflection;

namespace Musoq.Schema.DataSources
{
    public abstract class SchemaBase : ISchema
    {
        private readonly MethodsAggregator _aggregator;

        private IDictionary<string, Reflection.ConstructorInfo[]> Constructors { get; } = new Dictionary<string, Reflection.ConstructorInfo[]>();
        private List<SchemaMethodInfo> ConstructorsMethods { get; } = new List<SchemaMethodInfo>();
        private List<SchemaMethodInfo> RawConstructorsMethods { get; } = new List<SchemaMethodInfo>();
        private IDictionary<string, object[]> AdditionalArguments { get; } = new Dictionary<string, object[]>();

        protected SchemaBase(string name, MethodsAggregator methodsAggregator)
        {
            Name = name;
            _aggregator = methodsAggregator;

            AddSource<SingleRowSource>("empty");
            AddTable<SingleRowSchemaTable>("empty");
        }

        public void AddSource<TType>(string name, params object[] args)
        {
            var sourceName = $"{name.ToLowerInvariant()}_source";
            AddToConstructors<TType>(sourceName);
            AdditionalArguments.Add(sourceName, args);

            RawConstructorsMethods.AddRange(TypeHelper.GetSchemaMethodInfosForType<TType>(name));
        }

        public void AddTable<TType>(string name)
        {
            AddToConstructors<TType>($"{name.ToLowerInvariant()}_table");
        }

        private void AddToConstructors<TType>(string name)
        {
            var schemaMethodInfos = TypeHelper
                .GetSchemaMethodInfosForType<TType>(name);

            ConstructorsMethods.AddRange(schemaMethodInfos);

            var schemaMethods = schemaMethodInfos
                .Select(schemaMethod => schemaMethod.ConstructorInfo)
                .ToArray();

            Constructors.Add(name, schemaMethods);
        }

        public string Name { get; }

        public virtual ISchemaTable GetTableByName(string name, params object[] parameters)
        {
            var methods = GetConstructors($"{name.ToLowerInvariant()}_table").Select(c => c.ConstructorInfo).ToArray();

            if (!TryMatchConstructorWithParams(methods, parameters, out var constructorInfo))
                throw new NotSupportedException($"Unrecognized method {name}.");

            return (ISchemaTable)constructorInfo.OriginConstructor.Invoke(parameters);
        }

        public virtual RowSource GetRowSource(string name, InterCommunicator interCommunicator, params object[] parameters)
        {
            var sourceName = $"{name.ToLowerInvariant()}_source";

            var methods = GetConstructors(sourceName).Select(c => c.ConstructorInfo).ToArray();

            if (AdditionalArguments.ContainsKey(sourceName))
                parameters = parameters.ExpandParameters(AdditionalArguments[sourceName]);

            if (!TryMatchConstructorWithParams(methods, parameters, out var constructorInfo))
                throw new NotSupportedException($"Unrecognized method {name}.");

            if (constructorInfo.SupportsInterCommunicator)
                parameters = parameters.ExpandParameters(interCommunicator);

            return (RowSource)constructorInfo.OriginConstructor.Invoke(parameters);
        }

        public SchemaMethodInfo[] GetConstructors(string methodName)
        {
            return GetConstructors().Where(constr => constr.MethodName == methodName).ToArray();
        }

        public virtual SchemaMethodInfo[] GetConstructors()
        {
            return ConstructorsMethods.ToArray();
        }

        public SchemaMethodInfo[] GetRawConstructors()
        {
            return RawConstructorsMethods.ToArray();
        }

        public SchemaMethodInfo[] GetRawConstructors(string methodName)
        {
            return RawConstructorsMethods.Where(constr => constr.MethodName == methodName).ToArray();
        }

        public bool TryResolveAggreationMethod(string method, Type[] parameters, out MethodInfo methodInfo)
        {
            var founded = _aggregator.TryResolveMethod(method, parameters, out methodInfo);

            if (founded)
                return methodInfo.GetCustomAttribute<AggregationMethodAttribute>() != null;

            return false;
        }

        public MethodInfo ResolveMethod(string method, Type[] parameters)
        {
            return _aggregator.ResolveMethod(method, parameters);
        }

        protected bool ParamsMatchConstructor(Reflection.ConstructorInfo constructor, object[] parameters)
        {
            bool matchingResult = true;

            if (parameters.Length != constructor.Arguments.Length)
                return false;

            for (int i = 0; i < parameters.Length && matchingResult; ++i)
            {
                matchingResult &= 
                    constructor.Arguments[i].Type.IsAssignableFrom(
                        parameters[i].GetType());
            }

            return matchingResult;
        }

        protected bool TryMatchConstructorWithParams(Reflection.ConstructorInfo[] constructors, object[] parameters, out Reflection.ConstructorInfo foundedConstructor)
        {
            foreach(var constructor in constructors)
            {
                if(ParamsMatchConstructor(constructor, parameters))
                {
                    foundedConstructor = constructor;
                    return true;
                }
            }

            foundedConstructor = null;
            return false;
        }
    }
}