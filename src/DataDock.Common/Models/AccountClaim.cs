using System;
using System.Security.Claims;
using Nest;

namespace DataDock.Common.Models
{
    public class AccountClaim
    {
        public AccountClaim()
        {
        }

        public AccountClaim(Claim claim)
        {
            if (claim == null)
            {
                throw new ArgumentNullException(nameof(claim));
            }

            Type = claim.Type;
            Value = claim.Value;
        }

        public AccountClaim(string type, string value)
        {
            if (string.IsNullOrEmpty(type))
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentNullException(nameof(value));
            }

            Type = type;
            Value = value;
        }

        [Keyword]
        public string Type { get; set; }

        [Keyword]
        public string Value { get; set; }

        public Claim AsClaim()
        {
            return new Claim(Type, Value);
        }

        public bool Equals(AccountClaim other)
        {
            return other.Type.Equals(Type)
                   && other.Value.Equals(Value);
        }

        public bool Equals(Claim other)
        {
            return other.Type.Equals(Type)
                   && other.Value.Equals(Value);
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override string ToString()
        {
            return Type + ": " + Value;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Value != null ? Value.GetHashCode() : 0) * 397) ^ (Type != null ? Type.GetHashCode() : 0);
            }
        }

        public static bool operator ==(AccountClaim left, Claim right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(AccountClaim left, Claim right)
        {
            return !Equals(left, right);
        }
    }
}