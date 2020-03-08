using System;
using System.Collections.Generic;
using System.Reflection;

namespace LiveBot.Core.Repository.Models
{
    public abstract class BaseModel<T> : IEquatable<T>
        where T : BaseModel<T>
    {
        public int Id { get; set; }

        public DateTime TimeStamp { get; set; } = DateTime.UtcNow;
        public bool Deleted { get; set; }

        /// <summary>Allows for the == operator to be used for equality.</summary>
        /// <param name="x">The left side of the operator.</param>
        /// <param name="y">The right side of the operator.</param>
        /// <returns>Whether or not both sides are equal.</returns>
        public static bool operator ==(BaseModel<T> x, BaseModel<T> y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (((object)x == null) || ((object)y == null))
            {
                return false;
            }

            return x.Equals(y);
        }

        /// <summary>Allows for the != operator to be used for equality.</summary>
        /// <param name="x">The left side of the operator.</param>
        /// <param name="y">The right side of the operator.</param>
        /// <returns>Whether or not both sides are not equal.</returns>
        public static bool operator !=(BaseModel<T> x, BaseModel<T> y)
        {
            return !(x == y);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            T other = obj as T;
            return Equals(other);
        }

        /// <inheritdoc />
        public virtual bool Equals(T other)
        {
            if (other == null)
                return false;

            Type t = GetType();
            Type otherType = other.GetType();

            if (t != otherType)
                return false;

            FieldInfo[] fields = t.GetFields(BindingFlags.Instance
                                             | BindingFlags.NonPublic
                                             | BindingFlags.Public);
            foreach (FieldInfo field in fields)

            {
                if (field.Name == nameof(this.TimeStamp))
                    continue;

                object value1 = field.GetValue(other);
                object value2 = field.GetValue(this);

                if (value1 == null)
                {
                    if (value2 != null)
                        return false;
                }
                else if (!value1.Equals(value2))
                    return false;
            }

            return true;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            IEnumerable<FieldInfo> fields = GetFields();

            const int StartValue = 17;
            const int Multiplier = 59;

            int hashCode = StartValue;
            foreach (FieldInfo field in fields)
            {
                object value = field.GetValue(this);
                if (value != null)
                    hashCode = (hashCode * Multiplier) + value.GetHashCode();
            }

            return hashCode;
        }

        /// <summary>Gets all instanced, non public, and public fields on this model.</summary>
        /// <returns>The collection of fields.</returns>
        private IEnumerable<FieldInfo> GetFields()
        {
            Type t = GetType();
            List<FieldInfo> fields = new List<FieldInfo>();

            while (t != typeof(object))
            {
                fields.AddRange(t.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public));
                t = t.BaseType;
            }

            return fields;
        }
    }
}