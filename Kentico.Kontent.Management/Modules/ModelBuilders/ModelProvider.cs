﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kentico.Kontent.Management.Models.Assets;
using Kentico.Kontent.Management.Models.Items;
using Kentico.Kontent.Management.Models.StronglyTyped;
using Newtonsoft.Json.Linq;

namespace Kentico.Kontent.Management.Modules.ModelBuilders
{
    internal class ModelProvider : IModelProvider
    {
        public IPropertyMapper PropertyMapper { get; set; }

        internal ModelProvider(IPropertyMapper propertyMapper = null)
        {
            PropertyMapper = propertyMapper ?? new PropertyMapper();
        }

        public ContentItemVariantModel<T> GetContentItemVariantModel<T>(ContentItemVariantModel variant) where T : new()
        {
            var result = new ContentItemVariantModel<T>
            {
                Item = variant.Item,
                Language = variant.Language,
                LastModified = variant.LastModified
            };

            var type = typeof(T);
            var instance = new T();

            var properties = type.GetProperties().Where(x => x.SetMethod?.IsPublic ?? false).ToList();

            foreach (var element in variant.Elements)
            {
                var property = properties.FirstOrDefault(x => PropertyMapper.IsMatch(x, element.Key));
                if (property == null) continue;

                var value = GetTypedElementValue(property.PropertyType, element.Value);
                if (value != null)
                {
                    property.SetValue(instance, value);
                }
            }

            result.Elements = instance;
            return result;
        }

        public ContentItemVariantUpsertModel GetContentItemVariantUpsertModel<T>(T variantElements) where T : new()
        {
            var type = typeof(T);
            var nameMapping = PropertyMapper.GetNameMapping(type);

            var elements = type.GetProperties()
                .Where(x => (x.GetMethod?.IsPublic ?? false) && nameMapping.ContainsKey(x.Name) && x.GetValue(variantElements) != null)
                .ToDictionary(x => nameMapping[x.Name], x => x.GetValue(variantElements));

            var result = new ContentItemVariantUpsertModel
            {
                Elements = elements
            };

            return result;
        }

        private static object GetTypedElementValue(Type propertyType, object elementValue)
        {
            if (elementValue == null)
            {
                return null;
            }

            if (elementValue.GetType() == propertyType)
            {
                return elementValue;
            }

            if (propertyType == typeof(string))
            {
                return elementValue.ToString();
            }

            if (IsNumericType(propertyType))
            {
                return Convert.ChangeType(elementValue, propertyType);
            }

            if (IsNullableType(propertyType) && IsNumericType(Nullable.GetUnderlyingType(propertyType)))
            {
                return Convert.ChangeType(elementValue, Nullable.GetUnderlyingType(propertyType));
            }

            if (propertyType == typeof(DateTime) || propertyType == typeof(DateTime?))
            {
                return Convert.ToDateTime(elementValue);
            }

            if (IsArrayType(propertyType))
            {
                return JArray.FromObject(elementValue)?.ToObject(propertyType);
            }

            return elementValue;
        }

        private static bool IsNumericType(Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Byte:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Single:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return true;

                default:
                    return false;
            }
        }

        private static bool IsNullableType(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        private static bool IsArrayType(Type type)
        {
            var supportedGenericTypes = new List<Type> { typeof(AssetIdentifier), typeof(ContentItemIdentifier), typeof(MultipleChoiceOptionIdentifier), typeof(TaxonomyTermIdentifier) };

            var isGeneric = type.GetTypeInfo().IsGenericType;
            var isCollection = isGeneric && type.GetInterfaces().Any(gt =>
                    gt.GetTypeInfo().IsGenericType
                    && gt.GetTypeInfo().GetGenericTypeDefinition() == typeof(ICollection<>))
                    && type.GetTypeInfo().IsClass;
            var isEnumerable = isGeneric && type.GetGenericTypeDefinition() == typeof(IEnumerable<>);
            var hasSupportedGenericArgument = type.GenericTypeArguments.Length == 1 && supportedGenericTypes.Contains(type.GenericTypeArguments[0]);

            if ((isCollection || isEnumerable) && hasSupportedGenericArgument)
            {
                return true;
            }

            if (type.IsArray && supportedGenericTypes.Contains(type.GetElementType()))
            {
                return true;

            }

            return false;
        }
    }
}
