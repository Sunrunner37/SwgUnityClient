using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Swg.Client.Core.Assets.Formats.DataTable
{
    /// <summary>
    /// Maps decoded DTII rows to public writable fields/properties. Names match exactly unless overridden.
    /// Model reflection is cached per type; column indices are bound per table. No runtime code generation is used.
    /// Reflection-only model members and converter constructors must be retained when using managed stripping.
    /// </summary>
    public static class DataTableReader
    {
        public static List<T> Read<T>(byte[] bytes)
            where T : class, new() => Read<T>(DataTableFile.Parse(bytes));
        public static List<T> Read<T>(DataTableFile table)
            where T : class, new()
        {
            if (table == null)
                throw new ArgumentNullException(nameof(table));
            var members = Model<T>.Members.Value;
            var bindings = new Binding[members.Length];
            for (int i = 0; i < members.Length; i++)
                bindings[i] = Bind(table, members[i]);
            var result = new List<T>(table.Rows.Count);
            for (int row = 0; row < table.Rows.Count; row++)
            {
                var model = new T();
                foreach (var binding in bindings)
                {
                    if (binding.Index < 0 && !binding.Member.Column.HasDefaultValue)
                        continue;
                    try
                    {
                        object value = binding.Index < 0 ? binding.Member.Column.DefaultValue : table.Rows[row].Values[binding.Index];
                        if (binding.Converter != null)
                            value = binding.Converter.Convert(value);
                        if (binding.Converter != null && !AcceptsValue(binding.Converter.TargetType, value))
                            throw new InvalidCastException("Converter returned a value incompatible with its declared target type.");
                        if (!AcceptsValue(binding.Member.ValueType, value))
                            throw new InvalidCastException("Converter/default returned a value incompatible with " + binding.Member.ValueType.Name);
                        binding.Member.Set(model, value);
                    }
                    catch (Exception error)
                    {
                        var cause = error is TargetInvocationException invocation && invocation.InnerException != null ? invocation.InnerException : error;
                        throw new InvalidDataException("Row " + (row + 1) + ", column '" + binding.Member.Column.Name + "', member '" + typeof(T).Name + "." + binding.Member.Name + "': " + cause.Message, cause);
                    }
                }

                result.Add(model);
            }

            return result;
        }

        static Binding Bind(DataTableFile table, Member member)
        {
            int index = -1;
            for (int i = 0; i < table.Columns.Count; i++)
            {
                if (table.Columns[i].Name != member.Column.Name)
                    continue;
                if (index >= 0)
                    throw new InvalidDataException("Ambiguous column '" + member.Column.Name + "' for member " + member.Name);
                index = i;
            }

            if (index < 0 && !member.Column.Optional)
                throw new InvalidDataException("Missing required column '" + member.Column.Name + "' for member " + member.Name);
            IDataTableValueConverter converter = member.ConverterConstructor == null ? null : (IDataTableValueConverter)member.ConverterConstructor.Invoke(Array.Empty<object>());
            Type input = converter == null ? member.ValueType : converter.SourceType;
            if (input == null || (converter != null && (converter.TargetType == null || !AcceptsType(member.ValueType, converter.TargetType))))
                throw new InvalidOperationException("Invalid converter types for member " + member.Name);
            if (index >= 0 && !AcceptsType(input, table.Columns[index].ValueType))
                throw new InvalidDataException("Column '" + member.Column.Name + "' contains " + table.Columns[index].ValueType.Name + "; member " + member.Name + " expects " + input.Name);
            if (index < 0 && member.Column.HasDefaultValue && !AcceptsValue(input, member.Column.DefaultValue))
                throw new InvalidDataException("Default value is incompatible with member " + member.Name + " (expected " + input.Name + ").");
            return new Binding
            {
                Member = member,
                Index = index,
                Converter = converter
            };
        }

        static bool AcceptsType(Type target, Type source) => target.IsAssignableFrom(source) || (Nullable.GetUnderlyingType(target)?.IsAssignableFrom(source) ?? false);
        static bool AcceptsValue(Type target, object value) => value == null ? !target.IsValueType || Nullable.GetUnderlyingType(target) != null : AcceptsType(target, value.GetType());
        static class Model<T>
        {
            internal static readonly Lazy<Member[]> Members = new Lazy<Member[]>(Discover);
            static Member[] Discover()
            {
                var result = new List<Member>();
                foreach (var info in typeof(T).GetMembers(BindingFlags.Instance | BindingFlags.Public))
                {
                    if (Attribute.IsDefined(info, typeof(DataColumnIgnoreAttribute)))
                        continue;
                    var column = info.GetCustomAttribute<DataColumnAttribute>();
                    Type valueType;
                    Action<object, object> set;
                    if (info is FieldInfo field)
                    {
                        if (field.IsInitOnly)
                        {
                            if (column != null)
                                throw Invalid(info);
                            continue;
                        }

                        valueType = field.FieldType;
                        set = field.SetValue;
                    }
                    else if (info is PropertyInfo property)
                    {
                        if (property.GetIndexParameters().Length != 0 || property.SetMethod == null || !property.SetMethod.IsPublic)
                        {
                            if (column != null)
                                throw Invalid(info);
                            continue;
                        }

                        valueType = property.PropertyType;
                        set = (model, value) => property.SetValue(model, value);
                    }
                    else
                        continue;
                    column = column ?? new DataColumnAttribute(info.Name);
                    // Keep cached model metadata private; bind against each table's column order separately.
                    var definition = new DataColumnAttribute(column.Name ?? info.Name)
                    {
                        Optional = column.Optional,
                        Converter = column.Converter
                    };
                    if (column.HasDefaultValue)
                        definition.DefaultValue = column.DefaultValue;
                    ConstructorInfo constructor = null;
                    if (definition.Converter != null)
                    {
                        if (!typeof(IDataTableValueConverter).IsAssignableFrom(definition.Converter) || definition.Converter.IsAbstract || (constructor = definition.Converter.GetConstructor(Type.EmptyTypes)) == null)
                            throw new InvalidOperationException("Converter for " + typeof(T).Name + "." + info.Name + " must implement IDataTableValueConverter and have a public parameterless constructor.");
                    }

                    result.Add(new Member { Name = info.Name, ValueType = valueType, Column = definition, Set = set, ConverterConstructor = constructor });
                }

                if (result.Count == 0)
                    throw new InvalidOperationException("No public writable data-table members on " + typeof(T).FullName);
                return result.ToArray();
            }

            static InvalidOperationException Invalid(MemberInfo member) => new InvalidOperationException("Annotated member " + typeof(T).Name + "." + member.Name + " must be a public writable field or non-indexed property.");
        }

        sealed class Member
        {
            public string Name;
            public Type ValueType;
            public DataColumnAttribute Column;
            public Action<object, object> Set;
            public ConstructorInfo ConverterConstructor;
        }

        sealed class Binding
        {
            public Member Member;
            public int Index;
            public IDataTableValueConverter Converter;
        }
    }
}
