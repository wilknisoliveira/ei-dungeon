namespace ei_back.Core.Application.Repository;

public class PersistenceConflictException : Exception
{
    public PersistenceConflictException() : base("A persistence uniqueness constraint was violated.") { }
}