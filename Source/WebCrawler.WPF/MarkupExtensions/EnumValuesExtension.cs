using System;
using System.Windows.Markup;

namespace WebCrawler.WPF.MarkupExtensions
{
    [MarkupExtensionReturnType(typeof(object[]))]
    public class EnumValuesExtension : MarkupExtension
    {
        public EnumValuesExtension()
        { 
        }

        public EnumValuesExtension(Type enumType)
        {
            EnumType = enumType;
        }

        [ConstructorArgument("enumType")]
        public Type EnumType { get; set; }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (EnumType == null || !EnumType.IsEnum)
            {
                throw new ArgumentException("Enum type is required");
            }

            return Enum.GetValues(EnumType);
        }
    }
}
