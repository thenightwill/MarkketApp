using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class Task
    {
        public Guid Id { get; private set; }

        public Guid UserId { get; private set; }

        public string Title { get; private set; }

        public string Description { get; private set; }

        public TaskStatus Status { get; private set; }

        public DateTime DueDate { get; private set; }

        public DateTime CreatedAt { get; private set; }

        public DateTime? UpdatedAt { get; private set; }

        private Task() { }
    }
}
