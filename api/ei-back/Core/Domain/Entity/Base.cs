namespace ei_back.Core.Domain.Entity
{
    public class Base
    {
        public Guid Id { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }

        public void SetCreatedDate(DateTimeOffset createdDate)
        {
            CreatedAt = createdDate;
            UpdatedAt = createdDate;
        }

        public void SetUpdatedDate(DateTimeOffset updatedDate)
        {
            UpdatedAt = updatedDate;
        }

    }
}
