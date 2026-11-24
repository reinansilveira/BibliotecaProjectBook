using Library.Domain.Enums;

namespace Library.Domain.Entities;

public class Member
{
    private Member()
    {
    }

    public Member(
        string name,
        DateTime birthDate,
        string phone,
        string email,
        string passwordHash,
        DateTime createdAt,
        Role role = Role.Member)
    {
        Id = Guid.NewGuid();
        Name = name;
        BirthDate = birthDate;
        Phone = phone;
        Email = email;
        PasswordHash = passwordHash;
        CreatedAt = createdAt;
        Role = role;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public DateTime BirthDate { get; private set; }

    public string Phone { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public DateTime CreatedAt { get; private set; }

    public Role Role { get; private set; }

    public bool IsDeleted { get; private set; }

    public void Update(string name, DateTime birthDate, string phone, string email)
    {
        Name = name;
        BirthDate = birthDate;
        Phone = phone;
        Email = email;
    }

    public void Delete()
    {
        IsDeleted = true;
    }
}
