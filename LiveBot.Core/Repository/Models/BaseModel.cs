using System;
using System.Runtime.CompilerServices;

namespace LiveBot.Core.Repository.Models
{
    public abstract class BaseModel<T> : IEquatable<T>
        where T : BaseModel<T>
    {
        public long Id { get; set; }

        public DateTime TimeStamp { get; set; } = DateTime.UtcNow;
        public bool Deleted { get; set; }

        /// <summary>
        /// Allows for the == operator to be used for equality.
        /// </summary>
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

        /// <summary>
        /// Allows for the != operator to be used for equality.
        /// </summary>
        /// <param name="x">The left side of the operator.</param>
        /// <param name="y">The right side of the operator.</param>
        /// <returns>Whether or not both sides are not equal.</returns>
        public static bool operator !=(BaseModel<T> x, BaseModel<T> y)
        {
            return !(x == y);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as T);
        }

        /// <summary>
        /// Two models are equal when they are the same instance or represent the same persisted row.
        /// Typed as <typeparamref name="T"/> rather than compared by <c>GetType()</c> so a lazy loading
        /// proxy and the plain entity it derives from are equal.
        /// </summary>
        /// <param name="other">The model to compare with.</param>
        /// <returns>Whether both sides are the same row.</returns>
        public virtual bool Equals(T other)
        {
            if (other is null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            return Id != 0 && Id == other.Id;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            // Unsaved models share Id 0, so they fall back to reference identity. The hash changes once
            // the database assigns an Id, so do not hold an unsaved model in a hash collection across a save.
            return Id != 0 ? Id.GetHashCode() : RuntimeHelpers.GetHashCode(this);
        }
    }
}