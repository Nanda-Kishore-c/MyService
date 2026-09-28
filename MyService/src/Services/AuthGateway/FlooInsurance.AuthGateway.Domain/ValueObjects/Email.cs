using System.Text.RegularExpressions;
using FlooInsurance.AuthGateway.Domain.Exceptions;

namespace FlooInsurance.AuthGateway.Domain.ValueObjects;

public sealed class Email : IEquatable<Email>
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string Value { get; }

    private Email(string value)
    {
        Value = value;
    }

    public static Email Create(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new DomainException("Email cannot be empty.");
        }

        var trimmed = email.Trim().ToLowerInvariant();

        if (trimmed.Length > 256)
        {
            throw new DomainException("Email exceeds maximum allowed length of 256 characters.");
        }

        if (!EmailRegex.IsMatch(trimmed))
        {
            throw new DomainException($"'{email}' is not a valid email address.");
        }

        return new Email(trimmed);
    }

    public static implicit operator string(Email email) => email.Value;

    public bool Equals(Email? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);
    }

    public override bool Equals(object? obj) => obj is Email other && Equals(other);

    public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

    public override string ToString() => Value;
}
