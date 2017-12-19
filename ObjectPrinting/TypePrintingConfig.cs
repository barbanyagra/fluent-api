using System;
using System.Globalization;

namespace ObjectPrinting
{
    public class TypePrintingConfig<TType, TOwner> : ITypePrintingConfig<TType, TOwner>
    {
        private readonly Func<Func<object, string>, PrintingConfig<TOwner>> setSerializerFunc;

        public TypePrintingConfig(Func<Func<object, string>, PrintingConfig<TOwner>> setSerializerFunc)
        {
            this.setSerializerFunc = setSerializerFunc;
        }

        public PrintingConfig<TOwner> SetSerializer(Func<TType, string> serializer)
            => setSerializerFunc(o => serializer((TType) o));

        Func<Func<object, string>, PrintingConfig<TOwner>> ITypePrintingConfig<TType, TOwner>.GetSetSerializerFunc
            => setSerializerFunc;
    }

    public interface ITypePrintingConfig<TType, TOwner>
    {
        Func<Func<object, string>, PrintingConfig<TOwner>> GetSetSerializerFunc { get; }
    }

    public static class TypePrintingExtensions
    {
        public static PrintingConfig<TOwner> SetCulture<TOwner>(this ITypePrintingConfig<int, TOwner> config, CultureInfo cultureInfo)
        {
            return config.GetSetSerializerFunc(o => ((int) o).ToString(cultureInfo));
        }
        
        public static PrintingConfig<TOwner> SetCulture<TOwner>(this ITypePrintingConfig<long, TOwner> config, CultureInfo cultureInfo)
        {
            return config.GetSetSerializerFunc(o => ((long) o).ToString(cultureInfo));
        }
        
        public static PrintingConfig<TOwner> SetCulture<TOwner>(this ITypePrintingConfig<double, TOwner> config, CultureInfo cultureInfo)
        {
            return config.GetSetSerializerFunc(o => ((double) o).ToString(cultureInfo));
        }
        
        public static PrintingConfig<TOwner> ShrinkToLength<TOwner>(this ITypePrintingConfig<string, TOwner> config, int length)
        {
            return config.GetSetSerializerFunc(o => ((string) o).Truncate(length));
        }
    }
    
    public static class StringExt
    {
        public static string Truncate(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength); 
        }
    }
}