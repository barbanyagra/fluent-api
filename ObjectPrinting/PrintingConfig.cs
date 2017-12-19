using System;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;

namespace ObjectPrinting
{
    public class PrintingConfig<TOwner>
    {
        public PrintingConfig()
        {
            excludedTypes = ImmutableHashSet<Type>.Empty;
            excludedFields = ImmutableHashSet<string>.Empty;
            typeToSerializer = ImmutableDictionary<Type, Func<object, string>>.Empty;
            propertyNameToSerializer = ImmutableDictionary<string, Func<object, string>>.Empty;
        }
        
        private PrintingConfig(
            ImmutableHashSet<Type> excludedTypes,
            ImmutableHashSet<string> excludedFields,
            ImmutableDictionary<Type, Func<object, string>> typeToSerializer,
            ImmutableDictionary<string, Func<object, string>> propertyNameToSerializer)
        {
            this.excludedTypes = excludedTypes;
            this.excludedFields = excludedFields;
            this.typeToSerializer = typeToSerializer;
            this.propertyNameToSerializer = propertyNameToSerializer;
        }

        private ImmutableHashSet<Type> excludedTypes;
        private ImmutableHashSet<string> excludedFields;
        private ImmutableDictionary<Type, Func<object, string>> typeToSerializer;
        private ImmutableDictionary<string, Func<object, string>> propertyNameToSerializer;

        public string PrintToString(TOwner obj)
            => PrintToString(obj, 0);

        private string PrintToString(object obj, int nestingLevel, string properyName = null)
        {
            if (obj == null)
                return "null" + Ln();

            if (properyName != null && propertyNameToSerializer.ContainsKey(properyName))
                return propertyNameToSerializer[properyName](obj) + Ln();

            if (typeToSerializer.ContainsKey(obj.GetType()))
                return typeToSerializer[obj.GetType()](obj) + Ln();

            var finalTypes = new[]
            {
                typeof(int), typeof(double), typeof(float), typeof(string),
                typeof(DateTime), typeof(TimeSpan)
            };

            if (finalTypes.Contains(obj.GetType()))
                return obj + Ln();

            var identation = new string('\t', nestingLevel + 1);
            var type = obj.GetType();
            var propertiesPrinted = type.GetProperties()
                .Where(x => !excludedTypes.Contains(x.PropertyType))
                .Where(x => !excludedFields.Contains(GetName(properyName, x.Name)))
                .Select(propertyInfo =>
                    identation + propertyInfo.Name + " = " +
                    PrintToString(
                        propertyInfo.GetValue(obj),
                        nestingLevel + 1,
                        GetName(properyName, propertyInfo.Name)));

            return type.Name + Ln() + string.Join("", propertiesPrinted);
        }

        private static string GetName(string parent, string child)
        {
            return (parent != null ? parent + "." : "") + child;
        }

        private static string Ln() => Environment.NewLine;

        public TypePrintingConfig<T, TOwner> ConfigureType<T>()
        {
            return new TypePrintingConfig<T, TOwner>(typeSerializer => WithTypeToSerializer(
                typeToSerializer
                    .Remove(typeof(T))
                    .Add(typeof(T), typeSerializer)));
        }

        public TypePrintingConfig<T, TOwner> ConfigureProperty<T>(Expression<Func<TOwner, T>> selector)
        {
            var member = selector.Body as MemberExpression;
            if (member == null) throw new ArgumentException("selector should be MemberExpression"); 
            return new TypePrintingConfig<T, TOwner>(typeSerializer => WithPropertyNameToSerializer(
                propertyNameToSerializer
                    .Remove(member.Member.Name)
                    .Add(member.Member.Name, typeSerializer)));
        }

        public PrintingConfig<TOwner> ExcludeType<TExcludedType>()
        {
            return WithExcludedTypes(excludedTypes.Add(typeof(TExcludedType)));
        }

        public PrintingConfig<TOwner> ExcludeProperty<TProperty>(Expression<Func<TOwner, TProperty>> selector)
        {
            var member = selector.Body as MemberExpression;
            if (member == null) throw new ArgumentException("selector should be MemberExpression"); 
            return WithExcludedFields(excludedFields.Add(member.Member.Name));
        }

        private PrintingConfig<TOwner> WithExcludedTypes(ImmutableHashSet<Type> excludedTypes)
        {
            return new PrintingConfig<TOwner>(
                excludedTypes,
                excludedFields,
                typeToSerializer,
                propertyNameToSerializer
            );
        }

        private PrintingConfig<TOwner> WithExcludedFields(ImmutableHashSet<string> excludedFields)
        {
            return new PrintingConfig<TOwner>(
                excludedTypes,
                excludedFields,
                typeToSerializer,
                propertyNameToSerializer
            );
        }

        private PrintingConfig<TOwner> WithTypeToSerializer(
            ImmutableDictionary<Type, Func<object, string>> typeToSerializer)
        {
            return new PrintingConfig<TOwner>(
                excludedTypes,
                excludedFields,
                typeToSerializer,
                propertyNameToSerializer
            );
        }

        private PrintingConfig<TOwner> WithPropertyNameToSerializer(
            ImmutableDictionary<string, Func<object, string>> propertyNameToSerializer)
        {
            return new PrintingConfig<TOwner>(
                excludedTypes,
                excludedFields,
                typeToSerializer,
                propertyNameToSerializer
            );
        }
    }

    public static class PrintingExtensions
    {
        public static string PrintToString<T>(this T obj)
            => ObjectPrinter.For<T>().PrintToString(obj);

        public static string PrintToString<T>(this T obj, Func<PrintingConfig<T>, PrintingConfig<T>> configurer)
            => configurer(ObjectPrinter.For<T>()).PrintToString(obj);
    }
}